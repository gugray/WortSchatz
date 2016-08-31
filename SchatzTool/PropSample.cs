using System;
using System.Collections.Generic;
using System.IO;

namespace SchatzTool
{
    internal class PropSample : TaskBase
    {
        private readonly int sampleSize;
        private readonly string outFileName;
        private readonly List<string> otWords = new List<string>();

        public PropSample(int sampleSize, string otFile, string outFolder)
        {
            this.sampleSize = sampleSize;
            outFileName = Path.Combine(outFolder, "30-propsample-" + sampleSize.ToString() + ".txt");
            ProcessFile(otFile, true, (parts, hdr) => otWords.Add(parts[1]));
        }

        public override void Process()
        {
            // Generate sampleSize equidistanced points
            // First and last ones are half a distance away from 0 and from last word
            // And, we include several items around that point so manual selection can find a word in close proximity
            int[] points = new int[sampleSize];
            int halfDist = otWords.Count / (2 * points.Length);
            for (int i = 0; i != points.Length; ++i) points[i] = halfDist + i * otWords.Count / points.Length;
            // Get batches of increasing size around each point
            // Dump them straight to file
            using (FileStream fs = new FileStream(outFileName, FileMode.Create))
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
