using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services.Files
{
    internal class Medals(string content) : IFile
    {
		internal string Content { get; set; } = content;

		public string GetInternationalizedContent(bool completeInternationalization = true)
        {
            Content = Regexp.HardCodedAltNames.Replace(Content, new MatchEvaluator(XstrManager.InternationalizeHardcodedValue));

            Content = Regexp.HardcodedMedalNames.Replace(Content, new MatchEvaluator(XstrManager.GenerateAltNames));

            return Content;
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
