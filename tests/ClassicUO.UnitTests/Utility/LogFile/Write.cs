using System;
using System.Globalization;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.LogFile
{
    public class Write
    {
        [Theory]
        [InlineData("plain ascii stays plain")]
        [InlineData("accented: café crème naïve")]
        [InlineData("box drawing: ╔═╗ ╚═╝")]
        [InlineData("cjk: ウルティマオンライン")]
        [InlineData("astral: \U0001F600 after the surrogate pair")]
        public void Message_Should_RoundTrip_Whatever_Its_Encoded_Length(string message)
        {
            // The buffer used to be sized and written by message.Length, the character
            // count. UTF-8 spends more than one byte on everything above ASCII, so each
            // of these encodes longer than it reads and the line came back cut short.
            string directory = Path.Combine(Path.GetTempPath(), nameof(Message_Should_RoundTrip_Whatever_Its_Encoded_Length) + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                string path;

                using (var log = new ClassicUO.Utility.Logging.LogFile(directory, "test.log"))
                {
                    path = log.ToString();
                    log.Write(message);
                }

                File.ReadAllText(path, Encoding.UTF8)
                    .Should()
                    .Be(message + "\n");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        public void FileName_Should_Use_The_24_Hour_Clock()
        {
            // hh is the 12-hour clock and nothing printed AM/PM, so an afternoon session
            // named its file for the morning and appended into it. Parsing the name back
            // with HH and comparing against now catches that: at 14:30 the old format
            // produced "02-30", which parses cleanly but lands twelve hours away.
            //
            // Before noon the two formats agree, so this can only fail in the afternoon.
            // It is still worth having - it never passes wrongly, and half the day it is
            // a real check.
            string directory = Path.Combine(Path.GetTempPath(), nameof(FileName_Should_Use_The_24_Hour_Clock) + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                DateTime before = DateTime.Now;

                using (var log = new ClassicUO.Utility.Logging.LogFile(directory, "test.log"))
                {
                    string name = Path.GetFileName(log.ToString());
                    string stamp = name.Substring(0, name.Length - "_test.log".Length);

                    DateTime parsed = DateTime.ParseExact(stamp, "yyyy-MM-dd_HH-mm-ss-fff", CultureInfo.InvariantCulture);

                    Math.Abs((parsed - before).TotalMinutes).Should().BeLessThan(1);
                }
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
