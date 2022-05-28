using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeSpace2TranslationTools.Services
{
    internal class Ships : IFile
    {
        private readonly string Content;

        public Ships(string content)
        {
            Content = content;
        }

        public string GetInternationalizedContent()
        {
            throw new NotImplementedException();
        }
    }
}
