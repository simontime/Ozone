using DSharpPlus.CommandsNext;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Ozone
{
    internal class WebUtils
    {
        private static readonly string Dir = Directory.GetCurrentDirectory();

        public static bool AcceptAllCertifications(object Input, X509Certificate Cert, X509Chain Chain, System.Net.Security.SslPolicyErrors Err)
        {
            return true;
        }

        public static string MakeReq(string URL)
        {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(URL);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            StreamReader ReadResponse = new StreamReader(Response.GetResponseStream());
            return ReadResponse.ReadToEnd();
        }

        public static Stream MakeReqToAtum(string TitleID, long DeviceID, CommandContext Ctx)
        {
            var URL = $"https://atum.hac.lp1.d4c.nintendo.net/a/d/{TitleID}";
            ServicePointManager.ServerCertificateValidationCallback = AcceptAllCertifications;
            X509Certificate2 Cert = new X509Certificate2("nx_tls_client_cert.pfx", "switch");
            Ctx.RespondAsync($"Making request to {URL}...");
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(URL);
            Request.ClientCertificates.Add(Cert);
            Request.Host = "atum.hac.lp1.d4c.nintendo.net";
            Request.Accept = "*/*";
            Request.UserAgent = $"NintendoSDK Firmware/5.1.0-3.0 (platform:NX; did:{DeviceID:x}; eid:lp1)";
            Request.AddRange(0);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            Ctx.RespondAsync($"Latest title version: {Response.GetResponseHeader("X-Nintendo-Title-Version")}");
            var Strm = Response.GetResponseStream();
            var Mem = new MemoryStream();
            Strm.CopyTo(Mem);
            Strm.Dispose();
            return Mem;
        }

        public static string MakeReqToShogun(CommandContext context, string URL)
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = AcceptAllCertifications;
                X509Certificate2 Cert = new X509Certificate2("ShopN.p12", "shop");
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(URL);
                Request.ClientCertificates.Add(Cert);
                Request.Headers.Add("X-DeviceAuthorization", $"Bearer {File.ReadAllText($"{Dir}/auth/device_auth_token_93af0acb26258de9.txt")}");
                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                StreamReader ReadResponse = new StreamReader(Response.GetResponseStream());
                string Result = ReadResponse.ReadToEnd();
                return Result;
            }
            catch (WebException Web)
            {
                HttpWebResponse Error = Web.Response as HttpWebResponse;
                if (Error.StatusCode == HttpStatusCode.Forbidden)
                {
                    context.RespondAsync("Oops! The DAuth token has expired. Please wait until the bot owner refreshes the token.");
                    return "";
                }
                else
                {
                    return "{\"formal_name\":\"No results.\"}";
                }
            }
        }
    }
}