using System.Collections.Generic;

namespace FreeSpace2TranslationTools.Services.Entries
{
	internal class EMainhall : IEntry
	{
        public string Name { get; set; }
        public int NumResolutions { get; set; }
        public List<EMainhallResolution> MainhallResolutions { get; set; } = [];
    }
}
