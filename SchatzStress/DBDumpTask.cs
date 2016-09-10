using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using SchatzApp.Logic;

namespace SchatzStress
{
    /// <summary>
    /// Dumps huge DB created by <see cref="DBStressTask"/>.
    /// </summary>
    internal class DBDumpTask : ITaskBase
    {
        private const string encRes = "sv10-BBBaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string encSurv = "Native=yes;Age=51;NativeCountr=de;NativeEducation=higher;NativeOtherLangs=2";
        private int finished = 0;

        public override void Process()
        {
            string fileNameFull = Path.GetFullPath("../_work/90-dbstress.db");
            Console.WriteLine("DB file: " + fileNameFull);
            string outFileNameFull = Path.GetFullPath("../_work/90-dbstress-dump.txt");
            Console.WriteLine("Dump file: " + outFileNameFull);
            ResultRepo rr = new ResultRepo(null, fileNameFull);
            Console.WriteLine("Results DB loaded.");
            Stopwatch watch = new Stopwatch();
            watch.Restart();
            ThreadPool.QueueUserWorkItem(storeFun, rr);
            rr.DumpToFile(outFileNameFull);
            finished = 1;
            long msec = watch.ElapsedMilliseconds;
            Console.WriteLine("Dump completed in " + msec + " msec.");
        }

        private void storeFun(object para)
        {
            ResultRepo rr = para as ResultRepo;
            Stopwatch watch = new Stopwatch();
            Thread.Sleep(2000);
            while (finished == 0)
            {
                watch.Restart();
                StoredResult sr = new StoredResult("NNN", DateTime.Now, 0, 0, 1200, encRes, encSurv);
                rr.StoreResult(sr);
                long msec = watch.ElapsedMilliseconds;
                Console.WriteLine("Worker thread stored new result in " + msec + " msec.");
                Thread.Sleep(2000);
            }
        }
    }
}
