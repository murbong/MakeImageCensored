using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeImageCensored
{
    public static class Global
    {
        public static double Gamma = 0.0095d;
        public static Bitmap SourceImage;
        public static Bitmap StrokeImage;


        public static uint[] CRCTable;

        public static uint ComputeChecksum(uint init, byte[] bytes,int length)
        {
            /*
            uint c = init;
            int n = length;

            int i = 0;

            if (n > 0) do
                {
                    c = CRCTable[(c ^ (bytes[i++])) & 0xff] ^ (c >> 8);
                } while (--n>0);
            return c;*/

            uint crc = init;
            for (int i = 0; i < length; ++i)
            {
                byte index = (byte)(((crc) & 0xff) ^ bytes[i]);
                crc = (uint)((crc >> 8) ^ CRCTable[index]);
            }
            return crc;
        }

        static Global()
        {
            uint poly = 0xedb88320;
            CRCTable = new uint[256];
            uint temp = 0;
            for (uint i = 0; i < CRCTable.Length; ++i)
            {
                temp = i;
                for (int j = 8; j > 0; --j)
                {
                    if ((temp & 1) == 1)
                    {
                        temp = (uint)((temp >> 1) ^ poly);
                    }
                    else
                    {
                        temp >>= 1;
                    }
                }
                CRCTable[i] = temp;
            }
        }

    }
}
