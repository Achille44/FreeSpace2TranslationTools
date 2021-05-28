using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FreeSpace2TranslationTools.Services
{
    public class Xstr
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FullLine { get; set; }
        public bool Treated { get; set; }

        public Xstr(int id, string text, string fullLine)
        {
            Id = id;
            Text = text;
            FileName = string.Empty;
            FilePath = string.Empty;
            FullLine = fullLine;
            Treated = false;
        }

        public Xstr(int id, string text, FileInfo file)
        {
            Id = id;
            Text = text;
            FileName = file.Name;
            FilePath = file.FullName;
            Treated = false;
        }

        public Xstr(int id, string text, FileInfo file, string fullLine)
        {
            Id = id;
            Text = text;
            FileName = file.Name;
            FilePath = file.FullName;
            FullLine = fullLine;
            Treated = false;
        }
    }
}
