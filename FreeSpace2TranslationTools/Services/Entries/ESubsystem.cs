using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Entries
{
	internal class ESubsystem : IEntry
	{
        public string Name { get; set; }
        public string HP { get; set; }
        public string DegreeTurn { get; set; }
        public string AltSubsystemName { get; set; }
        public string AltDamagePopupSubsystemName { get; set; }
        public string TurretNameFromDefaultBank { get; set; }
        public string TurretTypeFromDefaultBank { get; set; }
		public bool IsMissileLauncher { get; set; } = false;
        public bool IsTurret { get; set; } = false;
    }
}
