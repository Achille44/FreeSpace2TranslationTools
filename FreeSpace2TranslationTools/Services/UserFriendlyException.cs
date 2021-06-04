using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSpace2TranslationTools.Services
{
    public class UserFriendlyException: Exception
    {
        public UserFriendlyException(string message) : base(message)
        {

        }
    }
}
