using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services.Files
{
    internal class Rank(string originalContent) : IFile
    {
        private readonly string OriginalContent = originalContent;

		public string GetInternationalizedContent(bool completeInternationalization = true)
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
