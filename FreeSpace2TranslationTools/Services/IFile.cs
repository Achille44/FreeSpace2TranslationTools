using System.Collections.Generic;

namespace FreeSpace2TranslationTools.Services
{
    internal interface IFile
    {
        public string GetInternationalizedContent();
        public string GetInternationalizedContent(List<Weapon> modWeapons);
    }
}
