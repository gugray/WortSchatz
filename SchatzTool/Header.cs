using System.Collections.Generic;

namespace SchatzTool
{
    internal class Header
    {
        private Dictionary<string, int> dict = new Dictionary<string, int>();

        public Header(string[] parts)
        {
            for (int i = 0; i != parts.Length; ++i) dict[parts[i]] = i;
        }

        public string Get(string[] parts, string key)
        {
            return parts[dict[key]];
        }
    }
}
