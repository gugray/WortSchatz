using System;
using System.Collections.Generic;
using System.IO;

namespace SchatzApp.Logic
{
    internal class Sampler
    {
        private static Sampler instance;
        public static Sampler Instance { get { return instance; } }
        public static void Init(string logFolder, string dataFileName)
        {
            instance = new Sampler(logFolder, dataFileName);
        }

        private readonly string logFileName;

        private class SamplePoint
        {
            public readonly int Rank;
            public readonly string Word;
            public SamplePoint(int rank, string word)
            {
                Rank = rank;
                Word = word;
            }
        }

        private readonly SamplePoint[] points;
        private readonly int dictSize;
        private readonly Dictionary<string, int> wordToIndex = new Dictionary<string, int>();

        private Sampler(string logFolder, string dataFileName)
        {
            // Read sample
            List<SamplePoint> pointList = new List<SamplePoint>();
            using (FileStream fs = new FileStream(dataFileName, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split('\t');
                    SamplePoint sp = new SamplePoint(int.Parse(parts[0]), parts[1]);
                    wordToIndex[sp.Word] = pointList.Count;
                    pointList.Add(sp);
                }
            }
            points = pointList.ToArray();
            dictSize = points[points.Length - 1].Rank + points[0].Rank;
            // Touch log file
            logFileName = Path.Combine(logFolder, "testlog.txt");
            if (!File.Exists(logFileName)) File.CreateText(logFileName);
        }

        public string[] GetPermutatedSample()
        {
            // Get random permutation of indexes
            Random rnd = new Random();
            string[] res = new string[points.Length];
            for (int i = 0; i != res.Length; ++i) res[i] = points[i].Word;
            for (int i = res.Length - 1; i > 0; --i)
            {
                int swapIndex = rnd.Next(i + 1);
                string tmp = res[i];
                res[i] = res[swapIndex];
                res[swapIndex] = tmp;
            }
            return res;
        }

        public static int RoundTo(int val, int prec)
        {
            return (int)(Math.Round((double)val / prec) * prec);
        }

        public void Eval(string name, string[] words, out int scoreProp, out int scoreMean)
        {
            // Proportion-based score: piece of cake
            scoreProp = dictSize * words.Length / points.Length;

            // Mean-based score
            HashSet<int> positiveIndexes = new HashSet<int>();
            foreach (string word in words) positiveIndexes.Add(wordToIndex[word]);
            int[] negBelow = new int[points.Length];
            int[] posAbove = new int[points.Length];
            int count = 0;
            for (int i = 0; i < points.Length; ++i)
            {
                negBelow[i] = count;
                if (!positiveIndexes.Contains(i)) count += 1;
            }
            count = 0;
            for (int i = points.Length - 1; i >= 0; --i)
            {
                posAbove[i] = count;
                if (positiveIndexes.Contains(i)) count += 1;
            }
            int sweetPoint = -1;
            for (int i = 0; i != points.Length; ++i)
            {
                if (negBelow[i] >= posAbove[i]) { sweetPoint = i; break; }
            }
            scoreMean = points[sweetPoint].Rank;

            // Log
            lock (this)
            {
                string wordsInOne = "";
                for (int i = 0; i != words.Length; ++i)
                {
                    if (i != 0) wordsInOne += ";";
                    wordsInOne += words[i];
                }
                DateTime dt = DateTime.UtcNow;
                using (FileStream fs = new FileStream(logFileName, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    string line = "{0}-{1}-{2}!{3}:{4}\t{5}\t{6}\t{7}\t{8}";
                    line = string.Format(line, dt.Year, dt.Month.ToString("00"), dt.Day.ToString("00"),
                        dt.Hour.ToString("00"), dt.Minute.ToString("00"),
                        name, scoreProp, scoreMean, wordsInOne);
                    sw.WriteLine(line);
                }
            }
        }
    }
}
