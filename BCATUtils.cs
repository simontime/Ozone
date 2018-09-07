using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.IO;
using static Ozone.Utils;

namespace Ozone
{
    class BCATUtils
    {
        // Huge thanks to OatmealDome and his team for the basis behind this code! (https://github.com/OatmealDome/BcatDecryptor)

        public static string[] Keys =
{
            "a3e20c5c1cd7b720",
            "7f4c637432c8d420",
            "188d087d92a0c087",
            "8e7d23fa7fafe60f",
            "5252ae57c026d3cb",
            "2650f5e53554f01d",
            "b213a1e986307c9f",
            "875d8b01e3df5d7c",
            "c1b9a5ce866e00b1",
            "6a48ae69161e0138",
            "3f7b0401928b1f46",
            "0e9db55903a10f0e",
            "a8914bcbe7b888f9",
            "b15ef3ed6ce0e4cc",
            "f3b9d9f43dedf569",
            "bda4f7a0508c7462",
            "f5dc3586b1b2a8af",
            "7f6828b6f33dd118",
            "860de88547dcbf70",
            "ccbacacb70d11fb5",
            "b1475e5ea18151b9",
            "5f857ca15cf3374c",
            "cfa747c1d09d4f05",
            "30e7d70cb6f98101",
            "c8b3c78772bdcf43",
            "533dfc0702ed9874",
            "a29301cac5219e5c",
            "5776f5bec1b0df06",
            "1d4ab85a07ac4251",
            "7c1bd512b1cf5092",
            "2691cb8b3f76b411",
            "4400abee651c9eb9"
        };

        public static byte[] DecryptBCAT(string TitleID, string Passphrase, FileStream File)
        {
            var Rd = new BinaryReader(File);

            var Pos = Rd.BaseStream.Position;

            Pos += 5;

            int NumBits;

            var AES = Rd.ReadByte();

            if (AES == 0x1)
            {
                NumBits = 128;
            }
            else if (AES == 0x2)
            {
                NumBits = 192;
            }
            else
            {
                NumBits = 256;
            }

            Pos += 1;

            var KeyIndex = Rd.ReadByte();

            Pos += 4;

            var CTR = Rd.ReadBytes(0x10);

            Pos += 0x100;

            var Data = Rd.ReadBytes((int)(Rd.BaseStream.Length - 0x120));
            var Salt = TitleID + Keys[KeyIndex];

            var ParamGen = new Pkcs5S2ParametersGenerator(new Sha256Digest());
            ParamGen.Init(StrToB(Passphrase), StrToB(Salt), 4096);
            KeyParameter Params = (KeyParameter)ParamGen.GenerateDerivedParameters($"AES{NumBits}", NumBits);
            var Cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            Cipher.Init(false, new ParametersWithIV(Params, CTR));

            return Cipher.DoFinal(Data);
        }
    }
}
