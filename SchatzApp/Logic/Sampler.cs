using System;
using System.Collections.Generic;
using System.IO;

namespace SchatzApp.Logic
{
    internal class Sampler
    {
        private static Sampler instance;
        public static Sampler Instance { get { return instance; } }
        public static void Init(string dataFileName)
        {
            instance = new Sampler(dataFileName);
        }

        private class SamplePoint
        {
            public readonly int Rank;
            public readonly string WordA;
            public readonly string WordB;
            public readonly string WordC;
            public SamplePoint(int rank, string wordA, string wordB, string wordC)
            {
                Rank = rank;
                WordA = wordA;
                WordB = wordB;
                WordC = wordC;
            }
        }

        private class RangeSample
        {
            public readonly SamplePoint[] Points = new SamplePoint[40];
            public readonly int Size;
            public readonly Dictionary<string, int> WordToIndex = new Dictionary<string, int>();
            public RangeSample(int size) { Size = size; }
        }

        private readonly RangeSample range1 = new RangeSample(9000);
        private readonly RangeSample range2 = new RangeSample(18000);
        private readonly RangeSample range3 = new RangeSample(27885);

        private Sampler(string dataFileName)
        {
            // Read sample
            List<SamplePoint> pointList = new List<SamplePoint>();
            using (FileStream fs = new FileStream(dataFileName, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                string line;
                int i = -1;
                int lastRank = -1;
                RangeSample range = range1;
                string wordA = null;
                string wordB = null;
                string wordC = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") continue;
                    string[] parts = line.Split('\t');
                    int rank = int.Parse(parts[0]);
                    if (lastRank != rank) { ++i; lastRank = rank; }
                    if (i == 40)
                    {
                        if (range == range1) range = range2;
                        else range = range3;
                        i = 0;
                    }
                    if (parts[2] == "a") wordA = parts[1];
                    if (parts[2] == "b") wordB = parts[1];
                    if (parts[2] == "c") wordC = parts[1];
                    if (wordA != null && wordB != null && wordC != null)
                    {
                        SamplePoint point = new SamplePoint(rank, wordA, wordB, wordC);
                        range.Points[i] = point;
                        range.WordToIndex[wordA] = i;
                        range.WordToIndex[wordB] = i;
                        range.WordToIndex[wordC] = i;
                        wordA = wordB = wordC = null;
                    }
                }
            }
        }

        private static void permutate(string[] arr, Random rnd)
        {
            for (int i = arr.Length - 1; i > 0; --i)
            {
                int swapIndex = rnd.Next(i + 1);
                string tmp = arr[i];
                arr[i] = arr[swapIndex];
                arr[swapIndex] = tmp;
            }
        }

        public void GetPermutatedSample(out string[] list1, out string[] list2)
        {
            // Get random permutation of indexes
            Random rnd = new Random();
            // Make three samples: randomly choose A, B or C from each range
            string[] sample1 = new string[40];
            string[] sample2 = new string[40];
            string[] sample3 = new string[40];
            for (int i = 0; i != 40; ++i)
            {
                int x = rnd.Next(3);
                if (x == 0) sample1[i] = range1.Points[i].WordA;
                else if (x == 1) sample1[i] = range1.Points[i].WordB;
                else sample1[i] = range1.Points[i].WordC;
                x = rnd.Next(3);
                if (x == 0) sample2[i] = range2.Points[i].WordA;
                else if (x == 1) sample2[i] = range2.Points[i].WordB;
                else sample2[i] = range2.Points[i].WordC;
                x = rnd.Next(3);
                if (x == 0) sample3[i] = range3.Points[i].WordA;
                else if (x == 1) sample3[i] = range3.Points[i].WordB;
                else sample3[i] = range3.Points[i].WordC;
            }
            // Permutate all three samples
            permutate(sample1, rnd);
            permutate(sample2, rnd);
            permutate(sample3, rnd);
            // Return two lists
            list1 = sample1;
            list2 = new string[80];
            for (int i = 0; i != 40; ++i)
            {
                list2[i] = sample2[i];
                list2[i + 40] = sample3[i];
            }
        }

        public static int RoundTo(int val, int prec)
        {
            return (int)(Math.Round((double)val / prec) * prec);
        }

        public void Eval(IList<string[]> qres, out int score, out char[] resCoded)
        {
            resCoded = new char[120];
            int count1 = 0;
            int count2 = 0;
            int count3 = 0;
            foreach (var x in qres)
            {
                string word = x[0];
                // Which range, which point?
                RangeSample range;
                int rangeOfs;
                if (range1.WordToIndex.ContainsKey(word)) { range = range1; rangeOfs = 0; }
                else if (range2.WordToIndex.ContainsKey(word)) { range = range2; rangeOfs = 40; }
                else if (range3.WordToIndex.ContainsKey(word)) { range = range3; rangeOfs = 80; }
                else throw new Exception("Word not in sample: " + word);
                // Which word
                int ixInRange = range.WordToIndex[word];
                SamplePoint sp = range.Points[ixInRange];
                char mark;
                if (word == sp.WordA) mark = 'a';
                else if (word == sp.WordB) mark = 'b';
                else mark = 'c';
                // Positive or negative?
                if (x[1] == "yes")
                {
                    mark = char.ToUpper(mark);
                    if (range == range1) ++count1;
                    else if (range == range2) ++count2;
                    else ++count3;
                }
                else if (x[1] != "no") throw new Exception("Invalid quiz value for word: " + x[1]);
                // Encode
                resCoded[rangeOfs + ixInRange] = mark;
            }
            // Estimate three ranges separately
            int est1 = range1.Size * count1 / range1.Points.Length;
            int est2 = range2.Size * count2 / range2.Points.Length;
            int est3 = range3.Size * count3 / range3.Points.Length;
            // Result is sum of three estimates
            score = est1 + est2 + est3;
        }
    }
}
