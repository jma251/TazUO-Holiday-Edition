# Protocol / Transport Codemap — TazUO Holiday Edition (`legacy`)

All paths relative to `/home/user/TazUO-Holiday-Edition/`.

---

## 0. Which client class is actually live

`NetClient` (`src/ClassicUO.Client/Network/NetClient.cs:134`) is **dead code**. It is
never instantiated — `grep` for `new NetClient()` finds only its own constructor at
`NetClient.cs:148`. It survives only as a static façade:

```csharp
// NetClient.cs:164
public static AsyncNetClient Socket => AsyncNetClient.Socket;
```

Every one of the ~200 `NetClient.Socket.*` call sites in the tree therefore resolves to
`AsyncNetClient` (`src/ClassicUO.Client/Network/AsyncNetClient.cs:181`), whose singleton
is `AsyncNetClient.Socket` (`AsyncNetClient.cs:214`, replaced wholesale on relay at
`Game/Scenes/LoginScene.cs:737` and on login reset at `LoginScene.cs:390`).

The synchronous `SocketWrapper` (`NetClient.cs:43`), `NetClient.CollectAvailableData()`
(`NetClient.cs:233`), `NetClient.Flush()` (`NetClient.cs:283`) and
`NetClient.ProcessSend()` (`NetClient.cs:328`) are all unreachable. Read them for
structure, but do not modify them expecting a runtime effect.

---

## 1. A byte arrives on the socket — full trace

### 1.1 Read: a thread-pool thread, not the frame thread

`AsyncSocketWrapper.ConnectAsync` starts the reader as a detached `Task`:

```csharp
// AsyncNetClient.cs:52
_receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
```

`ReceiveLoopAsync` (`AsyncNetClient.cs:91`) runs on the thread pool:

- rents a 4096-byte scratch buffer — `AsyncNetClient.cs:93`
- polls `_socket.Client.Available` and only reads what is already buffered —
  `AsyncNetClient.cs:99-102`
- `bytesRead == 0` ⇒ remote close ⇒ `OnDisconnected` + `Disconnect()` —
  `AsyncNetClient.cs:104-110`
- copies the read bytes into a **fresh `byte[]`** and raises `OnDataReceived` synchronously
  on this same thread-pool thread — `AsyncNetClient.cs:112-117`
- `await Task.Delay(1, ...)` each iteration — `AsyncNetClient.cs:119`. This is a hard
  ~1ms-per-iteration ceiling on read frequency (in practice 1–15ms depending on the OS
  timer), and it also means one iteration reads at most 4096 bytes.
- buffer returned to the pool in `finally` — `AsyncNetClient.cs:153`

### 1.2 Decrypt then decompress — still on the network thread

`AsyncNetClient.OnDataReceived` (`AsyncNetClient.cs:338`) is wired at
`AsyncNetClient.cs:211`. In order:

1. `Statistics.TotalBytesReceived += data.Length` — `AsyncNetClient.cs:342`
   (raw wire bytes, before decompression)
2. `ProcessEncryption(span)` — `AsyncNetClient.cs:345` → `AsyncNetClient.cs:404`
3. `DecompressBuffer(span)` — `AsyncNetClient.cs:346` → `AsyncNetClient.cs:446`
4. `decompressed.ToArray()` → `_incomingMessages.Enqueue(message)` — `AsyncNetClient.cs:350-351`

**Decryption** (`AsyncNetClient.cs:404-410`) is gated on `_isCompressionEnabled`:

```csharp
private void ProcessEncryption(Span<byte> buffer)
{
    if (!_isCompressionEnabled) return;              // AsyncNetClient.cs:406-407
    EncryptionHelper.Decrypt(buffer, buffer, buffer.Length);
}
```

In-place, src == dst. See §2 for why this is almost always a no-op.

**Decompression** (`AsyncNetClient.cs:446-462`), same gate:

```csharp
if (!_isCompressionEnabled) return buffer;           // AsyncNetClient.cs:448-449
var size = 65536;
if (!_huffman.Decompress(buffer, _uncompressedBuffer, ref size)) { /* disconnect */ }
return _uncompressedBuffer.AsSpan(0, size);          // AsyncNetClient.cs:461
```

`Huffman` (`Network/Huffman.cs:37`) is the standard UO 256-entry static decode tree
(`_decTree`, `Huffman.cs:39-297`). `Decompress` (`Huffman.cs:310`) is a **stateful
bit-stream decoder** — `_bitNum`, `_value`, `_mask`, `_treePos` (`Huffman.cs:299-300`)
persist across calls, so a code word may straddle two TCP reads. `-256` is the flush/
terminator symbol which resets bit alignment (`Huffman.cs:347-352`). `Reset()`
(`Huffman.cs:302`) is called on connect (`AsyncNetClient.cs:256`), on
`EnableCompression()` (`AsyncNetClient.cs:303`) and on disconnect
(`AsyncNetClient.cs:296`). **Consequence: the Huffman state is owned by the network
thread and is per-`AsyncNetClient`-instance; the relay path throws the whole instance
away (`LoginScene.cs:737`) rather than trying to reset it.**

The scratch buffers `_compressedBuffer[4096]` and `_uncompressedBuffer[0x10000]`
(`AsyncNetClient.cs:185-186`) are instance fields touched only from the network thread.
Note `_compressedBuffer` is in fact unused in the async path — `ReceiveLoopAsync` rents
its own buffer.

### 1.3 Hand-off to the frame thread

`_incomingMessages` is a `ConcurrentQueue<byte[]>` (`AsyncNetClient.cs:192`). This is
**the only thread boundary in the receive path**. Each queued element is a decompressed
TCP *chunk*, not a packet — it may contain several packets, or a fragment of one.

Drained by `AsyncNetClient.TryDequeuePacket` (`AsyncNetClient.cs:360`), cleared by
`ClearIncomingMessages` (`AsyncNetClient.cs:365`, called from `Disconnect`,
`AsyncNetClient.cs:293`).

### 1.4 The pump, and where it sits in the game loop

```csharp
// GameController.cs:136-147
private const int MAX_PACKETS_PER_FRAME = 25;

private void ProcessNetworkPackets()
{
    int packetsProcessed = 0;
    while (packetsProcessed < MAX_PACKETS_PER_FRAME && AsyncNetClient.Socket.TryDequeuePacket(out byte[] message))
    {
        var c = PacketHandlers.Handler.ParsePackets(message);
        AsyncNetClient.Socket.Statistics.TotalPacketsReceived += (uint)c;
        packetsProcessed++;
    }
}
```

Called from `GameController.Update` (`GameController.cs:466`), which is FNA/XNA's
frame-thread `Game.Update`:

```
GameController.Update(gameTime)                       GameController.cs:466
  Time.Ticks / Time.Delta assignment                  GameController.cs:470-471
  Mouse.Update()                                      GameController.cs:474
  ── Profiler "Packets" ──
  ProcessNetworkPackets()                             GameController.cs:478   <== HERE
  ── /Profiler ──
  Plugin.Tick()                                       GameController.cs:481
  Scene.Update()                                      GameController.cs:486
  UIManager.Update()                                  GameController.cs:491
  LegionScripting.OnUpdate()                          GameController.cs:495
  MainThreadQueue.ProcessQueue()                      GameController.cs:498
```

So **handlers run on the frame thread, before the scene and UI update**, meaning world
state mutated by a handler is visible to `Scene.Update()` in the same frame.

`MAX_PACKETS_PER_FRAME` is misleadingly named: it bounds *dequeued chunks*, not packets.
A chunk can carry dozens of packets, so the real per-frame packet count is unbounded.
Conversely, a burst of >25 chunks slips to the next frame.

### 1.5 Framing: the length table

`PacketHandlers.ParsePackets(Span<byte>)` (`Network/PacketHandlers.cs:91`):

```csharp
public int ParsePackets(Span<byte> data)
{
    Append(data, false);                                            // :93
    return ParsePackets(_buffer, true) + ParsePackets(_pluginsBuffer, false);   // :95
}
```

Two independent reassembly streams, both `CircularBuffer` (`PacketHandlers.cs:88-89`):

| Stream | Fed by | `allowPlugins` |
| --- | --- | --- |
| `_buffer` | the wire, via `Append(data, false)` (`PacketHandlers.cs:165`) | `true` |
| `_pluginsBuffer` | plugin injection, via `Append(data, true)` (`Plugin.cs:757`, `Plugin.cs:779`) | `false` |

`CircularBuffer` (`Network/CircularBuffer.cs:38`) is a plain non-thread-safe ring —
`Enqueue` (`:102`) grows to `(Length + size + 2047) & ~2047` (`:106`), `Dequeue` (`:139`)
handles wrap-around, indexer `this[int]` (`:57`) reads without consuming, which is what
the framing peek relies on.

The framing loop, `ParsePackets(CircularBuffer, bool)` (`PacketHandlers.cs:98`), under
`lock (stream)` (`PacketHandlers.cs:102`):

1. `GetPacketInfo(...)` (`PacketHandlers.cs:109` → definition `PacketHandlers.cs:197`)
2. `if (stream.Length < packetlength) break;` — partial packet, wait for more data
   (`PacketHandlers.cs:125-133`; it logs a `Log.Warn "need more data"`, which is noisy but
   normal)
3. grow `_readingBuffer` by doubling until it fits (`PacketHandlers.cs:135-138`)
4. `stream.Dequeue(packetBuffer, 0, packetlength)` (`PacketHandlers.cs:140`)
5. log (§4) — `PacketHandlers.cs:142-143`
6. plugin gate (§5) — `PacketHandlers.cs:148`
7. `AnalyzePacket(...)` (`PacketHandlers.cs:150`)

`GetPacketInfo` is where the length table does its work:

```csharp
// PacketHandlers.cs:214-229
packetLen = PacketsTable.GetPacketLength(packetID = buffer[0]);
packetOffset = 1;

if (packetLen == -1)                 // variable-length packet
{
    if (bufferLen < 3) return false;
    var b0 = buffer[1];
    var b1 = buffer[2];
    packetLen = (b0 << 8) | b1;      // big-endian 16-bit total length, header inclusive
    packetOffset = 3;
}
```

`PacketsTable.GetPacketLength` (`Network/PacketsTable.cs:298`) indexes a `short[255]`
(`PacketsTable.cs:39`); `id >= 0xFF` returns `-1`. Note the array is 255 entries for 256
ids and the last comment reads `// ff` where it should be `0xFE` — the off-by-one is
masked by the `id >= 0xFF` guard, so `0xFE` actually reads the slot commented `0xFF`.

`PacketsTable.AdjustPacketSizeByVersion(ClientVersion)` (`PacketsTable.cs:303`) mutates
the table in place at startup — called once from `Client.cs:192`. It patches ~25 opcodes
across `CV_500A`, `CV_5090`, `CV_6013`, `CV_6017`, `CV_6060`, `CV_60142`, `CV_7000`,
`CV_7090`, `CV_70180`, `CV_706400`, `CV_7010400`. Because it is static mutable state
touched from the frame thread at boot and read from the frame thread during framing,
there is no race, but there is also no way to have two client versions in one process.

`Plugin` exports this same function to native plugins:
`_getPacketLength = PacketsTable.GetPacketLength;` (`Plugin.cs:191`), surfaced as
`PluginHeader.GetPacketLength` (`Plugin.cs:218`).

### 1.6 Dispatch

```csharp
// PacketHandlers.cs:181-195
private void AnalyzePacket(ReadOnlySpan<byte> data, int offset)
{
    if (data.IsEmpty) return;
    var bufferReader = _handlers[data[0]];
    if (bufferReader != null)
    {
        var buffer = new StackDataReader(data);
        buffer.Seek(offset);          // skip 1-byte or 3-byte header
        bufferReader(ref buffer);
    }
}
```

`_handlers` is `OnPacketBufferReader[0x100]` (`PacketHandlers.cs:80`), delegate signature
`void (ref StackDataReader p)` (`PacketHandlers.cs:61`). Registration is via
`Handler.Add(byte, handler)` (`PacketHandlers.cs:84`) — the bulk in the static ctor
(`PacketHandlers.cs:234` onward), plus late registrations such as
`Game/UltimaLive.cs:81-82` for `0x3F`/`0x40`.

Unregistered opcodes are silently dropped — the bytes are still consumed from the stream,
so framing stays in sync.

`buffer.Seek(offset)` means handlers begin reading **after** the header; the id and (for
variable packets) the length are already consumed.

### 1.7 One-line summary of the receive path

```
TCP → ReceiveLoopAsync (thread pool, AsyncNetClient.cs:91)
    → byte[] copy (:114)
    → OnDataReceived (:338)  [network thread]
        → ProcessEncryption (:404)  — no-op unless compression enabled AND TWOFISH_MD5
        → Huffman.Decompress (:453 / Huffman.cs:310) — no-op unless compression enabled
        → ConcurrentQueue.Enqueue (:351)         <<< THREAD BOUNDARY
    → GameController.ProcessNetworkPackets (GameController.cs:141) [frame thread]
        → PacketHandlers.ParsePackets (PacketHandlers.cs:91)
            → CircularBuffer.Enqueue (:93/:165)
            → GetPacketInfo / PacketsTable.GetPacketLength (:214)
            → CircularBuffer.Dequeue (:140)
            → PacketLogger.Default?.Log(..., false) (:142)
            → Plugin.ProcessRecvPacket (:148)     <<< PLUGIN VETO POINT
            → AnalyzePacket → _handlers[id](ref reader) (:186-193)
```

---

## 2. Is traffic encrypted on a plain unencrypted shard? No.

### 2.1 The decision

Everything hangs off `EncryptionHelper.Type` (`Network/Encryption/Encryption.cs:56`), a
static field of `ENCRYPTION_TYPE` (`Encryption.cs:38-46`) whose zero value is `NONE`.
**Nothing initializes it** — it is `NONE` unless `CalculateEncryption` is called.

```csharp
// Client.cs:194-205
if (Settings.GlobalSettings.Encryption != 0)
{
    Log.Trace("Calculating encryption by client version...");
    EncryptionHelper.CalculateEncryption(Version);
    Log.Trace($"encryption: {EncryptionHelper.Type}");

    if (EncryptionHelper.Type != (ENCRYPTION_TYPE)Settings.GlobalSettings.Encryption)
    {
        Log.Warn($"Encryption found: {EncryptionHelper.Type}");
        Settings.GlobalSettings.Encryption = (byte)EncryptionHelper.Type;
    }
}
```

`Settings.GlobalSettings.Encryption` is `byte` (`Configuration/Settings.cs:159`,
`[JsonPropertyName("encryption")]`), default `0`, settable only via the `-encryption`
command-line switch (`Main.cs:494`). **On a plain shard it stays `0`, the `if` never
fires, `CalculateEncryption` never runs, and `Type` stays `NONE`.**

`WorldMapEntityManager` uses the same field as a proxy for "is this a vanilla-protocol
shard" (`Game/Managers/WorldMapEntityManager.cs:90`, `:110`).

### 2.2 What `NONE` does at each call site

**Init** — `EncryptionHelper.Initialize` (`Encryption.cs:112`) returns immediately:

```csharp
if (encryption == ENCRYPTION_TYPE.NONE) return;   // Encryption.cs:114-117
```

Called twice: on connect to the login server with `is_login: true`
(`LoginScene.cs:574`), and after the relay with `is_login: false` (`LoginScene.cs:739`).
Both pass `(ENCRYPTION_TYPE)Settings.GlobalSettings.Encryption`, i.e. `NONE` on a plain
shard, so neither `LoginCryptBehaviour` (`Encryption/LoginCryptBehaviour.cs:37`),
`BlowfishEncryption` (`Encryption/BlowfishBehaviour.cs:37`) nor `TwofishEncryption`
(`Encryption/TwofishBehaviour.cs:48`) ever gets keyed.

**Send** — `EncryptionHelper.Encrypt` (`Encryption.cs:138`) returns immediately:

```csharp
if (Type == ENCRYPTION_TYPE.NONE) return;         // Encryption.cs:140-143
```

Call site `AsyncNetClient.cs:390-393`:

```csharp
if (!skipEncryption)
{
    EncryptionHelper.Encrypt(!_isCompressionEnabled, message, message, message.Length);
}
```

Note the `is_login` argument is derived from `!_isCompressionEnabled` — i.e. "we have not
yet been relayed to the game server", which is how the login-stream cipher gets selected
without an explicit phase flag.

**Receive** — `EncryptionHelper.Decrypt` (`Encryption.cs:194`) is even narrower:

```csharp
public static void Decrypt(Span<byte> src, Span<byte> dst, int size)
{
    if (Type == ENCRYPTION_TYPE.TWOFISH_MD5)
    {
        _twoFishBehaviour.Decrypt(src, dst, size);
    }
}
```

**Server → client is decrypted only under `TWOFISH_MD5`, and only after
`EnableCompression()` has been called** (the `_isCompressionEnabled` gate at
`AsyncNetClient.cs:406`). Under every other `ENCRYPTION_TYPE` the inbound stream is
treated as plaintext — which matches the real protocol: pre-Twofish UO encryption is
client→server only.

### 2.3 So on a plain shard

- No key material is ever derived.
- `Encrypt` is a two-instruction early return on every outbound packet.
- `Decrypt` is a two-instruction early return on every inbound chunk.
- The wire is plaintext UO in both directions.
- Compression is a separate concern and **is** active after relay (§2.4) — inbound game
  traffic is Huffman-compressed even with `NONE`.

### 2.4 Where the phase flip happens

`_isCompressionEnabled` (`AsyncNetClient.cs:188`) is `false` until
`AsyncNetClient.EnableCompression()` (`AsyncNetClient.cs:300`), whose only caller is the
relay handler:

```csharp
// LoginScene.cs:730-753  (HandleRelayServerPacket, opcode 0x8C)
long ip   = p.ReadUInt32LE();
ushort port = p.ReadUInt16BE();
uint seed = p.ReadUInt32BE();

NetClient.Socket.Disconnect().Wait();                                    // :736
AsyncNetClient.Socket = new AsyncNetClient();                            // :737  fresh instance
ClassicUO.Network.PacketHandlers.Handler.Reset();                        // :738  clears both CircularBuffers
EncryptionHelper.Initialize(false, seed, (ENCRYPTION_TYPE)Settings.GlobalSettings.Encryption);  // :739

NetClient.Socket.Connect(new IPAddress(ip).ToString(), port).Wait();     // :741

if (NetClient.Socket.IsConnected)
{
    NetClient.Socket.EnableCompression();                                // :745
    Span<byte> b = stackalloc byte[4] { ... seed big-endian ... };
    NetClient.Socket.Send(b, true, true);                                // :749  ignorePlugin AND skipEncryption
    NetClient.Socket.Send_SecondLogin(Account, Password, seed);          // :752
}
```

Two things to note. `.Wait()` on the frame thread (`:736`, `:741`) is a synchronous block
inside `Update` — a slow or unreachable game server stalls the render loop. And the 4-byte
seed prelude is sent raw (`skipEncryption: true`) because it precedes the cipher, as is
`Send_Seed` (`OutgoingPackets.cs:221`, also `true, true`).

`PacketHandlers.Handler.Reset()` (`PacketHandlers.cs:168`) clears both circular buffers
under their own locks — essential, since the login-server stream's trailing bytes must not
be reinterpreted as game-server packets.

---

## 3. Frame thread vs network thread — the definitive answer

**Handlers run on the frame thread.** The marshalling point is the
`ConcurrentQueue<byte[]> _incomingMessages` at `AsyncNetClient.cs:192`.

| Stage | Thread | Location |
| --- | --- | --- |
| `recv()` | thread pool (`Task.Run`) | `AsyncNetClient.cs:52`, `:91` |
| decrypt | thread pool | `AsyncNetClient.cs:345` → `:404` |
| Huffman decompress | thread pool | `AsyncNetClient.cs:346` → `:446` |
| enqueue | thread pool | `AsyncNetClient.cs:351` |
| **dequeue** | **frame thread** | `GameController.cs:141` |
| framing / length table | frame thread | `PacketHandlers.cs:98-158` |
| `PacketLogger` (inbound) | frame thread | `PacketHandlers.cs:142` |
| `Plugin.ProcessRecvPacket` | frame thread | `PacketHandlers.cs:148` |
| **handler invocation** | **frame thread** | `PacketHandlers.cs:193` |

The outbound path is the mirror image and crosses in the other direction:
`Send` (`AsyncNetClient.cs:372`) is called from the frame thread, runs the plugin hook,
the logger and the cipher inline, then enqueues into `_sendStream` under
`lock (_sendStream)` (`AsyncNetClient.cs:395-398`). `NetworkLoopAsync`
(`AsyncNetClient.cs:307`, started at `AsyncNetClient.cs:264`) drains it on the thread pool
via `ProcessSendAsync` (`AsyncNetClient.cs:412`), taking the same lock at
`AsyncNetClient.cs:422`. `NetworkLoopAsync` also drives `Statistics.Update()`
(`AsyncNetClient.cs:317`).

Two consequences worth naming:

- `EncryptionHelper`'s cipher objects (`Encryption.cs:50-52`) are static and stateful
  (stream ciphers with evolving key state). `Encrypt` is only ever invoked from the frame
  thread and `Decrypt` only from the network thread, so they do not collide *today* — but
  they are unsynchronized statics, and any future call to `Send` from a background thread
  would corrupt the outbound key stream silently.
- `PacketHandlers.Append(data, false)` at `PacketHandlers.cs:93` is called **without** a
  lock, while `ParsePackets` locks the stream (`:102`) and plugin injection locks
  `PacketHandlers.Handler` (`Plugin.cs:755`, `Plugin.cs:777`) — a *different* monitor from
  the `CircularBuffer` objects the framing loop locks. A plugin injecting from a
  background thread races the frame thread's `_pluginsBuffer` access.

---

## 4. PacketLogger

`Network/PacketLogger.cs:8`. Singleton `PacketLogger.Default` (`PacketLogger.cs:10`).
Note the unused per-instance `PacketHandlers._packetLogger` field at
`PacketHandlers.cs:87` — a leftover; all real logging goes through `Default`.

### Capture points

| Direction | Call site | What is logged |
| --- | --- | --- |
| Client → Server | `AsyncNetClient.cs:387` (and dead `NetClient.cs:303`) | **after** the plugin send hook, **before** encryption — plaintext |
| Server → Client | `PacketHandlers.cs:142` | after decrypt + decompress, after framing, **before** the plugin recv hook |

So the log is always plaintext, always exactly one framed packet per entry on the inbound
side, and one `Send` call's worth on the outbound side.

### Enabling

Off by default (`Enabled`, `PacketLogger.cs:16`). Only switch is the `-packetlog`
command-line argument (`Main.cs:526-546`):

```
-packetlog                 # log everything
-packetlog 0x1A,0x77,20    # only these ids; "0x" prefix optional, always parsed as hex
```

`Main.cs:528-529` sets `Enabled = true` and calls `CreateFile()`. The id filter goes into
`LogPacketID` (`PacketLogger.cs:11`), applied at `PacketLogger.cs:30`:

```csharp
if (LogPacketID.Count != 0 && !LogPacketID.Contains(message[0])) return;
```

Empty list = log all.

### File location

```csharp
// PacketLogger.cs:20-24
public LogFile CreateFile()
{
    _logFile?.Dispose();
    return _logFile = new LogFile(FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Logs", "Network"), "packets.log");
}
```

→ `<exe dir>/Logs/Network/<yyyy-MM-dd_hh-mm-ss>_packets.log`, the timestamp prefix being
added by `LogFile`'s constructor (`ClassicUO.Utility/Logging/LogFile.cs:44-55`, opened
`FileMode.Append`, `FileShare.ReadWrite`, async). `LogFile.Write` flushes every call
(`LogFile.cs:80`), so the log is complete even after a hard crash — and correspondingly
slow.

**If `CreateFile()` was never called, `_logFile` is null and entries go to
`Console.WriteLine` instead** (`PacketLogger.cs:106-109`) — which on a `-p:IS_DEV_BUILD=true`
`WinExe` build means they go nowhere.

### Format

Built with a stack-allocated `ValueStringBuilder` (256 chars, `PacketLogger.cs:33-34`),
indented by `sizeof(ulong) + 2` = 10 spaces (`PacketLogger.cs:36`), header then classic
hex dump with an ASCII gutter:

```
          Time: 14:22:07 | Server -> Client |  ID: 1A   Length: 21
          0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F
          -- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --
00000000  1A 00 15 40 00 12 34 09  B0 00 00 04 8F 03 E8 00  ...@..4.........
00000010  44 00 00 07 05                                    D....
```

- header line: `PacketLogger.cs:39` — wall-clock `{0:T}`, direction string, id as `X2`,
  decimal length
- 16 bytes per row, extra space after byte 7 (`PacketLogger.cs:62-65`), address column is
  a per-packet offset starting at 0, not a file offset (`PacketLogger.cs:54`, `:58`)
- short final rows padded with three spaces per missing byte (`PacketLogger.cs:70-74`) so
  the ASCII gutter stays aligned
- printable range is `0x20..0x7F`, everything else `.` (`PacketLogger.cs:83-90`)
- two trailing newlines (`PacketLogger.cs:97-98`), plus `LogFile.Write` appends one more
  (`LogFile.cs:79`)

### What it excludes

1. **Credentials.** Opcodes `0x80` (first login) and `0x91` (second login) have their
   bodies replaced with `[ACCOUNT CREDENTIALS HIDDEN]` (`PacketLogger.cs:41-45`). The
   header line — time, direction, id, length — is still written.
2. **Everything outside the id filter** when `-packetlog <ids>` was given
   (`PacketLogger.cs:30`).
3. **Everything, when disabled** (`PacketLogger.cs:28`).
4. Implicitly: raw ciphertext and compressed bytes are never visible, since both hooks sit
   on the plaintext side.

The credential redaction has a genuine bug at `PacketLogger.cs:41`:

```csharp
if ((message[0] == 0x80 || message[0] == 0x91) && (!LogPacketID.Contains(0x80) || !LogPacketID.Contains(0x91)))
```

The intent (per the trailing comment, "Avoid logging account UNLESS requested
specifically") is that asking for `0x80` explicitly opts you into seeing it. But the `||`
means the guard holds unless **both** `0x80` and `0x91` are in the list. `-packetlog 0x80`
alone still redacts, because `0x91` is absent. It errs toward hiding credentials, so the
failure is benign; it should be `&&` — or, more precisely,
`!LogPacketID.Contains(message[0])`.

### Sibling logger

`HouseDiagnostics.LogPacket` (`Game/Managers/HouseDiagnostics.cs:714`) is wired at exactly
the same two points (`AsyncNetClient.cs:388`, `PacketHandlers.cs:143`). It applies the same
`0x80`/`0x91` exclusion but unconditionally (`HouseDiagnostics.cs:723-727` — "Never these
two. They carry account name and password in clear."), and decodes a handful of opcodes
into prose rather than hex.

---

## 5. Plugins

`Network/Plugin.cs:55`, `internal unsafe class Plugin`. Native DLLs loaded from
`<exe dir>/Data/Plugins/<name>` (`Plugin.cs:156-158`) via `Native.LoadLibrary` +
`GetProcessAddress("Install")` (`Plugin.cs:246-266`), with a managed fallback that reflects
for `Assistant.Engine.Install` (`Plugin.cs:274-300`) — that is the Razor path. On Windows
the plugin directory is de-zoned first (`Plugin.cs:241`, `UnblockPath`/`UnblockFile`,
`Plugin.cs:797-819`).

Contract is the `PluginHeader` struct (`Plugin.cs:1371-1408`), passed by pointer. The
client fills in the callbacks it offers (`Plugin.cs:213-232`); the plugin fills in the
callbacks it wants, which the client then converts to delegates (`Plugin.cs:312-413`).

### What a plugin can hook

| Header field | Bound at | Dispatcher | Fires from |
| --- | --- | --- | --- |
| `OnInitialize` | `Plugin.cs:349` | called inline | `Plugin.cs:417-420`, end of `Load()` |
| `OnRecv` / `OnRecv_new` | `Plugin.cs:312`, `:389` | `ProcessRecvPacket` (`Plugin.cs:532`) | `PacketHandlers.cs:148`, frame thread |
| `OnSend` / `OnSend_new` | `Plugin.cs:317`, `:396` | `ProcessSendPacket` (`Plugin.cs:567`) | `AsyncNetClient.cs:379`, frame thread |
| `Tick` | `Plugin.cs:384` | `Plugin.Tick` (`Plugin.cs:521`) | `GameController.cs:481`, every frame |
| `OnHotkeyPressed` | `Plugin.cs:322` | `ProcessHotkeys` (`Plugin.cs:661`) | keyboard, **can veto** |
| `OnMouse` | `Plugin.cs:329` | `ProcessMouse` (`Plugin.cs:692`) | mouse, notify-only |
| `OnPlayerPositionChanged` | `Plugin.cs:334` | `UpdatePlayerPosition` (`Plugin.cs:732`) | movement |
| `OnClientClosing` | `Plugin.cs:342` | `OnClosing` (`Plugin.cs:604`) | shutdown |
| `OnConnected` / `OnDisconnected` | `Plugin.cs:356`, `:363` | `Plugin.cs:639`, `:650` | socket lifecycle |
| `OnFocusGained` / `OnFocusLost` | `Plugin.cs:370`, `:377` | `Plugin.cs:617`, `:628` | window focus |
| `OnDrawCmdList` | `Plugin.cs:403` | `ProcessDrawCmdList` (`Plugin.cs:700`) | render, plugin draws into the GD |
| `OnWndProc` | `Plugin.cs:410` | `ProcessWndProc` (`Plugin.cs:717`) | raw SDL events |

Services offered *to* the plugin (`Plugin.cs:187-200`): `Recv`/`Send` injection,
`GetPacketLength`, `GetPlayerPosition`, `CastSpell`, `GetStaticImage`, `GetUOFilePath`,
`RequestMove`, `SetTitle`, `GetStaticData`, `GetTileData`, `GetCliloc`.

### Can a plugin swallow or rewrite an inbound packet before the handler runs?

**Yes to both.**

```csharp
// PacketHandlers.cs:148-153
if (!allowPlugins || Plugin.ProcessRecvPacket(packetBuffer, ref packetlength))
{
    AnalyzePacket(packetBuffer.AsSpan(0, packetlength), offset);
    ++packetsCount;
}
```

Returning `false` skips `AnalyzePacket` entirely — the handler never runs and the packet is
not counted in `Statistics.TotalPacketsReceived`. The bytes have already been dequeued from
the `CircularBuffer` (`PacketHandlers.cs:140`), so framing stays intact; the packet is
genuinely dropped, not deferred.

`ProcessRecvPacket` (`Plugin.cs:532-565`):

```csharp
foreach (Plugin plugin in Plugins)
{
    if (plugin._onRecv_new != null)
    {
        byte[] tmp = new byte[length];
        Array.Copy(data, tmp, length);
        if (!plugin._onRecv_new(tmp, ref length)) { result = false; }
        Array.Copy(tmp, data, length);
    }
    else if (plugin._onRecv != null) { /* identical, via ref byte[] */ }
}
```

Every plugin sees the packet even after one has vetoed — `result` is latched to `false` but
the loop continues, so ordering matters for mutation but not for the veto. Each plugin gets
a **fresh copy**, mutates it, and the copy is written back over `data`, so plugin *N+1*
sees plugin *N*'s edits. `length` is `ref`, so a plugin can shorten or lengthen the packet;
`AnalyzePacket` then reads `packetBuffer.AsSpan(0, packetlength)` with the new length.

Three sharp edges here:

- **`offset` is computed before the plugin runs** (`PacketHandlers.cs:112-114`) and is
  passed to `AnalyzePacket` unchanged (`:150`). A plugin that rewrites a variable-length
  packet into a fixed-length one, or vice versa, leaves the reader seeking to the wrong
  start position.
- **A plugin can grow `length` past `packetBuffer.Length`.** The doubling loop at
  `PacketHandlers.cs:135-138` ran against the *original* length. `Array.Copy(tmp, data,
  length)` at `Plugin.cs:548` would then throw.
- Two `byte[]` allocations per packet per plugin (`Plugin.cs:540`, `:552`) — this is on the
  hot path for every inbound packet. The `// TODO` at `PacketHandlers.cs:145-147` calls
  this out: *"the pluging function should allow Span&lt;byte&gt; or unsafe type only. The
  current one is a bad style decision."*

### Outbound

Same shape, at the top of `Send`:

```csharp
// AsyncNetClient.cs:379-385
if (!ignorePlugin && !Plugin.ProcessSendPacket(ref message))
{
    return;                       // swallowed — never reaches the wire, logger, or cipher
}
if (message.IsEmpty) return;
```

`ProcessSendPacket` (`Plugin.cs:567-602`) takes `ref Span<byte>` and reslices in place:
`message = message.Slice(0, length); tmp.AsSpan(0, length).CopyTo(message);`
(`Plugin.cs:583-584`). A plugin can therefore only **shrink** an outbound packet — growing
`length` makes the `Slice` throw, since the span is a view over the caller's buffer.

Because the veto happens before `PacketLogger.Default?.Log(message, true)`
(`AsyncNetClient.cs:387`), a swallowed outbound packet leaves no trace in the packet log.
Compare the inbound side, where the log is written *before* the plugin hook
(`PacketHandlers.cs:142` vs `:148`) — so a swallowed inbound packet **is** logged. The
asymmetry is worth remembering when reading a log to debug a plugin.

`ignorePlugin: true` bypasses the hook entirely — used by the plugin's own injection path
(`Plugin.cs:767`, `:790`) to avoid recursion, and by the seed prelude
(`LoginScene.cs:749`, `OutgoingPackets.cs:221`).

### Injection

```csharp
// Plugin.cs:753-761  — plugin fabricates an inbound packet
private static bool OnPluginRecv(ref byte[] data, ref int length)
{
    lock (PacketHandlers.Handler) { PacketHandlers.Handler.Append(data.AsSpan(0, length), true); }
    return true;
}

// Plugin.cs:763-771  — plugin sends an outbound packet
private static bool OnPluginSend(ref byte[] data, ref int length)
{
    if (NetClient.Socket.IsConnected) { NetClient.Socket.Send(data.AsSpan(0, length), true); }
    return true;
}
```

`IntPtr` variants at `Plugin.cs:773` and `Plugin.cs:786`.

Injected inbound data lands in `_pluginsBuffer`, parsed with `allowPlugins: false`
(`PacketHandlers.cs:95`) — so injected packets are **not** re-offered to the recv hook,
which prevents an infinite loop, and are also **not** written to the packet log by the
inbound call at `PacketHandlers.cs:142`... except they are, because that log call sits
inside the shared `ParsePackets(CircularBuffer, bool)` body above the `allowPlugins`
check. Injected packets therefore appear in the log indistinguishable from real ones.

Injected outbound data goes through `Send(..., ignorePlugin: true)` — so it is logged
(`AsyncNetClient.cs:387`) and encrypted (`:390`) like any other packet.

Note the lock at `Plugin.cs:755`/`:777` is on `PacketHandlers.Handler`, while the framing
loop locks the `CircularBuffer` instance (`PacketHandlers.cs:102`) — different monitors,
so these do not actually exclude each other. See §3.

---

## 6. Quick reference

| Question | Answer | Anchor |
| --- | --- | --- |
| Who reads the socket? | thread-pool task | `AsyncNetClient.cs:52`, `:91` |
| Where is it buffered? | `ConcurrentQueue<byte[]>` then two `CircularBuffer`s | `AsyncNetClient.cs:192`; `PacketHandlers.cs:88-89` |
| Where is the length table applied? | `GetPacketInfo` | `PacketHandlers.cs:214` |
| Variable-length encoding? | `-1` in table ⇒ big-endian u16 at bytes 1-2, header-inclusive | `PacketHandlers.cs:217-229` |
| Where does decompression happen? | network thread, before enqueue | `AsyncNetClient.cs:346` → `:446` |
| Where is decryption applied? | network thread; only if compression on **and** `TWOFISH_MD5` | `AsyncNetClient.cs:404`; `Encryption.cs:194-200` |
| Encrypted on a plain shard? | No — `Type` stays `NONE`, both paths early-return | `Client.cs:194`; `Encryption.cs:140`, `:114` |
| Handler thread? | frame thread | `GameController.cs:478` → `PacketHandlers.cs:193` |
| Where in the loop? | after `Mouse.Update`, before `Plugin.Tick` / `Scene.Update` | `GameController.cs:473-486` |
| Packet log file? | `<exe>/Logs/Network/<timestamp>_packets.log` | `PacketLogger.cs:23` |
| Log enable? | `-packetlog[ ids]` only | `Main.cs:526-546` |
| Log excludes? | `0x80`/`0x91` bodies; non-matching ids; all when disabled | `PacketLogger.cs:41`, `:30`, `:28` |
| Plugin can drop inbound? | yes, return `false` from `OnRecv` | `PacketHandlers.cs:148` |
| Plugin can rewrite inbound? | yes, mutate buffer + `ref length` | `Plugin.cs:540-548` |
| Plugin can drop outbound? | yes | `AsyncNetClient.cs:379` |
| Plugin can grow outbound? | no — `Slice` throws | `Plugin.cs:583` |

## 7. Things that look wrong

1. `PacketLogger.cs:41` — credential-redaction guard uses `||` where `&&` is meant;
   `-packetlog 0x80` cannot actually reveal `0x80`. Benign direction.
2. `PacketHandlers.cs:150` — `offset` is stale if a plugin changed the packet's
   length-class in `ProcessRecvPacket`.
3. `Plugin.cs:548` — `Array.Copy(tmp, data, length)` with a plugin-grown `length` overruns
   `_readingBuffer`; the doubling loop at `PacketHandlers.cs:135` ran on the pre-hook length.
4. `PacketHandlers.cs:93` — `Append` outside any lock; plugin injection locks a different
   monitor (`Plugin.cs:755`) than the framing loop (`PacketHandlers.cs:102`).
5. `LoginScene.cs:736`, `:741` — `.Wait()` on the frame thread during relay.
6. `PacketsTable.cs:39` — `short[255]` for 256 ids; final comment says `// ff` but the slot
   is `0xFE`. Masked by the `id >= 0xFF` guard at `PacketsTable.cs:300`.
7. `PacketHandlers.cs:127` — `Log.Warn("need more data")` on every legitimate partial read.
8. `NetClient.cs` — entire synchronous implementation is unreachable; only the static
   `Socket` property at `:164` matters.
9. `AsyncNetClient.cs:185` — `_compressedBuffer` is never used in the async path.
10. `PacketHandlers.cs:87` — `_packetLogger` field is dead; logging goes via
    `PacketLogger.Default`.
