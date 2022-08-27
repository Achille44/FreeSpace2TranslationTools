using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Rank : IFile
    {
        private readonly string OriginalContent;

        public Rank(string originalContent)
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
