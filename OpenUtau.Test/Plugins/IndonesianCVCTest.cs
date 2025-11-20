using OpenUtau.Api;
using OpenUtau.Plugin.Builtin;
using Xunit;
using Xunit.Abstractions;

namespace OpenUtau.Plugins {
    public class IndonesianCVCTest : PhonemizerTestBase {
        public IndonesianCVCTest(ITestOutputHelper output) : base(output) { }

        class TestIndonesianCVCPhonemizer : IndonesianCVCPhonemizer {
            protected override string GetDictionaryName() => null;
        }

        protected override Phonemizer CreatePhonemizer() {
            return new TestIndonesianCVCPhonemizer();
        }

        [Theory]
        [InlineData("en_arpa",
            new string[] { "a", "ba" },
            new string[] { "C4", "C4" },
            new string[] { "", "" },
            new string[] { "- a", "ab", "ba" })]
        [InlineData("en_arpa",
            new string[] { "a", "ka" },
            new string[] { "C4", "C4" },
            new string[] { "", "" },
            new string[] { "- a", "ak", "ka" })]
        [InlineData("en_arpa",
            new string[] { "a", "sa" },
            new string[] { "C4", "C4" },
            new string[] { "", "" },
            new string[] { "- a", "as", "sa" })]
        [InlineData("en_arpa",
            new string[] { "a", "sta" },
            new string[] { "C4", "C4" },
            new string[] { "", "" },
            new string[] { "- a", "as", "-ta" })]
        [InlineData("en_arpa",
            new string[] { "a", "ba", "-" },
            new string[] { "C4", "C4", "C4" },
            new string[] { "", "", "" },
            new string[] { "- a", "ab", "ba", "a-" })]
        public void PhonemizeTest(string singerName, string[] lyrics, string[] tones, string[] colors, string[] aliases) {
            RunPhonemizeTest(singerName, lyrics, RepeatString(lyrics.Length, ""), tones, colors, aliases);
        }
    }
}
