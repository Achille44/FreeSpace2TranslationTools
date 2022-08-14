using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public class HudText : MissionVariable
    {
        public string BeginningOfSexp { get; set; }
        public string EndOfSexp { get; set; }
        public new string NewSexp => "@" + Name + "[" + DefaultValue + "]";

        public HudText(string name, string beginningOfSexp, string defaultValue, string endOfSexp) : base(name, defaultValue)
        {
            BeginningOfSexp = beginningOfSexp;
            EndOfSexp = endOfSexp;
        }
    }
}
