using System;
using System.Collections.Generic;
using System.IO;

namespace SchatzTool
{
    internal abstract class TaskBase
    {
        public abstract void Process();

        protected delegate void ProcessLineDelegate(string[] parts, Header hdr);

        protected void ProcessFile(string fileName, bool useHeader, ProcessLineDelegate proc)
        {
            using (FileStream st = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(st))
            {
                Header hdr = null;
                string line;
                string[] parts;
                if (useHeader)
                {
                    line = sr.ReadLine();
                    parts = line.Split('\t');
                    hdr = new Header(parts);
                }
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == string.Empty) continue;
                    if (line[0] == '#') continue;
                    parts = line.Split('\t');
                    for (int i = 0; i != parts.Length; ++i) parts[i] = parts[i].Trim();
                    proc(parts, hdr);
                }
            }
        }
    }
}
