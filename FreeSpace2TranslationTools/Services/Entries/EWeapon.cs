using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Entries
{
	internal class EWeapon
	{
        public string Name { get; set; }
        public string AltName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TechTitle { get; set; }
        public string TechDescription { get; set; }
        public string TurretName { get; set; }
        public string Type { get; set; }

        public EWeapon() { }
    }
}
