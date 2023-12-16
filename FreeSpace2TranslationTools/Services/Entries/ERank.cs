using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services.Entries
{
	internal class ERank : IEntry
	{
        public string Name { get; set; }
        public string AltName { get; set; }
        public string Title { get; set; }
        public string PromotionText { get; set; }
    }
}
