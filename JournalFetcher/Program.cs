using Microsoft.Extensions.Configuration;
using System;

namespace EliteDangerousCompanionAPI.JournalFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            var beta = false;
            var legacy = false;
            string name = null;

            foreach (var arg in args)
            {
                if (arg == "--beta")
                {
                    beta = true;
                    legacy = false;
                }
                else if (arg == "--legacy")
                {
                    legacy = true;
                    beta = false;
                }
                else if (arg == "--live")
                {
                    legacy = false;
                    beta = false;
                }
                else
                {
                    name = arg;
                }
            }

            var config =
                new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true)
                    .Build();

            OAuth2Provider provider = new OAuth2Provider(config.GetSection("OAuth2").Get<OAuth2Settings>());
            OAuth2 auth = provider.Load(name);

            Console.WriteLine("Starting");

            if (auth == null || !auth.Refresh())
            {
                var req = provider.Authorize();
                Console.WriteLine(req.AuthURL);
                auth = req.GetAuth();
            }

            auth.Save();

            var capi = new CAPI(auth, beta, legacy);
            var today = DateTime.UtcNow.Date;

            for (var day = -25; day <= 0; day++)
            {
                var date = today.AddDays(day);
                var journal = capi.GetJournalRaw(date);
                System.IO.File.WriteAllLines(
                    $"journal-{Uri.EscapeDataString(name)}-{date:yyyy-MM-dd}.log",
                    journal
                );
            }
        }
    }
}
