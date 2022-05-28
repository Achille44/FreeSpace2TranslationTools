using System.IO;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    internal class XstrModifyVariable : IXstr
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FullLine { get; set; }
        public bool Treated { get; set; }

        private static readonly Regex RegexModifyXstr = new("(\\(\\s*modify-variable-xstr\\s*.*?\\s*\".*?\"\\s*)(-?\\d+)(\\s*\\))", RegexOptions.Singleline | RegexOptions.Compiled);

        public XstrModifyVariable(int id, string text, FileInfo file, string fullLine)
        {
            Id = id;
            Text = text;
            FileName = file.Name;
            FilePath = file.FullName;
            FullLine = fullLine;
            Treated = false;
        }

        public string ReplaceContentWithNewXstrId(string content)
        {
            string newLine = RegexModifyXstr.Replace(FullLine, match => $"{match.Groups[1].Value}{Id}{match.Groups[3].Value}");

            return content.Replace(FullLine, newLine);
        }
    }
}
