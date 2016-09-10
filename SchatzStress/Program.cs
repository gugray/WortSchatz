using System;
using System.Diagnostics;

namespace SchatzStress
{
    public class Program
    {
        private static void writeInfo()
        {
            Console.WriteLine("SchatzStress supports these tasks and parameters:");
            Console.WriteLine("--dbstress");
            Console.WriteLine("  ** Builds huge database of stored results and measures store/lookup times.");
        }

        private static ITaskBase parseArgs(string[] args)
        {
            if (args == null || args.Length == 0) return null;
            if (args[0] == "--dbstress") return new DBStressTask();
            if (args[0] == "--dbdump") return new DBDumpTask();
            return null;
        }

        private static void mainCore(string[] args)
        {
            ITaskBase task = parseArgs(args);
            if (task == null) { writeInfo(); return; }
            task.Process();
        }

        public static void Main(string[] args)
        {
            try { mainCore(args); }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press Enter...");
                Console.ReadLine();
            }
        }
    }
}
