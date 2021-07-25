using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public class Weapon
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public Weapon(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}
