using System;
using System.Security.Cryptography;
using System.Text;

namespace EliteDangerousCompanionAPI
{
    class Program
    {

        static void Main(string[] args)
        {
            OAuth2 auth = OAuth2.Load();

            if (auth == null || !auth.Refresh())
            {
                var req = OAuth2.Authorize();
                Console.WriteLine(req.AuthURL);
                auth = req.GetAuth();
            }

            auth.Save();

            var capi = new CAPI(auth);
            var profile = capi.GetProfile();
            System.Diagnostics.Trace.WriteLine(profile.ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }
}
