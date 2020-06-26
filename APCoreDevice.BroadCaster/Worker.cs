using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace APCoreDevice.BroadCaster
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;


        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    var httpClientHandler = new HttpClientHandler();

                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

                    HttpClient client = new HttpClient(httpClientHandler);
                    client.BaseAddress = new Uri("https://MyVmIPAddressOnInternet");

                    ConfigModel req = new ConfigModel
                    {
                        CreateDate = DateTime.Now,
                        Secret = "APISECRETKEY1654656546546546546546",
                        Secret2 = "APISECRETKEY2654656546546546546546",
                        Tick = DateTime.Now.Ticks
                    };


                    var jsonReq = System.Text.Json.JsonSerializer.Serialize(req);
                    var content = new StringContent(jsonReq, Encoding.UTF8, "application/json");

                    var res = await client.PostAsync("/api/Configs/SetConfig", content);
                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static bool ValidateServerCertificate(
    object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private class ConfigModel
        {
            public long Tick { get; set; }
            public string Secret { get; set; }
            public string Secret2 { get; set; }
            public DateTime CreateDate { get; set; }
        }
    }
}
