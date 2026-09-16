using System;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Renderer
{
    /// <summary>
    /// Notices that the graphics device has stopped accepting work, and says so once.
    ///
    /// The client has no device-lost handling and cannot get one cheaply - FNA declares
    /// GraphicsDevice.DeviceLost but never raises it, and nothing here is written to
    /// rebuild its resources against a new device. What it can do is stop pretending.
    ///
    /// Without this the failure is silent and then baffling. FNA only presents a frame
    /// if Draw returned, so once the device is gone the window keeps showing the last
    /// good frame while Update carries on - movement, sound and the network all keep
    /// working against a picture that will never change again. RenderedText catches its
    /// own texture failure and swallows it, so the client can sit like that for hours.
    /// It dies whenever something finally needs a texture on a path that does not catch,
    /// which is why the crash lands on innocent code a long way from the cause.
    ///
    /// Measured on one report: the device failed at 06:23:40 and the client ran until
    /// 08:55:03 - two and a half hours of frozen picture - before a creature that needed
    /// a new atlas page ended it. Windows' own System log had nvlddmkm Event 153 an hour
    /// before the first failure, which is the driver reporting the same thing.
    ///
    /// Sprite sizes are bounded before they reach the device, so a texture that cannot be
    /// created is not a bad request any more. It means the device is gone.
    /// </summary>
    public static class GpuDeviceWatch
    {
        private static readonly object _sync = new object();

        /// <summary>Set once the device has refused to create a texture.</summary>
        public static bool DeviceLost { get; private set; }

        /// <summary>
        /// Anything the client wants in the one snapshot written on first failure.
        /// Set by the client, which knows about uptime, scenes and profiles; this
        /// assembly does not.
        /// </summary>
        public static Func<string> ContextProvider;

        /// <summary>
        /// Raised once, on the first failure, with the text that was logged. The client
        /// uses it to tell the player and shut down instead of running on blind.
        /// </summary>
        public static event Action<string> DeviceLostDetected;

        /// <summary>
        /// Report a failure to create a GPU resource. Safe to call from anywhere and
        /// from any thread; everything after the first call is dropped.
        /// </summary>
        public static void Report(string where, Exception ex)
        {
            string summary;

            lock (_sync)
            {
                if (DeviceLost)
                {
                    return;
                }

                DeviceLost = true;

                string context;

                try
                {
                    context = ContextProvider?.Invoke() ?? "(no context)";
                }
                catch (Exception contextEx)
                {
                    // The snapshot is a diagnostic. It does not get to throw over the
                    // thing it is describing.
                    context = $"(context failed: {contextEx.Message})";
                }

                summary =
                    "The graphics device stopped accepting work and this client cannot recover it.\n"
                    + $"First refusal at: {where}\n"
                    + $"When (UTC): {DateTime.UtcNow:O}\n"
                    + $"{context}\n"
                    + $"Underlying error: {ex}";
            }

            Log.Error(summary);

            try
            {
                DeviceLostDetected?.Invoke(summary);
            }
            catch (Exception handlerEx)
            {
                Log.Error($"device-lost handler failed: {handlerEx}");
            }
        }
    }
}
