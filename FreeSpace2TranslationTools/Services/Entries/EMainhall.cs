using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Entries
{
	internal class EMainhall
	{
        public string Name { get; set; }
        public List<string> DoorDescriptions { get; set; }

        public EMainhall() 
		{
			DoorDescriptions = new List<string>();
		}

		public bool HasContent()
		{
			return DoorDescriptions.Count > 0;
		}
	}
}
