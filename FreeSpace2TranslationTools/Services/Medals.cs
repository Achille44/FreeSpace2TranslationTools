using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Medals : IFile
    {
        private readonly string OriginalContent;

        public Medals(string originalContent)
        {
            OriginalContent = originalContent;
        }

        public string GetInternationalizedContent()
        {
            return Regexp.HardcodedMedalNames.Replace(OriginalContent, new MatchEvaluator(XstrManager.GenerateAltNames));
        }
    }
}
