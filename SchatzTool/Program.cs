using System;
using System.Diagnostics;
using System.IO;

namespace SchatzTool
{
    public class Program
    {
        private static void writeInfo()
        {
            Console.WriteLine("SchatzTool supports these tasks and parameters:");
            Console.WriteLine("--norm100k");
            Console.WriteLine("  <DeReKo-100k-freq-list>");
            Console.WriteLine("  <Scrabble-word-list>");
            Console.WriteLine("  <German-lemmatization-list>");
            Console.WriteLine("  <output-normalized-list>");
            Console.WriteLine("  <output-scrabble-list>");
            Console.WriteLine("  <output-unresolved-scrabble-list>");
            Console.WriteLine("  ** Normalizes raw DeReKo list");
            Console.WriteLine("--ot");
            Console.WriteLine("  <OpenThesaurus-file>");
            Console.WriteLine("  <DeReWo-320k-freq-file>");
            Console.WriteLine("  <German-lemmatization-list>");
            Console.WriteLine("  <Output-folder>");
            Console.WriteLine("  ** Extracts words from OpenThesaurus and infuses frequency classes. Emits base data for analysis.");
            Console.WriteLine("--propsim");
            Console.WriteLine("  <sample-size>");
            Console.WriteLine("  <dictionary-size>");
            Console.WriteLine("  <output-file>");
            Console.WriteLine("  ** Simulates test results with 95% confidence intervals.");
            Console.WriteLine("--ranksim");
            Console.WriteLine("  <output-folder>");
            Console.WriteLine("  ** Simulations about mean rank scoring");
            Console.WriteLine("--propsample");
            Console.WriteLine("  <enriched-OpenThesaurus-file>");
            Console.WriteLine("  <output-folder>");
            Console.WriteLine("  ** Generates samples from the OT data enriched with DeGeWo ranks.");
            Console.WriteLine("--cloudtext");
            Console.WriteLine("  <quiz-sample-file>");
            Console.WriteLine("  <output-file>");
            Console.WriteLine("  ** Generates dummy text from actual sample for word cloud.");
            Console.WriteLine("--results1");
            Console.WriteLine("  <results-file>");
            Console.WriteLine("  <output-file>");
            Console.WriteLine("  ** Creates flat tab-separated TXT from results file, keeping only first surveys.");
        }

        private static TaskBase parseArgs(string[] args)
        {
            if (args == null || args.Length == 0) return null;
            if (args[0] == "--norm100k")
            {
                if (args.Length != 7) return null;
                return new NormFreqTask(args[1], args[2], args[3], args[4], args[5], args[6]);
            }
            else if (args[0] == "--ot")
            {
                if (args.Length != 5) return null;
                return new OpenThesTask(args[1], args[2], args[3], args[4]);
            }
            else if (args[0] == "--propsim")
            {
                if (args.Length != 4) return null;
                return new PropSim(int.Parse(args[1]), int.Parse(args[2]), args[3]);
            }
            else if (args[0] == "--ranksim")
            {
                if (args.Length != 2) return null;
                return new RankSim(args[1]);
            }
            else if (args[0] == "--propsample")
            {
                if (args.Length != 3) return null;
                return new PropSample(args[1], args[2]);
            }
            else if (args[0] == "--cloudtext")
            {
                if (args.Length != 3) return null;
                return new CloudText(args[1], args[2]);
            }
            else if (args[0] == "--results1")
            {
                if (args.Length != 3) return null;
                return new Results1(args[1], args[2]);
            }
            return null;
        }

        private static void mainCore(string[] args)
        {
            TaskBase task = parseArgs(args);
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
