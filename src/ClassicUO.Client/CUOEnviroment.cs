#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ClassicUO
{
    internal static class CUOEnviroment
    {
        public static Thread GameThread;
        public static float DPIScaleFactor = 1.0f;
        public static bool NoSound;
        public static string[] Args;
        public static string[] Plugins;
        public static bool Debug;
        public static bool IsHighDPI;
        public static uint CurrentRefreshRate;
        public static bool SkipLoginScreen;
        public static bool IsOutlands;
        public static bool NoServerPing;
        public static Assembly Assembly => Assembly.GetEntryAssembly();

        public static readonly bool IsUnix = Environment.OSVersion.Platform != PlatformID.Win32NT && Environment.OSVersion.Platform != PlatformID.Win32Windows && Environment.OSVersion.Platform != PlatformID.Win32S && Environment.OSVersion.Platform != PlatformID.WinCE;

        public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// The build this binary came from: "v4.5.2301" for a release, or
        /// "v4.5.2301-dev.a1b2c3d" for a dev build, which names the commit. The assembly
        /// version alone cannot separate a dev build from the release it was cut from,
        /// which makes a log or a bug report ambiguous about which build produced it.
        /// Both workflows stamp the tag in as the informational version; a local build
        /// has none and says so.
        /// </summary>
        public static readonly string BuildTag = ReadBuildTag();

        /// <summary>
        /// The version as it is written down: "4.5.2301", three parts.
        ///
        /// Version alone renders "4.5.2301.0". Assembly metadata holds four numbers and
        /// fills the unset one with zero, so the trailing part is an artefact of the
        /// format rather than anything anyone chose. ToString(3) asks for the three that
        /// were actually set.
        /// </summary>
        public static string ShortVersion => Version.ToString(3);

        /// <summary>
        /// One short string for the login screen, where the label sits next to the
        /// ClassicUO links and has little room. The Holiday tag already carries the base
        /// version, so printing both ran the label underneath those links; this prints
        /// the tag alone when there is one, and the assembly version when there is not.
        /// </summary>
        public static string DisplayVersion => BuildTag.StartsWith("v", StringComparison.Ordinal)
            ? BuildTag.Substring(1)
            : ShortVersion;

        private static string ReadBuildTag()
        {
            try
            {
                string informational = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;

                // The leading "v" is what separates a stamped build from an unstamped one.
                // Both workflows stamp a tag ("v4.5.2301"); when nothing is stamped the SDK
                // falls back to the plain version ("4.5.2301"), with no "v" and no more use
                // than the assembly version itself.
                if (string.IsNullOrWhiteSpace(informational) || !informational.StartsWith("v", StringComparison.Ordinal))
                {
                    return "local build";
                }

                // Belt and braces: the SDK appends "+<commit sha>" unless told not to,
                // and a stale build or a different SDK may still do it.
                int plus = informational.IndexOf('+');

                return plus < 0 ? informational : informational.Substring(0, plus);
            }
            catch
            {
                return "unknown";
            }
        }
        public static readonly string ExecutablePath = 
#if NETFRAMEWORK
           Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
#else
            Environment.CurrentDirectory;
#endif
    }
}