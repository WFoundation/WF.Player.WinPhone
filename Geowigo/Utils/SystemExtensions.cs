using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geowigo.Utils
{
    public static class SystemExtensions
    {
        public static string ToNormalizedString(this Uri uri)
        {
            return uri.ToString().Replace("//", "/");
        }
    }
}
