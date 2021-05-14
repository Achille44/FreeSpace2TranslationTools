using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSpace2TranslationTools.Utils
{
    public class UserFriendlyException: Exception
    {
        public UserFriendlyException(string message): base (message)
        {

        }
    }
}
