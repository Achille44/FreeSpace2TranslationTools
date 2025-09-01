using System.Collections.Generic;

namespace FreeSpace2TranslationTools.Services.Entries
{
	internal class EMainhallResolution : IEntry
	{
        public string Name { get; set; }
        public List<string> DoorDescriptions { get; set; } = [];
    }
}
