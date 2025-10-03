using System.Collections.Generic;

namespace FreeSpace2TranslationTools.Services.Files
{
    internal interface IFile
    {
        public string GetInternationalizedContent(bool completeInternationalization = true);
        public string GetInternationalizedContent(List<Weapon> modWeapons);
        public string GetInternationalizedContent(List<Ship> modShips);
    }
}
