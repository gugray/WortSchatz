using System;
using System.Collections.Generic;
using System.IO;

namespace SchatzTool
{
    internal class PropSample : TaskBase
    {
        private readonly string ofnEq60;
        private readonly string ofnProp120A;
        private readonly List<string> otWords = new List<string>();

        public PropSample(string otFile, string outFolder)
        {
            ofnEq60 = Path.Combine(outFolder, "30-propsample-60.txt");
            ofnProp120A = Path.Combine(outFolder, "30-propsample-120xA.txt");
            ProcessFile(otFile, true, (parts, hdr) => otWords.Add(parts[1]));
        }

        public override void Process()
        {
            do60();
            do120A();
        }

        private void do40X(int[] points, int rankLo, int rankHi)
        {
            int band = rankHi - rankLo;
            int halfDist = band / (2 * points.Length);
            for (int i = 0; i != points.Length; ++i) points[i] = rankLo + halfDist + i * band / points.Length;
        }

        private void do120A()
        {
            int[] pointsA = new int[40];
            int[] pointsB = new int[40];
            int[] pointsC = new int[40];
            do40X(pointsA, 0, 9000);
            do40X(pointsB, 9001, 27000);
            do40X(pointsC, 27001, otWords.Count);
            int[] points = new int[120];
            for (int i = 0; i != 40; ++i) points[i] = pointsA[i];
            for (int i = 0; i != 40; ++i) points[i + 40] = pointsB[i];
            for (int i = 0; i != 40; ++i) points[i + 80] = pointsC[i];
            // Get batches around each point
            // Dump them straight to file
            using (FileStream fs = new FileStream(ofnProp120A, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                // Header
                string tmplt = "{0}\t{1}\t{2}\t{3}\t{4}";
                string line = string.Format(tmplt, "batch_part", "rank", "word", "choice", "note");
                sw.WriteLine(line);
                // Batch around each point
                foreach (int pt in points)
                {
                    line = string.Format(tmplt, "PX0000", pt, otWords[pt], "", "");
                    sw.WriteLine(line);
                    for (int i = 1; i != 20; ++i)
                    {
                        line = string.Format(tmplt, "P+" + i.ToString("00"), pt, otWords[pt + i], "", "");
                        sw.WriteLine(line);
                        line = string.Format(tmplt, "P-" + i.ToString("00"), pt, otWords[pt - i], "", "");
                        sw.WriteLine(line);
                    }
                }
            }
        }

        private void do60()
        {
            // Generate 60 equidistanced points
            // First and last ones are half a distance away from 0 and from last word
            // And, we include several items around that point so manual selection can find a word in close proximity
            int[] points = new int[60];
            int halfDist = otWords.Count / (2 * points.Length);
            for (int i = 0; i != points.Length; ++i) points[i] = halfDist + i * otWords.Count / points.Length;
            // Get batches of increasing size around each point
            // Dump them straight to file
            using (FileStream fs = new FileStream(ofnEq60, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                // Header
                string tmplt = "{0}\t{1}\t{2}\t{3}\t{4}";
                string line = string.Format(tmplt, "batch_part", "rank", "word", "choice", "note");
                sw.WriteLine(line);
                // Batch around each point
                foreach (int pt in points)
                {
                    // Batch size is log2 of position. Band is half for plus/minus.
                    int band = (int)(Math.Round(Math.Log(pt, 2) / 2));
                    for (int i = pt - band; i <= pt + band; ++i)
                    {
                        string batchPart = i == pt ? "point" : "";
                        line = string.Format(tmplt, batchPart, pt, otWords[i], "", "");
                        sw.WriteLine(line);
                    }
                }
            }
        }
    }
}
