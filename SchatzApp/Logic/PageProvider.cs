using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SchatzApp.Logic
{
    internal class PageProvider
    {
        private static PageProvider instance;
        public static PageProvider Instant { get { return instance; } }
        public static void Init(bool isDevelopment) { instance = new PageProvider(isDevelopment); }

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
        private readonly Dictionary<string, Dictionary<string, PageInfo>> langPageDict;

        private PageProvider(bool isDevelopment)
        {
            this.isDevelopment = isDevelopment;
            langPageDict = new Dictionary<string, Dictionary<string, PageInfo>>();
            init();
        }

        private readonly Regex reHtmlName = new Regex(@".+\-([^\.\-]+)\.html");

        private void init()
        {
            langPageDict.Clear();
            var files = Directory.EnumerateFiles("./html");
            foreach (var fn in files)
            {
                string name = Path.GetFileName(fn);
                Match m = reHtmlName.Match(name);
                if (!m.Success) continue;
                string lang = m.Groups[1].Value;
                string rel;
                PageInfo pi = loadPage(fn, out rel);
                if (rel == null) continue;
                if (!langPageDict.ContainsKey(lang)) langPageDict[lang] = new Dictionary<string, PageInfo>();
                langPageDict[lang][rel] = pi;
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

        public PageInfo GetPage(string lang, string rel)
        {
            if (isDevelopment) init();
            if (!langPageDict.ContainsKey(lang)) return null;
            Dictionary<string, PageInfo> x = langPageDict[lang];
            if (!x.ContainsKey(rel)) return null;
            return x[rel];
        }
    }
}
