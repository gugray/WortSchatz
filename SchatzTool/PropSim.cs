using System;
using System.IO;

namespace SchatzTool
{
    internal class PropSim : TaskBase
    {
        private readonly int sampleSize;
        private readonly int dictSize;
        private readonly string outFileName;

        public PropSim(int sampleSize, int dictSize, string outFileName)
        {
            this.sampleSize = sampleSize;
            this.dictSize = dictSize;
            this.outFileName = outFileName;
        }

        private class Outcome
        {
            public int CntCorrect;
            public int Estimate;
            public double Margin95Percent;
            public int InterLo95;
            public int InterHi95;
        }

        public override void Process()
        {
            Outcome[] res = new Outcome[sampleSize + 1];
            for (int i = 0; i <= sampleSize; ++i)
            {
                double prop = (double)i / sampleSize;
                double margin = 1.96D * Math.Sqrt(prop * (1 - prop) / sampleSize);
                int estimate = (int)(prop * dictSize);
                Outcome oc = new Outcome
                {
                    CntCorrect = i,
                    Estimate = estimate,
                    Margin95Percent = margin * 100,
                    InterLo95 = (int)((prop - margin) * dictSize),
                    InterHi95 = (int)((prop + margin) * dictSize),
                };
                res[i] = oc;
            }
            string tmplt, line;
            using (FileStream fs = new FileStream(outFileName, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                tmplt = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}";
                line = string.Format(tmplt, "cnt_correct", "estimate", "margin", "est_min", "est_max", "breadth");
                sw.WriteLine(line);
                foreach (var oc in res)
                {
                    line = string.Format(tmplt, oc.CntCorrect, oc.Estimate, oc.Margin95Percent.ToString("0.00"),
                        oc.InterLo95, oc.InterHi95, (oc.InterHi95 - oc.InterLo95) / 2);
                    sw.WriteLine(line);
                }
            }
        }
    }
}
