using ClassicUO.Network.Encryption;
using ClassicUO.Utility.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Buffers;
using SDL2;

namespace ClassicUO.Network
{
    /// <summary>
    /// The buffer is borrowed: it is valid for the duration of the call and is
    /// reused for the next read. A handler that needs to keep the bytes must copy
    /// them. Passing the pooled buffer rather than a fresh array is what keeps the
    /// receive path from allocating once per packet.
    /// </summary>
    delegate void DataReceivedEventHandler(object sender, byte[] buffer, int offset, int count);

    sealed class AsyncSocketWrapper : IDisposable
    {
        private const int CONNECT_TIMEOUT_MS = 15_000;

        private TcpClient _socket;
        private NetworkStream _stream;
        private int _disposed;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _receiveTask;
        public bool IsConnected => _socket?.Client?.Connected ?? false;
        public EndPoint LocalEndPoint => _socket?.Client?.LocalEndPoint;

        public event EventHandler OnConnected, OnDisconnected;
        public event EventHandler<SocketError> OnError;
        public event DataReceivedEventHandler OnDataReceived;

        public async Task<bool> ConnectAsync(string ip, int port, CancellationToken cancellationToken = default)
        {
            if (IsConnected)
                return true;

            try
            {
                _socket = new TcpClient();
                _socket.NoDelay = true;
                _cancellationTokenSource = new CancellationTokenSource();

                // TcpClient.ConnectAsync takes no timeout and no cancellation token,
                // so a server that accepts the TCP connection slowly - or a filtered
                // port that never answers - left the login screen waiting on the OS
                // connect timeout with nothing said. Race it against a clock instead.
                Task connectTask = _socket.ConnectAsync(ip, port);

                if (await Task.WhenAny(connectTask, Task.Delay(CONNECT_TIMEOUT_MS, cancellationToken)) != connectTask)
                {
                    CloseSocket();

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Log.Error($"Connection to {ip}:{port} timed out.");
                        OnError?.Invoke(this, SocketError.TimedOut);
                    }

                    return false;
                }

                await connectTask;

                if (!IsConnected)
                {
                    OnError?.Invoke(this, SocketError.NotConnected);

                    return false;
                }

                _stream = _socket.GetStream();

                // Start background receive task
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

                OnConnected?.Invoke(this, EventArgs.Empty);

                return true;
            }
            catch (SocketException socketEx)
            {
                Log.Error($"Error while connecting {socketEx}");
                OnError?.Invoke(this, socketEx.SocketErrorCode);

                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"Error while connecting {ex}");
                OnError?.Invoke(this, SocketError.SocketError);

                return false;
            }
        }

        public async Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (!IsConnected || _stream == null)
                return;

            try
            {
                await _stream.WriteAsync(buffer, offset, count, cancellationToken);
                await _stream.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"Error while sending {ex}");
                OnError?.Invoke(this, SocketError.SocketError);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4096);

            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    // ReadAsync already waits asynchronously until bytes arrive. The
                    // old shape asked Available first, read only what was already
                    // buffered, and then slept a millisecond - so an idle connection
                    // woke this loop a thousand times a second to find nothing, and a
                    // busy one paid that millisecond as latency on every packet.
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        OnDisconnected?.Invoke(this, EventArgs.Empty);
                        CloseSocket();

                        break;
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // The handler reads the pooled buffer in place. Nothing here
                        // allocates per packet any more.
                        OnDataReceived?.Invoke(this, buffer, 0, bytesRead);
                    }
                }
            }
            catch (IOException) when (cancellationToken.IsCancellationRequested)
            {
                CloseSocket();
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                CloseSocket();
            }
            catch (IOException ioEx) when (ioEx.InnerException is SocketException socketEx)
            {
                CloseSocket();

                switch (socketEx.SocketErrorCode)
                {
                    case SocketError.OperationAborted: break;
                    default:
                        Log.Error($"Socket error in receive loop: {socketEx.SocketErrorCode} - {socketEx.Message}");
                        OnError?.Invoke(this, socketEx.SocketErrorCode); break;
                }

            }
            catch (OperationCanceledException)
            {
                CloseSocket();
            }
            catch (Exception ex)
            {
                Log.Error($"Error in receive loop {ex}");
                CloseSocket();
                OnError?.Invoke(this, SocketError.SocketError);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public void Disconnect()
        {
            CloseSocket();
        }

        /// <summary>
        /// Awaits the receive loop's exit for callers that need the socket quiet
        /// before carrying on. Disconnect used to do this with Task.Wait(5000),
        /// which blocks whichever thread called it - the game thread, in practice -
        /// for as long as the loop takes to notice.
        /// </summary>
        public async Task WaitForReceiveCompletionAsync()
        {
            Task receiveTask = _receiveTask;

            if (receiveTask != null && !receiveTask.IsCompleted)
            {
                try
                {
                    await receiveTask;
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Idempotent by construction: cancelling a cancelled token and closing a
        /// closed socket are both no-ops, so this needs no guard flag.
        ///
        /// It used to have one - a _isDisconnecting bool that was set on the first
        /// disconnect and never cleared. This wrapper is built once, in the
        /// constructor of a static AsyncNetClient.Socket, and reused for every
        /// connection the process makes. So after the first disconnect the flag
        /// stayed true and every later Disconnect returned without cancelling the
        /// token, leaving the previous receive loop running. On the next connect
        /// that loop read from the newly assigned stream alongside the new one -
        /// two readers taking turns at the same byte stream.
        /// </summary>
        private void CloseSocket()
        {
            _cancellationTokenSource?.Cancel();
            _stream?.Close();
            _socket?.Close();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            CloseSocket();
            _stream?.Dispose();
            _socket?.Dispose();

            // The receive loop holds the token; disposing the source while it is
            // still in ReadAsync throws inside it. Hand the disposal to whoever
            // finishes last.
            CancellationTokenSource cancellation = _cancellationTokenSource;
            Task receiveTask = _receiveTask;

            if (receiveTask == null || receiveTask.IsCompleted)
            {
                cancellation?.Dispose();
            }
            else
            {
                receiveTask.ContinueWith(
                    _ => cancellation?.Dispose(),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default
                );
            }
        }
    }

    internal sealed class AsyncNetClient : IDisposable
    {
        private const int BUFF_SIZE = 0x10000;

        private readonly byte[] _compressedBuffer = new byte[4096];
        private readonly byte[] _uncompressedBuffer = new byte[BUFF_SIZE];
        private readonly Huffman _huffman = new Huffman();
        private bool _isCompressionEnabled;
        private readonly AsyncSocketWrapper _socket;
        private uint? _localIP;
        private readonly CircularBuffer _sendStream;
        private readonly ConcurrentQueue<byte[]> _incomingMessages = new();
        private Task _networkTask;
        private CancellationTokenSource _cancellationTokenSource = new();

        public AsyncNetClient()
        {
            Statistics = new NetStatistics(this);
            _sendStream = new CircularBuffer();

            _socket = new AsyncSocketWrapper();

            _socket.OnConnected += (o, e) =>
            {
                Statistics.Reset();
                Connected?.Invoke(this, EventArgs.Empty);
            };

            _socket.OnDisconnected += (o, e) => Disconnected?.Invoke(this, SocketError.Success);
            _socket.OnError += (o, e) => Disconnected?.Invoke(this, e);
            _socket.OnDataReceived += OnDataReceived;
        }

        public static AsyncNetClient Socket { get; set; } = new AsyncNetClient();

        public bool IsConnected => _socket != null && _socket.IsConnected;
        public NetStatistics Statistics { get; }

        public uint LocalIP
        {
            get
            {
                if (!_localIP.HasValue)
                {
                    try
                    {
                        byte[] addressBytes = (_socket?.LocalEndPoint as IPEndPoint)?.Address.MapToIPv4().GetAddressBytes();

                        if (addressBytes != null && addressBytes.Length != 0)
                        {
                            _localIP = (uint)(addressBytes[0] | (addressBytes[1] << 8) | (addressBytes[2] << 16) | (addressBytes[3] << 24));
                        }

                        if (!_localIP.HasValue || _localIP == 0)
                        {
                            _localIP = 0x100007f;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"error while retrieving local endpoint address: \n{ex}");
                        _localIP = 0x100007f;
                    }
                }

                return _localIP.Value;
            }
        }

        public event EventHandler Connected;
        public event EventHandler<SocketError> Disconnected;

        public async Task<bool> Connect(string ip, ushort port, CancellationToken cancellationToken = new ())
        {
            _sendStream.Clear();
            _huffman.Reset();
            Statistics.Reset();

            var success = await _socket.ConnectAsync(ip, port, cancellationToken);

            if (success)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _networkTask = Task.Run(() => NetworkLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }

            return success;
        }

        private bool _isDisconnecting;

        /// <summary>
        /// The flag is re-entrancy protection, not a latch. It used to be set on the
        /// first disconnect and never cleared, and since AsyncNetClient.Socket is a
        /// static built once per process, that made every disconnect after the first
        /// return immediately - no cancellation, no teardown, the previous network
        /// loop still running when the next connection came up. Clearing it in a
        /// finally keeps the guard and drops the latch.
        /// </summary>
        public async Task Disconnect()
        {
            if (_isDisconnecting)
                return;

            _isDisconnecting = true;

            try
            {
                SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_FALSE);
                _isCompressionEnabled = false;
                Statistics.Reset();

                _cancellationTokenSource?.Cancel();

                if (_networkTask != null)
                {
                    try
                    {
                        await Task.WhenAny(_networkTask, Task.Delay(5000));
                    }
                    catch { }
                }

                ClearIncomingMessages();

                _socket.Disconnect();
                _huffman.Reset();
                _sendStream.Clear();
            }
            finally
            {
                _isDisconnecting = false;
            }
        }

        public void EnableCompression()
        {
            _isCompressionEnabled = true;
            _huffman.Reset();
            _sendStream.Clear();
        }

        private async Task NetworkLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    // Process outgoing data
                    await ProcessSendAsync(cancellationToken);

                    // Update statistics
                    Statistics.Update();

                    // Small delay to prevent excessive CPU usage
                    await Task.Delay(1, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await Disconnect();
                    Disconnected?.Invoke(this, SocketError.Success);
                    break;
                }
                catch (Exception ex)
                {
                    await Disconnect();
                    Log.Error($"Network loop error: {ex}");
                    Disconnected?.Invoke(this, SocketError.SocketError);
                    break;
                }
            }
        }

        private void OnDataReceived(object sender, byte[] buffer, int offset, int count)
        {
            try
            {
                Statistics.TotalBytesReceived += (uint)count;

                // The buffer belongs to the receive loop and is reused on the next
                // read. Everything below either works on the span in place or copies
                // out of it - the enqueued message is already a fresh array.
                var span = new Span<byte>(buffer, offset, count);
                ProcessEncryption(span);
                var decompressed = DecompressBuffer(span);

                if (!decompressed.IsEmpty)
                {
                    var message = decompressed.ToArray();
                    _incomingMessages.Enqueue(message);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing received data: {ex}");
            }
        }

        public bool TryDequeuePacket(out byte[] packet)
        {
            return _incomingMessages.TryDequeue(out packet);
        }

        public void ClearIncomingMessages()
        {
            while (_incomingMessages.TryDequeue(out _))
            {
            }
        }

        public void Send(Span<byte> message, bool ignorePlugin = false, bool skipEncryption = false)
        {
            if (!IsConnected || message == null || message.Length == 0)
            {
                return;
            }

            if (!ignorePlugin && !Plugin.ProcessSendPacket(ref message))
            {
                return;
            }

            if (message.IsEmpty)
                return;

            PacketLogger.Default?.Log(message, true);
            Game.Managers.HouseDiagnostics.LogPacket(message, true);

            if (!skipEncryption)
            {
                EncryptionHelper.Encrypt(!_isCompressionEnabled, message, message, message.Length);
            }

            lock (_sendStream)
            {
                _sendStream.Enqueue(message);
            }

            Statistics.TotalBytesSent += (uint)message.Length;
            Statistics.TotalPacketsSent++;
        }

        private void ProcessEncryption(Span<byte> buffer)
        {
            if (!_isCompressionEnabled)
                return;

            EncryptionHelper.Decrypt(buffer, buffer, buffer.Length);
        }

        private async Task ProcessSendAsync(CancellationToken cancellationToken)
        {
            if (!IsConnected)
                return;

            byte[] sendingBuffer = null;
            int bytesToSend = 0;

            try
            {
                lock (_sendStream)
                {
                    if (_sendStream.Length > 0)
                    {
                        sendingBuffer = new byte[4096];

                        int size = Math.Min(sendingBuffer.Length, _sendStream.Length);

                        bytesToSend = _sendStream.Dequeue(sendingBuffer, 0, size);
                    }
                }

                if (bytesToSend > 0 && sendingBuffer != null)
                {
                    await _socket.SendAsync(sendingBuffer, 0, bytesToSend, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ProcessSendAsync: {ex}");
                Disconnected?.Invoke(this, SocketError.SocketError);
            }
        }

        private Span<byte> DecompressBuffer(Span<byte> buffer)
        {
            if (!_isCompressionEnabled)
                return buffer;

            var size = 65536;

            if (!_huffman.Decompress(buffer, _uncompressedBuffer, ref size))
            {
                _ = Disconnect();
                Disconnected?.Invoke(this, SocketError.SocketError);

                return Span<byte>.Empty;
            }

            return _uncompressedBuffer.AsSpan(0, size);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            try
            {
                _networkTask?.Wait(5000);
            }
            catch { }
            _socket?.Dispose();
        }
    }
}
