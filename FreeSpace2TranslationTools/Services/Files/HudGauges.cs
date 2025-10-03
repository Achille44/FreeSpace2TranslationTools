using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services.Files
{
    internal class HudGauges(string originalContent) : IFile
    {
        private readonly string OriginalContent = originalContent;

		public string GetInternationalizedContent(bool completeInternationalization = true)
        {
            return Regexp.HardcodedTexts.Replace(OriginalContent, new MatchEvaluator(GenerateInternationalizedHudGauges));
        }

        public string GetInternationalizedContent(List<Weapon> modWeapons)
        {
            return GetInternationalizedContent();
        }

        public string GetInternationalizedContent(List<Ship> modShips)
        {
            return GetInternationalizedContent();
        }

        private string GenerateInternationalizedHudGauges(Match match)
        {
            // Always Show Text is a boolean, so don't treat this case
            if (match.Value.Contains("Always Show Text"))
            {
                return match.Value;
            }
            else
            {
                return XstrManager.ReplaceHardcodedValueWithXstr(match.Value, match.Groups[1].Value, match.Groups[2].Value);
            }
        }
    }
}
