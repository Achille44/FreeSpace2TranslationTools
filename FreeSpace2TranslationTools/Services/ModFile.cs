using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public class ModFile
    {
        public List<string> JumpNodes { get; set; }

        public ModFile()
        {
            JumpNodes = new List<string>();
        }
    }
}
