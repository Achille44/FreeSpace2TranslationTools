using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Campaign : IFile
    {
        private readonly string OriginalContent;

        public Campaign(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            return Regexp.HardcodedNames.Replace(OriginalContent, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));
        }

        public string GetInternationalizedContent(List<Weapon> modWeapons)
        {
            return GetInternationalizedContent();
        }

        public string GetInternationalizedContent(List<Ship> modShips)
        {
            return GetInternationalizedContent();
        }
    }
}
