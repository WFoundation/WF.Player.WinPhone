using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowigo.Utils
{
    public static class SystemExtensions
    {
        private static char[] _InvalidFileNameChars;
        
        static SystemExtensions()
        {
            _InvalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
        }
        
        public static string ToNormalizedString(this Uri uri)
        {
            return uri.ToString().Replace("//", "/");
        }

        public static string ReplaceInvalidFileNameChars(this string s, char newChar = '_')
        {
            string rs = s;

            foreach (char c in _InvalidFileNameChars)
            {
                rs = rs.Replace(c, newChar);
            }

            return rs;
        }
    }
}
