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
        public string OriginalSexp { get; private set; }
        public string NewSexp
        {
            get { return BeginningOfSexp + "@" + VariableName + "[" + DefaultValue + "]" + EndOfSexp; }
        }

        public HudText(string beginningOfSexp, string defaultValue, string endOfSexp) : base(defaultValue)
        {
            OriginalSexp = beginningOfSexp + defaultValue + endOfSexp;
            BeginningOfSexp = beginningOfSexp;
            EndOfSexp = endOfSexp;
        }
    }
}
