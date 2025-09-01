namespace FreeSpace2TranslationTools.Services
{
	class Sexp
    {
        public string Formula { get; set; }
        public string BeginningOfLine { get; set; }
        public string EndOfLine { get; set; }

        public Sexp(string type, string beginningOfLine, string endOfLine)
        {
            BeginningOfLine = beginningOfLine;
            EndOfLine = endOfLine;
            Formula = type + EndOfLine;
        }

        public Sexp(string type, string beginningOfLine)
        {
            BeginningOfLine = beginningOfLine;
            EndOfLine = " \r\n";
            Formula = type + EndOfLine;
        }

        public void AddParameter(string content)
        {
            Formula += BeginningOfLine + content.Trim() + EndOfLine;
        }

        public void CloseFormula()
        {
            Formula += $"{BeginningOfLine[..^3]})";
        }
    }
}
