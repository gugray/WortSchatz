using System;
using System.Collections.Generic;

namespace SchatzTool
{
    internal class RankSim : TaskBase
    {
        private const int dictSize = 50000;
        private const int pointCount = 120;
        private readonly string outFolder;
        private readonly Random rnd;
        private readonly int[] rangeTops = new int[] { 1000, 2000, 3000, 5000, 8000, 11000, 15000, 20000, 30000, 40000, 50000 };
        private readonly double[] propsFlatA = new double[] { 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3 };
        private readonly double[] propsSkewA = new double[] { 1.0, 0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1, 0.1 };
        private readonly double[] propsSkewB = new double[] { 0.5, 0.25, 0.12, 0.6, 0.1, 0.01, 0.01, 0.01, 0.01, 0.01, 0.01 };

        public RankSim(string outFolder)
        {
            this.outFolder = outFolder;
            this.rnd = new Random();
        }

        public override void Process()
        {
            List<int> estimates = new List<int>();
            genPointsLinear(0, dictSize, points);
            genPointsLinear(0, 1000, pointsA);
            genPointsLinear(1001, 3000, pointsB);
            genPointsLinear(3001, 9000, pointsC);
            genPointsLinear(9001, 27000, pointsD);
            genPointsLinear(27001, dictSize, pointsE);
            // Muchos FlatA trials: proportional estimate with linear points
            estimates.Clear();
            for (int i = 0; i != 1000; ++i)
            {
                genSubject(propsFlatA);
                estimates.Add(simProp());
            }
            TestEval evalFlatAProp = eval(estimates, vocabSize, "FlatA, proportion, linear");
            // Muchos SkewA trials: proportional estimate with linear points
            estimates.Clear();
            for (int i = 0; i != 1000; ++i)
            {
                genSubject(propsSkewA);
                estimates.Add(simProp());
            }
            TestEval evalSkewAProp = eval(estimates, vocabSize, "SkewA, proportion, linear");
            // Muchos SkewB trials: proportional estimate with linear points
            estimates.Clear();
            for (int i = 0; i != 1000; ++i)
            {
                genSubject(propsSkewB);
                estimates.Add(simProp());
            }
            TestEval evalSkewBProp = eval(estimates, vocabSize, "SkewB, proportion, linear");
            //// Muchos flat trials: mean rank estimate with linear points
            //estimates.Clear();
            //for (int i = 0; i != 1000; ++i)
            //{
            //    genSubject(propsFlatA);
            //    estimates.Add(simRank());
            //}
            //TestEval evalFlatAMean = eval(estimates, vocabSize, "FlatA, mean rank, linear");

            //// Muchos SkewA trials: mean rank estimate with linear points
            //estimates.Clear();
            //for (int i = 0; i != 1000; ++i)
            //{
            //    genSubject(propsSkewA);
            //    estimates.Add(simRank());
            //}
            //TestEval evalFlatAMean = eval(estimates, vocabSize, "FlatA, mean rank, linear");

            //// Muchos FlatA trials: mean rank estimate with linear points, narrow band
            //// BAD
            //genPointsLinear(10000, 20000);
            //estimates.Clear();
            //for (int i = 0; i != 1000; ++i)
            //{
            //    genSubject(propsFlatA);
            //    estimates.Add(simRank());
            //}
            //TestEval evalFlatAMeanNarrow = eval(estimates, vocabSize, "FlatA, mean rank, linear, narrow");

            // ------------------------------
            genPointsLogarithmic(4);
            // ------------------------------

            //// BAD
            //// Muchos flat trials: mean rank estimate with logarithmic points
            //estimates.Clear();
            //for (int i = 0; i != 1000; ++i)
            //{
            //    genSubject(propsFlatA);
            //    estimates.Add(simRank());
            //}
            //TestEval evalFlatAMean = eval(estimates, vocabSize, "FlatA, mean rank, linear");

            //// Muchos FlatA trials: proportional estimate with logarithmic points
            //estimates.Clear();
            //for (int i = 0; i != 1000; ++i)
            //{
            //    genSubject(propsFlatA);
            //    estimates.Add(simProp());
            //}
            //TestEval evalFlatAMeanLog = eval(estimates, vocabSize, "FlatA, proportion, logarithmic");

            //// Muchos SkewA trials: proportional estimate with logarithmic points
            //estimates.Clear();
            //for (int i = 0; i != 1000; ++i)
            //{
            //    genSubject(propsSkewA);
            //    estimates.Add(simProp());
            //}
            //TestEval evalSkewAMeanLog = eval(estimates, vocabSize, "SkewA, proportion, logarithmic");

            // Muchos FlatA trials: composite proportional estimate
            estimates.Clear();
            for (int i = 0; i != 1000; ++i)
            {
                genSubject(propsFlatA);
                estimates.Add(simPropComp());
            }
            TestEval evalFlatAPropComp = eval(estimates, vocabSize, "FlatA, proportion, composite");
            // Muchos SkewA trials: composite proportional estimate
            estimates.Clear();
            for (int i = 0; i != 1000; ++i)
            {
                genSubject(propsSkewA);
                estimates.Add(simPropComp());
            }
            TestEval evalSkewAPropComp = eval(estimates, vocabSize, "SkewA, proportion, composite");
            // Muchos SkewA trials: composite proportional estimate
            estimates.Clear();
            for (int i = 0; i != 1000; ++i)
            {
                genSubject(propsSkewB);
                estimates.Add(simPropComp());
            }
            TestEval evalSkewBPropComp = eval(estimates, vocabSize, "SkewB, proportion, composite");
        }

        private bool[] subject = new bool[dictSize];
        private int vocabSize;
        private int[] points = new int[pointCount];
        int[] pointsA = new int[24];
        int[] pointsB = new int[24];
        int[] pointsC = new int[24];
        int[] pointsD = new int[24];
        int[] pointsE = new int[24];
        private double logBase;

        private class TestEval
        {
            public string Descr;
            public int RunCount;
            public int VocabSize;
            public int AvgEstimate;
            public int AvgDiff;
            public int Range0500;
            public int Range1000;
            public int Range1500;
            public int Range2000;
            public int Range2500;
            public int Range3000;
            public int Range3500;
            public int Range4000;
            public int Range4500;
            public int RangeGreater;
        }

        private TestEval eval(List<int> estimates, int vocabSize, string descr)
        {
            TestEval res = new TestEval { Descr = descr, VocabSize = vocabSize, RunCount = estimates.Count };
            int sum = 0;
            int diffSum = 0;
            foreach (int est in estimates)
            {
                sum += est;
                int diff = Math.Abs(vocabSize - est);
                diffSum += diff;
                if (diff <= 500) ++res.Range0500;
                else if (diff < 1000) ++res.Range1000;
                else if (diff < 1500) ++res.Range1500;
                else if (diff < 2000) ++res.Range2000;
                else if (diff < 2500) ++res.Range2500;
                else if (diff < 3000) ++res.Range3000;
                else if (diff < 3500) ++res.Range3500;
                else if (diff < 4000) ++res.Range4000;
                else if (diff < 4500) ++res.Range4500;
                else ++res.RangeGreater;
            }
            res.AvgEstimate = sum / estimates.Count;
            res.AvgDiff = diffSum / estimates.Count;
            return res;
        }

        private int simPropComp()
        {
            int scoreA = 0;
            foreach (int ix in pointsA) if (subject[ix]) ++scoreA;
            int scoreB = 0;
            foreach (int ix in pointsB) if (subject[ix]) ++scoreB;
            int scoreC = 0;
            foreach (int ix in pointsC) if (subject[ix]) ++scoreC;
            int scoreD = 0;
            foreach (int ix in pointsD) if (subject[ix]) ++scoreD;
            int scoreE = 0;
            foreach (int ix in pointsE) if (subject[ix]) ++scoreE;
            return (scoreA * 1000 / pointsA.Length) +
                (scoreB * 2000 / pointsB.Length) +
                (scoreC * 6000 / pointsC.Length) +
                (scoreD * 18000 / pointsD.Length) +
                (scoreE * (dictSize - 27000) / pointsE.Length);
        }

        private int simProp()
        {
            int score = 0;
            foreach (int ix in points) if (subject[ix]) ++score;
            return (score * dictSize / pointCount);
        }

        private int simRank()
        {
            double[] negBelow = new double[pointCount];
            double[] posAbove = new double[pointCount];
            double count = 0;
            for (int i = 0; i < pointCount; ++i)
            {
                negBelow[i] = count;
                if (!subject[points[i]]) count += 1;
            }
            count = 0;
            for (int i = pointCount - 1; i >= 0; --i)
            {
                posAbove[i] = count;
                if (subject[points[i]]) count += 1;
            }
            int sweetPoint = -1;
            for (int i = 0; i != pointCount; ++i)
            {
                if (negBelow[i] >= posAbove[i]) { sweetPoint = i; break; }
            }
            return points[sweetPoint];
        }

        private void genPointsLinear(int minRank, int maxRank, int[] trg)
        {
            int band = maxRank - minRank;
            for (int i = 0; i != trg.Length; ++i)
            {
                int point = i * band / (trg.Length - 1);
                point += minRank;
                if (point == dictSize) --point;
                trg[i] = point;
            }
        }

        private void genPointsLogarithmic(double logBase)
        {
            double[] norm = new double[pointCount];
            this.logBase = logBase;
            for (int i = 0; i != pointCount; ++i)
            {
                double val = (Math.Pow(logBase, i / (double)pointCount) - 1) / (logBase - 1);
                points[i] = (int)Math.Round(val * dictSize);
            }
        }

        private void genSubject(double[] props)
        {
            // Generate known points in each range
            // We aim for an exact vocab size; in fact, an exact number of items in each range
            for (int i = 0; i != subject.Length; ++i) subject[i] = false;
            vocabSize = 0;
            for (int i = 0; i != rangeTops.Length; ++i)
            {
                int top = rangeTops[i];
                int bottom = i == 0 ? 0 : rangeTops[i - 1];
                int neededHere = (int)((top - bottom) * props[i]);
                vocabSize += neededHere;
                while (neededHere > 0)
                {
                    int point = rnd.Next(bottom, top);
                    if (subject[point]) continue;
                    subject[point] = true;
                    --neededHere;
                }
            }
        }
    }
}
