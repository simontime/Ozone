using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.IO;
using System.Security.Cryptography;

namespace Ozone
{
    internal class AESUtils
    {
        public static byte[] DecryptECB(byte[] Data, byte[] Key, PaddingMode PadMode)
        {
            RijndaelManaged Unwrap = new RijndaelManaged
            {
                Mode = CipherMode.ECB,
                Key = Key,
                Padding = PadMode
            };
            ICryptoTransform Decrypt = Unwrap.CreateDecryptor();
            return Decrypt.TransformFinalBlock(Data, 0, Data.Length);
        }

        public static byte[] DecryptCBC(byte[] Data, byte[] Key, byte[] IV, PaddingMode PadMode)
        {
            RijndaelManaged Unwrap = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                IV = IV,
                Key = Key,
                Padding = PadMode
            };
            ICryptoTransform DecryptCBC = Unwrap.CreateDecryptor();
            return DecryptCBC.TransformFinalBlock(Data, 0, Data.Length);
        }

        public static byte[] DecryptCTR(byte[] Key, byte[] CTR, byte[] Data)
        {
            KeyParameter Params = new KeyParameter(Key);
            IBufferedCipher Cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            Cipher.Init(false, new ParametersWithIV(Params, CTR));
            return Cipher.DoFinal(Data);
        }

        public static byte[] EncryptECB(byte[] Data, byte[] Key, PaddingMode PadMode)
        {
            RijndaelManaged Unwrap = new RijndaelManaged
            {
                Mode = CipherMode.ECB,
                Key = Key,
                Padding = PadMode
            };
            ICryptoTransform EncryptECB = Unwrap.CreateEncryptor();
            return EncryptECB.TransformFinalBlock(Data, 0, Data.Length);
        }

        public static byte[] EncryptCBC(byte[] Data, byte[] Key, byte[] IV, PaddingMode PadMode)
        {
            RijndaelManaged Unwrap = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                IV = IV,
                Key = Key,
                Padding = PadMode
            };
            ICryptoTransform EncryptCBC = Unwrap.CreateEncryptor();
            return EncryptCBC.TransformFinalBlock(Data, 0, Data.Length);
        }

        public static byte[] EncryptCTR(byte[] Key, byte[] CTR, byte[] Data)
        {
            KeyParameter Params = new KeyParameter(Key);
            IBufferedCipher Cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            Cipher.Init(true, new ParametersWithIV(Params, CTR));
            return Cipher.DoFinal(Data);
        }
    }
}