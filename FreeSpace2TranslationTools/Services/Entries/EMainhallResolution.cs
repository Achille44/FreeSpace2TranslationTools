using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Entries
{
	internal class EMainhallResolution : IEntry
	{
        public string Name { get; set; }
        public List<string> DoorDescriptions { get; set; } = new();
    }
}
