using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class Medals : IFile
    {
        internal string Content { get; set; }

        public Medals(string content)
        {
            Content = content;
        }

        public string GetInternationalizedContent()
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
