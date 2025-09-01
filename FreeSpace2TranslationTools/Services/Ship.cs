using System.Collections.Generic;

namespace FreeSpace2TranslationTools.Services
{
    public class Ship
    {
        public string Name { get; set; }
        public List<Subsystem> Subsystems { get; set; }

        public Ship()
        {
            Name = "";
            Subsystems = [];
        }
    }
}
