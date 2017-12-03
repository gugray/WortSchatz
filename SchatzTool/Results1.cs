using System;
using System.Collections.Generic;
using System.IO;

namespace SchatzTool
{
    internal class Results1 : TaskBase
    {
        StreamReader sr;
        StreamWriter sw;

        public Results1(string fnIn, string fnOut)
        {
            sr = new StreamReader(new FileStream(fnIn, FileMode.Open, FileAccess.Read));
            sw = new StreamWriter(new FileStream(fnOut, FileMode.Create, FileAccess.Write));
        }

        public override void Process()
        {
            string line = sr.ReadLine();
            sw.WriteLine("native\tage\tedu\tnn_yrs\totherl\tlevel\tscore");
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split('\t');
                string encSurvey = parts[6];
                if (!encSurvey.Contains("Native=")) continue;

                int prevSurveyCount = int.Parse(parts[3]);
                if (prevSurveyCount > 0) continue;

                string[] sp = encSurvey.Split(';');

                string native = "n/a";
                int age = -1;
                string edu = "n/a";
                string learnTime = "n/a";
                int otherLangs = -1;
                string langLevel = "n/a";
                foreach (string sitm in sp)
                {
                    string[] kvp = sitm.Split('=');
                    if (kvp[0] == "Native") native = kvp[1];
                    if (kvp[0] == "Age")
                    {
                        if (!int.TryParse(kvp[1], out age)) age = -1;
                    }
                    if (kvp[0] == "NnGermanTime") learnTime = kvp[1];
                    if (kvp[0] == "NnGermanLevel") langLevel = kvp[1];
                    if (kvp[0] == "NativeEducation") edu = kvp[1];
                    if (kvp[0] == "NativeOtherLangs") otherLangs = int.Parse(kvp[1]);
                }
                sw.Write(native);
                sw.Write('\t');
                sw.Write(age.ToString());
                sw.Write('\t');
                sw.Write(edu);
                sw.Write('\t');
                sw.Write(learnTime);
                sw.Write('\t');
                sw.Write(otherLangs.ToString());
                sw.Write('\t');
                sw.Write(langLevel);
                sw.Write('\t');
                sw.Write(parts[4]);
                sw.WriteLine();
            }
        }
    }
}
