using System;
using System.Collections.Generic;
using System.IO;

namespace SchatzTool
{
    internal class CloudText : TaskBase
    {
        private readonly string inFileName;
        private readonly string outFileName;

        public CloudText(string inFileName, string outFileName)
        {
            this.inFileName = inFileName;
            this.outFileName = outFileName;
        }

        private readonly List<string> words = new List<string>();

        public override void Process()
        {
            ProcessFile(inFileName, false, proc);
            using (FileStream fs = new FileStream(outFileName, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                for (int i = 0; i != words.Count; ++i)
                {
                    string word = words[i];
                    for (int j = 0; j <= words.Count - i; ++j) sw.WriteLine(word);
                }
            }
        }

        private void proc(string[] parts, Header hdr)
        {
            if (parts[2] != "a") return;
            words.Add(parts[1]);
        }
    }
}
