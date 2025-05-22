using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Plugin.Builtin {
    [Phonemizer("Indonesian CVC Phonemizer", "ID CVC", "Jules", language: "ID")]
    public class IndonesianCVCPhonemizer : SyllableBasedPhonemizer {

        private readonly string[] vowels = "a,e,3,i,o,u".Split(",");
        private readonly string[] consonants = "b,c,d,f,g,h,j,k,2,kh,l,m,n,ng,ny,p,r,s,sy,t,v,w,y,z".Split(",");
        private readonly Dictionary<string, string> aliasesFallback = new Dictionary<string, string>();
        private readonly string[] burstConsonants = "p,b,t,d,k,g,c,j".Split(",");
        private readonly Dictionary<string, string> dictionaryReplacements = "a=a;b=b;tS=c;d=d;e=e;@=3;f=f;g=g;h=h;i=i;dZ=j;k=k;?=2;x=kh;l=l;m=m;n=n;N=ng;J=ny;o=o;p=p;r=r;s=s;S=sy;t=t;u=u;v=v;w=w;j=y;z=z".Split(';')
                .Select(entry => entry.Split('='))
                .Where(parts => parts.Length == 2)
                .Where(parts => parts[0] != parts[1])
                .ToDictionary(parts => parts[0], parts => parts[1]);

        protected override string[] GetVowels() => vowels;
        protected override string[] GetConsonants() => consonants;
        protected override string GetDictionaryName() => null;
        protected override IG2p LoadBaseDictionary() => null;
        protected override Dictionary<string, string> GetAliasesFallback() => aliasesFallback;
        protected override Dictionary<string, string> GetDictionaryPhonemesReplacement() => dictionaryReplacements;

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
                // Determine if the first consonant in a cluster is short (keep it with prevV) or long (treat as onset for current V)
                // This logic might need refinement based on Indonesian phonotactics, especially for multi-consonant clusters.
                // The original Russian phonemizer had specific IsShort() logic; here we simplify.
                // If cc.Length is 1, it's a simple VCV like "V C V" -> prevV+C, C+V
                // If cc.Length > 1, e.g. "V C1 C2 V", this becomes prevV+C1, C1-, -C2+V (if C1 is not a burst consonant)
                // or prevV+C1, -C2+V (if C1 is a burst consonant, avoiding C1-).
                // The core idea is to have a CV transition for the main vowel if possible.
                
                phonemes.Add($"{prevV}{cc[0]}"); // Transition from previous vowel to first consonant of the cluster.
                
                // Handle intermediate consonants in a cluster C1 C2 ... Cn-1
                // The offset logic is to prevent adding a "-" to burst consonants if they are the first in the cluster.
                var offset = burstConsonants.Contains(cc[0]) ? 1 : 1; // Adjusted: always process from cc[0] for linkage, then handle middle ones.
                                                                    // Actually, the original logic was more about prevV+C1, then C1-, C2-, ... -CnV
                                                                    // Let's re-evaluate.
                                                                    // prevV-C1, C1-C2 (if exists), ..., Cn-V or -CnV

                // Simpler approach for Indonesian:
                // V C1 C2 V -> V-C1, C1-C2 (optional), -C2V
                // V C V     -> V-C, -CV
                
                // If it's just VCV (cc.length == 1)
                if (cc.Length == 1) {
                     // basePhoneme will be $"{cc.Last()}{v}" or $"-{cc.Last()}{v}"
                     // if prevV-C is sufficient and C-V is the next.
                     // The Russian phonemizer did: phonemes.Add($"{prevV}{cc[0]}"); basePhoneme = $"{cc.Last()}{v}";
                     // This seems fine for a simple VCV.
                } else {
                    // For V C1 C2 V:
                    // phonemes.Add($"{prevV}{cc[0]}"); // Already added: V-C1
                    // Need to add C1, C2, ... Cn-1 as standalone or connecting phonemes
                    for (var i = 0; i < cc.Length - 1; i++) {
                        var currentConsonant = cc[i];
                        var nextConsonant = cc[i+1];
                        // Attempt to make a C-C transition if available, otherwise just C-
                        string transitionPhoneme = $"{currentConsonant}{nextConsonant}";
                        if (HasOto(transitionPhoneme, syllable.tone)) {
                            phonemes.Add(transitionPhoneme);
                            i++; // Skip next consonant as it's part of C-C
                        } else {
                            phonemes.Add(currentConsonant + "-");
                        }
                    }
                }

                // Set the base phoneme for the current vowel
                // If the last consonant of the cluster should attach to V (e.g., -CV)
                // or if it's just CV.
                // The original code:
                // if (cc.Length == 1 || IsShort(syllable) || cc.Last() == "`") { basePhoneme = $"{cc.Last()}{v}"; } 
                // else { basePhoneme = $"-{cc.Last()}{v}"; }
                // Since IsShort is removed, let's simplify:
                // Always try for -CV if there are consonants, this helps with connecting to the vowel.
                basePhoneme = $"-{cc.Last()}{v}";
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
            else { // Ending VC or VCC...
                phonemes.Add($"{v}{cc[0]}-"); // V-C transition
                // Add remaining consonants with a trailing dash
                for (var i = 1; i < cc.Length; i++) {
                    var cr = $"{cc[i]}-";
                    phonemes.Add(HasOto(cr, ending.tone) ? cr : cc[i]);
                }
            }
            return phonemes;
        }

        protected override string ValidateAlias(string alias) {
            return alias;
        }

        protected override double GetTransitionBasicLengthMs(string alias = "") {
            return base.GetTransitionBasicLengthMs();
        }
    }
}
