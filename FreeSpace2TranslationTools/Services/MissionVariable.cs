using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public class MissionVariable
    {
        public string VariableName { get; set; }
        public string DefaultValue { get; set; }

        public MissionVariable(string defaultValue)
        {
            DefaultValue = defaultValue;
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
    }
}
