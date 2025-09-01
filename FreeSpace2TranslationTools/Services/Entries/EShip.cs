using System.Collections.Generic;

namespace FreeSpace2TranslationTools.Services.Entries
{
	internal class EShip : IEntry
	{
        public string Name { get; set; }
        public string AltName { get; set; }
        public string DefaultType { get; set; }
        public string Type { get; set; }
		public string Maneuverability { get; set; }
        public string Armor { get; set; }
        public string Manufacturer { get; set; }
        public string Description { get; set; }
        public string TechDescription { get; set; }
        public string Length { get; set; }
        public bool IsPlayerShip { get; set; } = false;
        public List<ESubsystem> Subsystems { get; set; } = [];
    }
}
