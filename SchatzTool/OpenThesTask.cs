using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace SchatzTool
{
    internal class OpenThesTask : TaskBase
    {
        /// <summary>
        /// File name for generated OT word+rank+class list in output folder.
        /// </summary>
        private const string outOTKeptFileName = "10-openthesaurus.txt";
        /// <summary>
        /// OT words that are not attested in 320k word list.
        /// </summary>
        private const string outOTDroppedFileName = "11-ot-unattested.txt";
        /// <summary>
        /// Statistics about frequncy classes in corpus, and in OT-derived word list.
        /// </summary>
        private const string outFreqClassStatsFileName = "12-freq-class-stats.txt";
        /// <summary>
        /// Base forms from corpus that are *not* included in OT-derived word list.
        /// </summary>
        private const string outCorpusOmittedFileName = "13-corpus-not-included.txt";
        /// <summary>
        /// OpenThesaurus in TXT format.
        /// </summary>
        private readonly string otFileName;
        /// <summary>
        /// 320k DeReWo Grundformliste.
        /// </summary>
        private readonly string freqFileName;
        /// <summary>
        /// Pre-generated lemmatization file.
        /// </summary>
        private readonly string lemmaFileName;
        /// <summary>
        /// Output folder.
        /// </summary>
        private readonly string outFolder;

        /// <summary>
        /// Ctor: take input file names, output folder.
        /// </summary>
        public OpenThesTask(string otFileName, string freqFileName, string lemmaFileName,
            string outFolder)
        {
            this.otFileName = otFileName;
            this.freqFileName = freqFileName;
            this.lemmaFileName = lemmaFileName;
            this.outFolder = outFolder;
        }

        /// <summary>
        /// Run the task.
        /// </summary>
        public override void Process()
        {
            // Read & process OpenThesaurus.
            ProcessFile(otFileName, false, procOT);
            // Read & process DeReWo 320k.
            ProcessFile(freqFileName, false, procFreq);
            // Process lemmatization file.
            ProcessFile(lemmaFileName, false, procLemma);
            // Add rank and class to words extracted from OT.
            infuseFreq();
            // Calculate frequency class statistics
            classStats();
            // Get base forms that are omitted from result.
            gatherOmitted();
            // Write output.
            write();
        }

        /// <summary>
        /// Write all output.
        /// </summary>
        private void write()
        {
            string fname, tmplt, line;
            // OT word list with rank & frequency from DeGeWo.
            fname = Path.Combine(outFolder, outOTKeptFileName);
            using (FileStream fs = new FileStream(fname, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                tmplt = "{0}\t{1}\t{2}";
                line = string.Format(tmplt, "orig_rank", "word", "freq_class");
                sw.WriteLine(line);
                foreach (var x in wordsWithClass)
                {
                    line = string.Format(tmplt, x.OrigRank, x.Word, x.FreqClass);
                    sw.WriteLine(line);
                }
            }
            // OT words that are not attested in DeGeWo.
            fname = Path.Combine(outFolder, outOTDroppedFileName);
            using (FileStream fs = new FileStream(fname, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                foreach (var x in otUnattested) sw.WriteLine(x);
            }
            // Frequency class statistics
            fname = Path.Combine(outFolder, outFreqClassStatsFileName);
            using (FileStream fs = new FileStream(fname, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                tmplt = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}";
                line = string.Format(tmplt, "freq_class", "corpus_count", "corpus_cumul_count",
                    "dict_count", "dict_cumul_count", "dict_coverage", "dict_cumul_coverage");
                sw.WriteLine(line);
                int corpusCumulative = 0;
                int resultCumulative = 0;
                for (int i = 0; i != corpusClassSizes.Length; ++i)
                {
                    int corpusCount = corpusClassSizes[i];
                    corpusCumulative += corpusCount;
                    int resultCount = resultClassSizes[i];
                    resultCumulative += resultCount;
                    double coveragePercent = (double)(100 * resultCount) / corpusCount;
                    if (corpusCount == 0) coveragePercent = 0;
                    double cumulCoveragePercent = (double)(100 * resultCumulative) / corpusCumulative;
                    if (corpusCumulative == 0) cumulCoveragePercent = 0;
                    line = string.Format(tmplt, i, corpusCount, corpusCumulative,
                        resultCount, resultCumulative,
                        coveragePercent.ToString("0.0000"),
                        cumulCoveragePercent.ToString("0.0000"));
                    sw.WriteLine(line);
                }
            }
            // Omitted corpus words
            fname = Path.Combine(outFolder, outCorpusOmittedFileName);
            using (FileStream fs = new FileStream(fname, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                tmplt = "{0}\t{1}\t{2}";
                line = string.Format(tmplt, "orig_rank", "word", "freq_class");
                sw.WriteLine(line);
                foreach (var x in corpusOmitted)
                {
                    line = string.Format(tmplt, x.OrigRank, x.Word, x.FreqClass);
                    sw.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// DeGeWo base forms that are omitted from OT-derived word list.
        /// </summary>
        private readonly List<WordWithClass> corpusOmitted = new List<WordWithClass>();

        /// <summary>
        /// Gather and sort list of DeGeWo base forms that are omitted from OT-derived word list.
        /// </summary>
        private void gatherOmitted()
        {
            foreach (var x in baseFormToClass)
            {
                if (wordToWWC.ContainsKey(x.Key)) continue;
                WordWithClass wwc = new WordWithClass
                {
                    Word = x.Key,
                    FreqClass = x.Value,
                    OrigRank = baseFormToRank[x.Key]
                };
                corpusOmitted.Add(wwc);
            }
            corpusOmitted.Sort((x, y) => x.OrigRank.CompareTo(y.OrigRank));
        }

        /// <summary>
        /// Number of words in each frequency class in corpus (index is class).
        /// </summary>
        private int[] corpusClassSizes;
        /// <summary>
        /// Number of words in each frequency class in result set (index is class).
        /// </summary>
        private int[] resultClassSizes;

        /// <summary>
        /// Count words in frequency classes: in DeGeWo corpus, and in our OT-derive result set.
        /// </summary>
        private void classStats()
        {
            // What's the largest class in corpus data? (29 at the time of writing, but larger data may emerge in the future.)
            int maxClass = 0;
            foreach (var x in baseFormToClass) if (x.Value > maxClass) maxClass = x.Value;
            corpusClassSizes = new int[maxClass + 1];
            resultClassSizes = new int[maxClass + 1];
            // Count items in classes: for whole corpus, and for our OT-derived result set
            foreach (var x in baseFormToClass) ++corpusClassSizes[x.Value];
            foreach (var x in wordsWithClass) ++resultClassSizes[x.FreqClass];
        }

        /// <summary>
        /// Possible lemmas for each surface form, as parsed from the lemmatization file.
        /// </summary>
        private readonly Dictionary<string, List<string>> surfToLemmas = new Dictionary<string, List<string>>();

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
        }

        /// <summary>
        /// One annotated word, with DeGeWo frequency class and rank.
        /// </summary>
        private class WordWithClass
        {
            public string Word;
            public int FreqClass;
            public int OrigRank;
        }

        /// <summary>
        /// Words from OT that are not found (even via a lemma) in DeGeWo.
        /// </summary>
        private readonly List<string> otUnattested = new List<string>();
        /// <summary>
        /// Final, sorted output: OT words infused with frequency data from DeGeWo.
        /// </summary>
        private readonly List<WordWithClass> wordsWithClass = new List<WordWithClass>();
        /// <summary>
        /// Maps from word to annotation.
        /// </summary>
        private readonly Dictionary<string, WordWithClass> wordToWWC = new Dictionary<string, WordWithClass>();

        /// <summary>
        /// Records OT word with the provided class+rank, or updates earlier class+rank if this one's lower.
        /// </summary>
        private void addUpdateWordWithClass(string word, int freqClass, int origRank)
        {
            // Not recorded before: add.
            if (!wordToWWC.ContainsKey(word))
            {
                WordWithClass wwc = new WordWithClass
                {
                    Word = word,
                    FreqClass = freqClass,
                    OrigRank = origRank
                };
                wordToWWC[word] = wwc;
                wordsWithClass.Add(wwc);
            }
            // Seen before: update if new class is lower.
            else if (freqClass < wordToWWC[word].FreqClass)
            {
                wordToWWC[word].FreqClass = freqClass;
                wordToWWC[word].OrigRank = origRank;
            }
        }

        /// <summary>
        /// Adds DeGeWo rank+class info to words extracted from OT. For unattested words, goes through lemmatized form.
        /// </summary>
        private void infuseFreq()
        {
            foreach (string word in otWords)
            {
                // OT word shows up "as is" in base form list: infuse with frequency class
                if (baseFormToClass.ContainsKey(word))
                {
                    addUpdateWordWithClass(word, baseFormToClass[word], baseFormToRank[word]);
                    continue;
                }
                // Hmm, not there. Can we lemmatize it and find it that way?
                string chosenLemma = null;
                int chosenClass = int.MaxValue;
                int chosenRank = int.MaxValue;
                // We have lemmas for this surface form
                if (surfToLemmas.ContainsKey(word))
                {
                    List<string> lemmas = surfToLemmas[word];
                    // Try all lemmas, pick one with lowest class (i.e., most frequent).
                    foreach (string lemma in lemmas)
                    {
                        if (baseFormToClass.ContainsKey(lemma) && baseFormToClass[lemma] < chosenClass)
                        {
                            chosenLemma = lemma;
                            chosenClass = baseFormToClass[lemma];
                            chosenRank = baseFormToRank[lemma];
                        }
                    }
                }
                // Add with chosen lemma and frequency class
                if (chosenLemma != null) addUpdateWordWithClass(chosenLemma, chosenClass, chosenRank);
                // Not even via lemma: we got no frequency.
                else otUnattested.Add(word);
            }
            // Sort the whole shebang by rank
            wordsWithClass.Sort((x, y) => x.OrigRank.CompareTo(y.OrigRank));
        }

        /// <summary>
        /// State while processing lines of frequency file: keeps track of current index, i.e., rank.
        /// </summary>
        private int currentRank = 0;
        /// <summary>
        /// For each base form, records frequency class.
        /// </summary>
        private readonly Dictionary<string, int> baseFormToClass = new Dictionary<string, int>();
        /// <summary>
        /// For each base form, records rank in input.
        /// </summary>
        private readonly Dictionary<string, int> baseFormToRank = new Dictionary<string, int>();

        /// <summary>
        /// Processes one line of the DeGeWo 320k frequency file.
        /// </summary>
        private void procFreq(string[] parts, Header hdr)
        {
            ++currentRank;
            string[] fields = parts[0].Split(' ');
            string lemma = fields[0];
            int freqClass = int.Parse(fields[1]);
            if (!baseFormToClass.ContainsKey(lemma)) baseFormToClass[lemma] = freqClass;
            else if (freqClass < baseFormToClass[lemma]) throw new Exception("Frequency classes must be decreasing monotonously.");
            if (!baseFormToRank.ContainsKey(lemma)) baseFormToRank[lemma] = currentRank;
        }

        /// <summary>
        /// Set of every individual word from OpenThesaurus.
        /// </summary>
        private readonly HashSet<string> otWords = new HashSet<string>();

        /// <summary>
        /// Regex to match parenthesized text at start.
        /// </summary>
        private readonly Regex reStartingParen = new Regex(@"^\([^\)]+\) *");
        /// <summary>
        /// Regex to match parenthesized text at end.
        /// </summary>
        private readonly Regex reEndingParen = new Regex(@" *\([^\)]+\)$");

        /// <summary>
        /// Processes one line of the OpenThesaurus file.
        /// </summary>
        private void procOT(string[] parts, Header hdr)
        {
            // Synset items delimited by semicolons.
            string[] fields = parts[0].Split(';');
            // Extract single-word items after trimming away parenthesized content and whitespace.
            for (int i = 0; i != fields.Length; ++i)
            {
                string itm = fields[i].Trim();
                // Trim parenthesies from start.
                while (true)
                {
                    Match m = reStartingParen.Match(itm);
                    if (!m.Success) break;
                    itm = itm.Substring(m.Length);
                }
                // Trim parentheses from end.
                while (true)
                {
                    Match m = reEndingParen.Match(itm);
                    if (!m.Success) break;
                    itm = itm.Substring(0, itm.Length - m.Length);
                }
                // Split by internal spaces
                string[] words = itm.Split(' ');
                foreach (string word in words)
                {
                    // Whatever WS that remains.
                    string trimmed = word.Trim();
                    // Non-words, multi-word expressions.
                    if (trimmed == string.Empty) continue;
                    bool hasForbidden = false;
                    foreach (char c in trimmed)
                    {
                        if (c == ' ') hasForbidden = true;
                        if (c == '-') continue;
                        if (char.IsPunctuation(c)) hasForbidden = true;
                    }
                    if (hasForbidden) continue;
                    // Cool, it's a word.
                    otWords.Add(trimmed);
                }
            }
        }
    }
}
