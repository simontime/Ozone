using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Ozone
{
    // Source: https://stackoverflow.com/a/30123190

    internal class CMACUtils
    {
        public static byte[] AESEncrypt(byte[] key, byte[] iv, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                AesCryptoServiceProvider aes = new AesCryptoServiceProvider
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.None
                };
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }
        public static byte[] Rol(byte[] b)
        {
            byte[]r=new byte[b.Length];byte c=0;for(int i=b.Length-1;i>=0;i--){ushort u=(ushort)(b[i]<<1);r[i]=(byte)((u&0xff)+c);c=(byte)((u&0xff00)>>8);}return r;
        }
        public static byte[] AESCMAC(byte[] key, byte[] data)
        {
            byte[] L = AESEncrypt(key, new byte[16], new byte[16]);
            byte[] FirstSubkey = Rol(L);
            if ((L[0] & 0x80) == 0x80)
                FirstSubkey[15] ^= 0x87;
            byte[] SecondSubkey = Rol(FirstSubkey);
            if ((FirstSubkey[0] & 0x80) == 0x80)
                SecondSubkey[15] ^= 0x87;
            if (((data.Length != 0) && (data.Length % 16 == 0)) == true)
            {
                for (int j = 0; j < FirstSubkey.Length; j++)
                    data[data.Length - 16 + j] ^= FirstSubkey[j];
            }
            else
            {
                byte[] padding = new byte[16 - data.Length % 16];
                padding[0] = 0x80;
                data = data.Concat<byte>(padding.AsEnumerable()).ToArray();
                for (int j = 0; j < SecondSubkey.Length; j++)
                    data[data.Length - 16 + j] ^= SecondSubkey[j];
            }
            byte[] encResult = AESEncrypt(key, new byte[16], data);
            byte[] HashValue = new byte[16];
            Array.Copy(encResult, encResult.Length - HashValue.Length, HashValue, 0, HashValue.Length);
            return HashValue;
        }
    }
}