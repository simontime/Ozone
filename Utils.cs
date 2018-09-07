using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ozone
{
    public static class CollectionExtension
    {
        private static Random rng = new Random();

        public static T RandomElement<T>(this IList<T> list)
        {
            return list[rng.Next(list.Count)];
        }

        public static T RandomElement<T>(this T[] array)
        {
            return array[rng.Next(array.Length)];
        }
    }

    class Utils
    {
        public static byte[] StrToB(string In)
        {
            return Encoding.ASCII.GetBytes(In);
        }

        public static string BToHexStr(byte[] In)
        {
            return BitConverter.ToString(In).Replace("-", "");
        }

        public static byte[] HexStrToB(string Hex)
        {
            return Enumerable.Range(0, Hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(Hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
