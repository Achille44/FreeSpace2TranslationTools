using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeSpace2TranslationTools.Services
{
    public class Xstr : IXstr
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FullLine { get; set; }
        public bool Treated { get; set; } = false;
		public bool Replaceable { get; set; } = false;
        public bool UniqueId { get; set; } = false;
        public string Comments { get; set; } = "";

        public Xstr(int id, string text, string fullLine, string endOfLine)
        {
            Id = id;
            Text = text;
            FullLine = fullLine;
            SetComments(endOfLine);
		}

        public Xstr(int id, string text, FileInfo file, string fullLine, string endOfLine)
        {
            Id = id;
            Text = text;
            FileName = file.Name;
            FilePath = file.FullName;
            FullLine = fullLine;
            SetComments(endOfLine);
		}

		public string ReplaceContentWithNewXstrId(string content)
        {
            string newLine = Regexp.Xstr.Replace(FullLine, match => $"XSTR({match.Groups[1].Value}, {Id}){match.Groups[4].Value}");

            return content.Replace(FullLine, newLine);
        }

        private void SetComments(string text)
        {
            string[] comments = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (comments.Length > 0)
            {
                Comments = " ;" + comments[0];
            }
        }
    }
}
