using System;
using System.Collections.Generic;
using System.IO;

namespace SchatzTool
{
    internal class NormFreqTask : TaskBase
    {
        private readonly string freqFileName;
        private readonly string scrabbleFileName;
        private readonly string lemmaFileName;
        private readonly string normFreqFileName;
        private readonly string scrabbleFreqFileName;
        private readonly string scrabbleUnresolvedFileName;

        /// <summary>
        /// Ctor: take input and output file names.
        /// </summary>
        public NormFreqTask(string freqFileName, string scrabbleFileName, string lemmaFileName,
            string normFreqFileName, string scrabbleFreqFileName, string scrabbleUnresolvedFileName)
        {
            this.freqFileName = freqFileName;
            this.scrabbleFileName = scrabbleFileName;
            this.lemmaFileName = lemmaFileName;
            this.normFreqFileName = normFreqFileName;
            this.scrabbleFreqFileName = scrabbleFreqFileName;
            this.scrabbleUnresolvedFileName = scrabbleUnresolvedFileName;
        }

        /// <summary>
        /// Process all the input, generate results.
        /// </summary>
        public override void Process()
        {
            // Process original DeReKo 100k frequency list
            ProcessFile(freqFileName, false, procFreq);
            // Process lemmatization file
            ProcessFile(lemmaFileName, false, procLemma);
            // Process Scrabble word list
            ProcessFile(scrabbleFileName, false, procScrabble);
            // Read lemmatization file
            ProcessFile(lemmaFileName, false, procLemma);

            // Prepare flat frequency list
            flattenFreq();
            // Write normalized frequency list
            writeNormFreq();
            // Write Scrabble output
            writeScrabble();
        }

        /// <summary>
        /// Lemma + Frequency for use in flat list.
        /// </summary>
        private class FreqItem
        {
            public string Lemma;
            public int Freq;
        }

        /// <summary>
        /// Maps each observed surface form in 100k list to the lemmas they are mapped to.
        /// </summary>
        private readonly Dictionary<string, List<string>> surfToLemmasCorpus = new Dictionary<string, List<string>>();

        /// <summary>
        /// Cumulated frequency of each lemma (across all parts of speech).
        /// </summary>
        private readonly Dictionary<string, int> lemmaToFreq = new Dictionary<string, int>();

        /// <summary>
        /// Flat, sorted lemma frequency list.
        /// </summary>
        private readonly List<FreqItem> lemmaFreqList = new List<FreqItem>();

        /// <summary>
        /// Writes normalized/merged frequency list.
        /// </summary>
        private void writeNormFreq()
        {
            using (FileStream fs = new FileStream(normFreqFileName, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                string tmplt = "{0}\t{1}\t{2}\t{3}";
                string line = string.Format(tmplt, "rank", "lemma", "freq", "scrabble_count");
                sw.WriteLine(line);
                for (int i = 0; i != lemmaFreqList.Count; ++i)
                {
                    FreqItem itm = lemmaFreqList[i];
                    int scrabbleCount = 0;
                    if (scrabbleAttestations.ContainsKey(itm.Lemma)) scrabbleCount = scrabbleAttestations[itm.Lemma];
                    line = string.Format(tmplt, i + 1, itm.Lemma, itm.Freq, scrabbleCount);
                    sw.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Decides if a lemma is anomalous.
        /// </summary>
        private static bool badLemma(string lemma)
        {
            // Single-character whatever
            if (lemma.Length == 1) return true;
            // Starts or ends with punctuation
            if (char.IsPunctuation(lemma[0]) || char.IsPunctuation(lemma[lemma.Length - 1])) return true;
            // Contains digit
            foreach (char c in lemma) if (char.IsDigit(c)) return true;
            // Good lemma pat on back
            return false;
        }

        /// <summary>
        /// Flatten normalized frequencies into sorted list.
        /// </summary>
        private void flattenFreq()
        {
            lemmaFreqList.Capacity = lemmaToFreq.Count;
            foreach (var x in lemmaToFreq)
            {
                lemmaFreqList.Add(new FreqItem { Lemma = x.Key, Freq = x.Value });
            }
            lemmaFreqList.Sort((x, y) => y.Freq.CompareTo(x.Freq));
        }

        /// <summary>
        /// Processes one line of the DeReKo 100k frequency file.
        /// </summary>
        private void procFreq(string[] parts, Header hdr)
        {
            // Dirty data
            if (parts[0] == string.Empty || parts[1] == string.Empty) return;

            // Surface form and lemma
            string surf = parts[0];
            string lemma = parts[1];

            // Frequency: only integer part
            string freqStr = parts[3];
            int dotPos = freqStr.IndexOf('.');
            if (dotPos != -1) freqStr = freqStr.Substring(0, dotPos);
            int freq = int.Parse(freqStr);

            // Parts of speech we ignore
            string pos = parts[2];
            if (pos.StartsWith("$")) return; // Punctuation
            if (pos == "CARD") return; // Numbers
            if (pos == "NE") return; // Named entities
            if (pos == "XY") return; // Whatevers
            if (pos == "FM") return; // Foreign words
            if (lemma == "@ord@") return; // Numbers
            if (lemma == "UNKNOWN" || lemma == "unknown") return; // Whatevers
            if (lemma == "&amp;") return; // Whatevers
            if (badLemma(lemma)) return; // Other anomalies

            // Index: surface form to lemmas
            if (!surfToLemmasCorpus.ContainsKey(surf)) surfToLemmasCorpus[surf] = new List<string>();
            surfToLemmasCorpus[surf].Add(lemma);
            // Index: lemma to frequency (we add up frequencies of same lemma)
            if (lemmaToFreq.ContainsKey(lemma)) lemmaToFreq[lemma] += freq;
            else lemmaToFreq[lemma] = freq;
        }

        /// <summary>
        /// Possible lemmas for each surface form, as parsed from the lemmatization file.
        /// </summary>
        private readonly Dictionary<string, List<string>> surfToLemmas = new Dictionary<string, List<string>>();

        /// <summary>
        /// All known lemmas from lemmatization file.
        /// </summary>
        private readonly HashSet<string> knownLemmas = new HashSet<string>();

        /// <summary>
        /// Process one line of the lemmatization file (lemma > surface).
        /// </summary>
        private void procLemma(string[] parts, Header hdr)
        {
            string lemma = parts[0];
            string surf = parts[1];
            List<string> lemmaList;
            if (!surfToLemmas.ContainsKey(surf))
            {
                lemmaList = new List<string>();
                surfToLemmas[surf] = lemmaList;
            }
            else lemmaList = surfToLemmas[surf];
            if (!lemmaList.Contains(lemma)) lemmaList.Add(lemma);
            knownLemmas.Add(lemma);
        }

        /// <summary>
        /// Gets all possible "real" variants from an all-uppercase, non-SZ scrabble word.
        /// </summary>
        private static string[] getScrabbleVariants(string upper)
        {
            // ß variants: one or more SS replaced with ß
            List<string> szVars = new List<string>();
            szVars.Add(upper);
            // Should word start with SS, we throw
            if (upper.StartsWith("SS")) throw new Exception("Word should not start with 'SS'.");
            // Tweak SS > SZ in all variations
            if (upper.Contains("SS"))
            {
                string szall = upper.Replace("SS", "ß");
                szVars.Add(szall);
                // Get all ß positions
                List<int> poss = new List<int>();
                for (int i = 0; i != szall.Length; ++i) if (szall[i] == 'ß') poss.Add(i);
                // Only one: we're done.
                if (poss.Count == 1) { }
                // Two: two new variants, with either one or the other changed back to SS
                else if (poss.Count == 2)
                {
                    szVars.Add(szall.Substring(0, poss[0]) + "SS" + szall.Substring(poss[0] + 1));
                    szVars.Add(szall.Substring(0, poss[1]) + "SS" + szall.Substring(poss[1] + 1));
                }
                // We're not prepared for more than 2 ßs
                else throw new Exception("Not prepared for more than two SS pairs in Scrabble word. Revise code to match new Scrabble list.");
            }
            // For all variants so far, create (max) three alternatives through casing
            // All-upper, first cap, all-lower. No all-upper though for words with ß.
            List<string> capVars = new List<string>(szVars.Count * 3);
            for (int i = 0; i != szVars.Count; ++i)
            {
                string word = szVars[i];
                // All lower
                capVars.Add(word.ToLowerInvariant());
                // First cap
                capVars.Add(word.Substring(0, 1) + word.Substring(1).ToLowerInvariant());
                // All caps, i.e., original: only if word has no ß. Also, we already did this if it's a single-letter word.
                if (i == 0 && word.Length > 1) capVars.Add(word);
            }

            // Done
            return capVars.ToArray();
        }

        /// <summary>
        /// Helper class for resolved and unresolved SCRABBLE items.
        /// </summary>
        private class ScrabbleItem
        {
            /// <summary>
            /// The surface form we picked for the SCRABBLE word (the one that lead to the most frequent lemma)
            /// 
            /// </summary>
            public string Surf;
            /// <summary>
            /// The lemma we picked for the surface word (i.e., the most frequent one).
            /// </summary>
            public string Lemma;
            /// <summary>
            /// Frequency of the lemma we picked.
            /// </summary>
            public int Freq;
        }

        /// <summary>
        /// Maps from SCRABBLE word to resolution.
        /// </summary>
        private readonly Dictionary<string, ScrabbleItem> scrabbleToResolved = new Dictionary<string, ScrabbleItem>();

        /// <summary>
        /// For each corpus lemma, number of attested surface forms in Scrabble list.
        /// </summary>
        private readonly Dictionary<string, int> scrabbleAttestations = new Dictionary<string, int>();

        /// <summary>
        /// Unresolved scrabble words (no lemma found, or lemma does not occur in 100k corpus).
        /// </summary>
        private readonly List<string> scrabbleUnresolved = new List<string>();

        /// <summary>
        /// Lemmas resolved from Scrabble via lemmatization file.
        /// </summary>
        private readonly HashSet<string> knownScrabbleLemmas = new HashSet<string>();

        /// <summary>
        /// Number of Scrabble items resolved from lemmatization file.
        /// </summary>
        private int scrabbleLemmaResolvedCount = 0;

        /// <summary>
        /// Process Scrabble word list; match to lemma through surface form heuristics; find frequency
        /// </summary>
        private void procScrabble(string[] parts, Header hdr)
        {
            string scrabble = parts[0].Trim();
            if (scrabble == string.Empty) return;
            // Get all possible forms (casing, SZ)
            string[] forms = getScrabbleVariants(scrabble);
            // Look for all variants among surface forms. Find most frequent lemma.
            string lemma = null;
            string surf = null;
            int freq = 0;
            foreach (string form in forms)
            {
                List<string> lemmas = new List<string>();
                // Find legit lemmas from both lemmatization file and from 100k corpus's surface forms.
                if (surfToLemmas.ContainsKey(form))
                {
                    lemmas.AddRange(surfToLemmas[form]);
                    // Raw lemmatization - just to know
                    ++scrabbleLemmaResolvedCount;
                    foreach (string x in surfToLemmas[form]) knownScrabbleLemmas.Add(x);
                }
                if (surfToLemmasCorpus.ContainsKey(form)) lemmas.AddRange(surfToLemmasCorpus[form]);
                // Find most frequent lemma.
                foreach (string thisLemma in lemmas)
                {
                    if (!lemmaToFreq.ContainsKey(thisLemma)) continue;
                    int thisFreq = lemmaToFreq[thisLemma];
                    if (thisFreq > freq)
                    {
                        lemma = thisLemma;
                        surf = form;
                        freq = thisFreq;
                    }
                }
            }
            // File away.
            if (lemma == null) scrabbleUnresolved.Add(scrabble);
            else
            {
                ScrabbleItem itm = new ScrabbleItem { Surf = surf, Lemma = lemma, Freq = freq };
                scrabbleToResolved[scrabble] = itm;
                if (scrabbleAttestations.ContainsKey(lemma)) ++scrabbleAttestations[lemma];
                else scrabbleAttestations[lemma] = 1;
            }
        }

        /// <summary>
        /// Writes Scrabble output: resolved and unresolved words.
        /// </summary>
        private void writeScrabble()
        {
            using (FileStream fs = new FileStream(scrabbleUnresolvedFileName, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (string scrabble in scrabbleUnresolved)
                    sw.WriteLine(scrabble);
            }
        }

    }
}
