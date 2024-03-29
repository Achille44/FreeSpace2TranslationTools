﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public class MissionVariable
    {
        public string Name { get; set; }
        public string DefaultValue { get; set; }
        public string NewSexp => "\"@" + Name + "[" + DefaultValue + "]\"";
        public bool Original { get; set; }

        public MissionVariable(string name, string defaultValue, bool original = false)
        {
            Name = name;
            DefaultValue = defaultValue;
            Original = original;
        }

        /// <summary>
        /// returns the modify-variable-xstr sexp
        /// </summary>
        /// <returns></returns>
        public string ModifyVariableXstr()
        {
            string result = $"   ( modify-variable-xstr {Environment.NewLine}"
                + $"      \"@{Name}[{DefaultValue}]\" {Environment.NewLine}"
                + $"      \"{DefaultValue}\" {Environment.NewLine}"
                + $"      -1 {Environment.NewLine}"
                + $"   ){Environment.NewLine}";

            return result;
        }
    }
}
