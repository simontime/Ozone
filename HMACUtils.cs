using System.Security.Cryptography;
using System.Text;

namespace Ozone
{
    internal class HMACUtils
    {
        public static string GenerateSHA512HMAC(string Data, string Key)
        {
            var HMAC = new HMACSHA512(Encoding.ASCII.GetBytes(Key));
            return Utils.BToHexStr(HMAC.ComputeHash(Encoding.ASCII.GetBytes(Data))).ToLower();
        }
    }
}