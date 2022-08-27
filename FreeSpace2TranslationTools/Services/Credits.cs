using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Credits : IFile
    {
        private readonly string OriginalContent;

        public Credits(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            return Regexp.HardcodedLines.Replace(OriginalContent, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));
        }

        public string GetInternationalizedContent(List<Weapon> modWeapons)
        {
            return GetInternationalizedContent();
        }
    }
}
