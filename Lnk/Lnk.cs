using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnk
{
    public static class Lnk
    {

        public static LnkFile LoadFile(string lnkFile)
        {
            var raw = File.ReadAllBytes(lnkFile);

            return new LnkFile(raw, lnkFile);
        }
    }
}
