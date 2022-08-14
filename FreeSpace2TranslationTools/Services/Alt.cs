using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSpace2TranslationTools.Services
{
    public class Alt : MissionVariable
    {
        public List<string> Ships { get; set; }

        public Alt(string name, string defaultValue) : base(name, defaultValue)
        {
            Ships = new();
        }

        /// <summary>
        /// adds a ship using this alt
        /// </summary>
        /// <param name="name"></param>
        public void AddShip(string name)
        {
            Ships.Add(name);
        }

        /// <summary>
        /// returns the ship-change-alt-name sexp
        /// </summary>
        /// <returns></returns>
        public string ShipChangeAltName()
        {
            string result = $"   ( ship-change-alt-name {Environment.NewLine}"
                + $"      \"@{Name}[{DefaultValue}]\" {Environment.NewLine}";

            foreach (string ship in Ships)
            {
                result += $"      \"{ship}\" {Environment.NewLine}";
            }

            result += $"   ){Environment.NewLine}";

            return result;
        }
    }
}
