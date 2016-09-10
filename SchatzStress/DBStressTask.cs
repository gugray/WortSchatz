using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using SchatzApp.Logic;

namespace SchatzStress
{
    /// <summary>
    /// Builds a huge (2mln) DB of results; measures individual storage and retrieval times at different sizes.
    /// </summary>
    internal class DBStressTask : ITaskBase
    {
        private const string encRes = "sv10-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string encSurv = "Native=yes;Age=51;NativeCountr=de;NativeEducation=higher;NativeOtherLangs=2";

        public override void Process()
        {
            string fileNameFull = Path.GetFullPath("../_work/90-dbstress.db");
            Console.WriteLine("DB file: " + fileNameFull);
            if (File.Exists(fileNameFull)) File.Delete(fileNameFull);
            DateTime date = DateTime.Now;
            Stopwatch watch = new Stopwatch();
            List<string> ids = new List<string>();
            int cycles = 10;

            Console.WriteLine("Stopwatch > Frequency: " + Stopwatch.Frequency + "; IsHighResolution: " + Stopwatch.IsHighResolution);

            using (ResultRepo rr = new ResultRepo(null, fileNameFull))
            {
                for (int i = 0; i < cycles; ++i)
                {
                    StoredResult sr = new StoredResult("NNN", date, 0, 0, 1200, encRes, encSurv);

                    // Store a single item: measure time
                    watch.Restart();
                    string uid = rr.StoreResult(sr);
                    long msStore = watch.ElapsedMilliseconds;
                    ids.Add(uid);
                    // Retrieve all UIDs
                    watch.Restart();
                    foreach (string x in ids) rr.LoadScore(x);
                    long msLoad = watch.ElapsedMilliseconds / ids.Count;
                    // Speak out
                    Console.WriteLine("Cycle " + (i + 1).ToString("00") + " > Store: " + msStore.ToString("000") + " msec; load: " + msLoad.ToString("000") + " msec");

                    // No time wasted storing the very last batch
                    if (i == cycles - 1) break;
                    // Store 50k items
                    rr.StoreBatch(sr, 50000);
                    // Next day :)
                    date = date.AddDays(1);
                }
            }
        }
    }
}
