using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSpace2TranslationTools.Services
{
    class Alt
    {
        public string VariableName { get; set; }
        public string DefaultValue { get; set; }
        public List<string> Ships { get; set; }

        public Alt (string defaultValue)
        {
            DefaultValue = defaultValue;
            Ships = new ();
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
        /// returns the modify-variable-xstr sexp
        /// </summary>
        /// <returns></returns>
        public string ModifyVariableXstr()
        {
            string result = $"   ( modify-variable-xstr {Environment.NewLine}"
                + $"      \"@{VariableName}[{DefaultValue}]\" {Environment.NewLine}"
                + $"      \"{DefaultValue}\" {Environment.NewLine}"
                + $"      -1 {Environment.NewLine}"
                + $"   ){Environment.NewLine}";

            return result;
        }

        /// <summary>
        /// returns the ship-change-alt-name sexp
        /// </summary>
        /// <returns></returns>
        public string ShipChangeAltName()
        {
            string result = $"   ( ship-change-alt-name {Environment.NewLine}"
                + $"      \"@{VariableName}[{DefaultValue}]\" {Environment.NewLine}";

            foreach (string ship in Ships)
            {
                result += $"      \"{ship}\" {Environment.NewLine}";
            }

            result += $"   ){Environment.NewLine}";

            return result;
        }
    }
}
