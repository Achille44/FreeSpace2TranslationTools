using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Entries
{
	internal class EMainhall : IEntry
	{
        public string Name { get; set; }
        public int NumResolutions { get; set; }
        public List<EMainhallResolution> MainhallResolutions { get; set; } = new();
    }
}
