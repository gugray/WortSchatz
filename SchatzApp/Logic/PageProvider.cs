using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SchatzApp.Logic
{
    public class PageProvider
    {
        public class PageInfo
        {
            public readonly string Title;
            public readonly string Keywords;
            public readonly string Description;
            public readonly string Html;
            public PageInfo(string title, string keywords, string description, string html)
            {
                Title = title;
                Keywords = keywords;
                Description = description;
                Html = html;
            }
        }

        private readonly bool isDevelopment;
        private readonly Dictionary<string, PageInfo> pageDict;

        public PageProvider(bool isDevelopment)
        {
            this.isDevelopment = isDevelopment;
            pageDict = new Dictionary<string, PageInfo>();
            init();
        }

        private void init()
        {
            pageDict.Clear();
            var files = Directory.EnumerateFiles("./html");
            foreach (var fn in files)
            {
                string name = Path.GetFileName(fn);
                if (!name.EndsWith(".html")) continue;
                string rel;
                PageInfo pi = loadPage(fn, out rel);
                if (rel == null) continue;
                pageDict[rel] = pi;
            }
        }

        private readonly Regex reMetaSpan = new Regex("<span id=\"x\\-([^\"]+)\">([^<]+)<\\/span>");

        private PageInfo loadPage(string fileName, out string rel)
        {
            StringBuilder html = new StringBuilder();
            string title = string.Empty;
            string description = string.Empty;
            string keywords = string.Empty;
            rel = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match m = reMetaSpan.Match(line);
                    if (!m.Success)
                    {
                        html.AppendLine(line);
                        continue;
                    }
                    string key = m.Groups[1].Value;
                    if (key == "title") title = m.Groups[2].Value;
                    else if (key == "description") description = m.Groups[2].Value;
                    else if (key == "keywords") keywords = m.Groups[2].Value;
                    else if (key == "rel") rel = m.Groups[2].Value;
                }
            }
            return new PageInfo(title, keywords, description, html.ToString());
        }

        public PageInfo GetPage(string rel)
        {
            if (isDevelopment) init();

            if (rel == null) rel = "/";
            else
            {
                rel = rel.TrimEnd('/');
                if (rel == string.Empty) rel = "/";
                if (!rel.StartsWith("/")) rel = "/" + rel;
            }

            if (!pageDict.ContainsKey(rel)) return null;
            return pageDict[rel];
        }
    }
}
