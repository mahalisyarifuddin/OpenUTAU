using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Indonesian CVC Phonemizer", "ID CVC", "Heiden.BZR", language: "ID")]
    public class IndonesianCVCPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "a,e,3,i,o,u".Split(",");
        private readonly string[] consonants = "b,c,d,f,g,h,j,k,2,kh,l,m,n,ng,ny,p,r,s,sy,t,v,w,y,z".Split(",");
        private readonly string[] burstConsonants = "p,b,t,d,k,g,c,j".Split(",");

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => "cmudict_id.txt";
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => null;

        protected override List<string> ProcessSyllable(Syllable syllable) {
            string prevV = syllable.prevV;
            string[] cc = syllable.cc;
            string v = syllable.v;

            string basePhoneme;
            var phonemes = new List<string>();
            if (syllable.IsStartingV) {
                basePhoneme = $"-{v}";
            }
            else if (syllable.IsVV) {
                if (!CanMakeAliasExtension(syllable)) {
                    basePhoneme = v;
                } else {
                    // the previous alias will be extended
                    basePhoneme = null;
                }
            }
            else if (syllable.IsStartingCV) {
                basePhoneme = $"-{cc.Last()}{v}";
                for (var i = 0; i < cc.Length - 1; i++) {
                    phonemes.Add($"-{cc[i]}");
                }
            }
            else { // VCV
                if (cc.Length == 1 || IsShort(syllable)) {
                    basePhoneme = $"{cc.Last()}{v}";
                } else {
                    basePhoneme = $"-{cc.Last()}{v}";
                }
                phonemes.Add($"{prevV}{cc[0]}"); ;
                var offset = burstConsonants.Contains(cc[0]) ? 0 : 1;
                for (var i = offset; i < cc.Length - 1; i++) {
                    var cr = $"{cc[i]}-";
                    phonemes.Add(HasOto(cr, syllable.tone) ? cr : cc[i]);
                }
            }
            phonemes.Add(basePhoneme);
            return phonemes;
        }

        protected override List<string> ProcessEnding(Ending ending) {
            string[] cc = ending.cc;
            string v = ending.prevV;

            var phonemes = new List<string>();
            if (ending.IsEndingV) {
                phonemes.Add($"{v}-");
            }
            else {
                phonemes.Add($"{v}{cc[0]}-");
                for (var i = 1; i < cc.Length; i++) {
                    var cr = $"{cc[i]}-";
                    phonemes.Add(HasOto(cr, ending.tone) ? cr : cc[i]);
                }
            }

            return phonemes;
        }
    }
}
