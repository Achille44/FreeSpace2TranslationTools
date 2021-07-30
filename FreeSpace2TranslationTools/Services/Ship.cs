using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public class Ship
    {
        public string Name { get; set; }
        public List<Subsystem> Subsystems { get; set; }

        public Ship()
        {
            Name = "";
            Subsystems = new List<Subsystem>();
        }
    }
}
