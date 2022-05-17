using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    public class GameFile
    {
        public string Name { get; set; }
        public string Content { get; private set; }
        public bool Modified { get; set; }

        public GameFile(string name)
        {
            Name = name;
            Content = File.ReadAllText(name);
            Modified = false;
        }

        public void SaveContent(string newContent)
        {
            if (newContent != Content)
            { 
                Content = newContent; 
                Modified = true; 
            }
        }
    }
}
