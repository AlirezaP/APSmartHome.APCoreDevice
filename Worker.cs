using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APCoreDevice.Business.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace APCoreDevice
{
    public class Worker : BackgroundService
    {

        private static object lockObj = new object();
        private readonly ILogger<Worker> _logger;
        private static GpioController controller;
        private static long lastTimeTick = 0;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {

                while (!stoppingToken.IsCancellationRequested)
                {

                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    Console.WriteLine("In The Name Of God");


                    //.....................
                    //DefaultSettings

                    if (controller == null)
                    {
                        controller = new GpioController();
                    }

                    SetSegment(PinValue.Low, 12, PinMode.Output);

                    //.....................

                    //Certificate
                    //......................................................

                    //var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    //var certificate = new X509Certificate2(Path.Combine(@"E:\Programs (C)\SmartHome\MQTT\Server\Refrences", @"APSmartH.pfx"), "pass", X509KeyStorageFlags.Exportable);
                    //var certificate = new X509Certificate2(Path.Combine(@"E:\Programs (C)\SmartHome\MQTT\Server\Refrences", @"APSmartH.pfx"));
                    //var certificate = new X509Certificate2(Path.Combine(@"c:\\cer", @"APSmartH.pfx"), "pass", X509KeyStorageFlags.Exportable);
                    //var certificate = new X509Certificate2(Path.Combine(currentPath, @"APSmartH.pfx"));

                    // Configure MQTT server.
                    //......................................................

                    var optionsBuilder = new MqttServerOptionsBuilder()
                        .WithConnectionBacklog(100)
                        .WithDefaultEndpointPort(1414)
                        //.WithoutDefaultEndpoint() // This call disables the default unencrypted endpoint on port 1883
                        //.WithEncryptedEndpoint()
                        //.WithEncryptedEndpointPort(1413)
                        //.WithEncryptionCertificate(certificate.Export(X509ContentType.Pfx))
                        //.WithEncryptionSslProtocol(SslProtocols.Tls12)
                        .WithConnectionValidator(c =>
                        {
                            if (c.ClientId != Settings.User)
                            {
                                c.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                                return;
                            }

                            if (c.Username != Settings.User)
                            {
                                c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                                return;
                            }

                            if (c.Password != Settings.Pass)
                            {
                                c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                                return;
                            }

                            c.ReasonCode = MqttConnectReasonCode.Success;
                        })
                        .WithSubscriptionInterceptor(
                            c =>
                            {
                                c.AcceptSubscription = true;
                                Console.WriteLine(c);

                            }).WithApplicationMessageInterceptor(
                            c =>
                            {
                                lock (lockObj)
                                {
                                    c.AcceptPublish = true;

                                    try
                                    {
                                        string strReq = Encoding.UTF8.GetString(c.ApplicationMessage?.Payload);
                                        var tmp = Newtonsoft.Json.JsonConvert.DeserializeObject<SecureCommandModel>(strReq);

                                        //.................................

                                        byte[] buf = Convert.FromBase64String(tmp.Data);
       
                                        var clearData =
                                             Business.Helper.Security.DecryptStringFromBytes_Aes(
                                                 buf,
                                                 Convert.FromBase64String(Settings.shKey),
                                                 Convert.FromBase64String(Settings.shIv));


                                        var model = Newtonsoft.Json.JsonConvert.DeserializeObject<CommandModel[]>(clearData);


                                        //.................................


                                        ProcessCommand(model);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex.Message);
                                    }
                                }
                            });


                    // Start a MQTT server.
                    //......................................................

                    var mqttServer = new MqttFactory().CreateMqttServer();
                    mqttServer.StartAsync(optionsBuilder.Build()).GetAwaiter().GetResult();
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadLine();

                    // await mqttServer.StopAsync();



                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Business.DeviceException ex)
            {
                _logger.LogError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private void ProcessCommand(CommandModel[] pinActions)
        {
            pinActions = pinActions.OrderBy(p => p.Order).ToArray();


            if (pinActions != null && pinActions.Length > 0)
            {
                if (controller == null)
                {
                    controller = new GpioController();
                }


                if (pinActions != null && pinActions.Length > 0)
                {
                    foreach (var item in pinActions)
                    {
                        if (lastTimeTick > 0 && item.Tick <= lastTimeTick)
                        {
                            Console.WriteLine("Duplicate ??!!");
                            return;
                        }

                        lastTimeTick = item.Tick;

                        if (item.Tcode != "MySecretCode1##$$%^&&kjhkjhk")
                        {
                            Console.WriteLine("MqttConnectReasonCode.Banned");
                            //c.ReasonCode = MqttConnectReasonCode.Banned;
                            return;
                        }

                        //If ... Dlay Before Exec Command
                        //......................................
                        if (item.DelayBefore > 0)
                        {
                            Task.Delay(item.DelayBefore).GetAwaiter().GetResult();
                        }
                        //......................................

                        if (item.PinActions != null)
                        {
                            //DO Actions For Pins
                            //...........................................
                            foreach (var pinAct in item.PinActions)
                            {
                                var currentPinMode = GetPinMode(pinAct);

                                if (pinAct.DelayBefore > 0)
                                {
                                    Task.Delay(pinAct.DelayBefore).GetAwaiter().GetResult();
                                }

                                SetSegment(pinAct.PinVal == true ? PinValue.High : PinValue.Low, pinAct.PinNumber, currentPinMode);


                                if (pinAct.DelayAfter > 0)
                                {
                                    Task.Delay(pinAct.DelayAfter).GetAwaiter().GetResult();
                                }
                            }


                            if (item.HasReversePinVal)
                            {
                                //If ... DelayForReversePinVal Exec Command
                                //......................................
                                if (item.DelayForReversePinVal > 0)
                                {
                                    Task.Delay(item.DelayForReversePinVal).GetAwaiter().GetResult();
                                }
                                //......................................

                                //Back To Default State
                                //..............................................
                                foreach (var pinAct in item.PinActions)
                                {
                                    var currentPinMode = GetPinMode(pinAct);

                                    SetSegment(item.ReversePinVal == true ? PinValue.High : PinValue.Low, pinAct.PinNumber, currentPinMode);
                                }

                                //...............................................
                            }

                        }

                        //If ... Dlay After Exec Command
                        //......................................
                        if (item.DelayAfter > 0)
                        {
                            Task.Delay(item.DelayAfter).GetAwaiter().GetResult();
                        }
                        //......................................
                    }
                }

            }
        }

        private PinMode GetPinMode(PinCommand pinAct)
        {
            PinMode currentPinMode = PinMode.Output;
            if (pinAct.PinMode == (int)PinMode.Output)
            {
                currentPinMode = PinMode.Output;
            }

            if (pinAct.PinMode == (int)PinMode.Input)
            {
                currentPinMode = PinMode.Input;
            }

            if (pinAct.PinMode == (int)PinMode.InputPullUp)
            {
                currentPinMode = PinMode.InputPullUp;
            }

            if (pinAct.PinMode == (int)PinMode.InputPullDown)
            {
                currentPinMode = PinMode.InputPullDown;
            }

            return currentPinMode;
        }

        private void SetSegment(PinValue val, int pinnumber, PinMode mode)
        {
            try
            {
                if (!controller.IsPinOpen(pinnumber))
                {
                    controller.OpenPin(pinnumber, mode);
                }
                controller.Write(pinnumber, val);
                _logger.LogInformation($"Pin:{pinnumber.ToString()} -> {val.ToString()}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
            }
        }
    }
}
