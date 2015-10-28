using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splunk.Logging;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace EventCollectorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true;
            };

            //Uncomment to see what happens if you use TraceData to send objects.
            //DoItTraceListener();
            DoItLogging();
            Console.ReadLine();
        }

        static async void DoItTraceListener()
        {
            var source = new TraceSource("Test");
            source.Switch.Level = SourceLevels.All;
            var listener = new HttpEventCollectorTraceListener(new Uri("https://localhost:8088"), "3E712E99-63C5-4C5A-841D-592DD070DA51");
            listener.AddLoggingFailureHandler(e => Console.WriteLine(e.Message));
            source.Listeners.Add(listener);
            source.TraceData(TraceEventType.Information, 1, new { Foo = "Bar" });
            dynamic obj = new JObject();
            obj.Bar = "Baz";
            source.TraceData(TraceEventType.Information, 1, (JObject)obj);
            await listener.FlushAsync();
        }

        static async void DoItLogging()
        {
            var middleware = new HttpEventCollectorResendMiddleware(100);
            var ecSender = new HttpEventCollectorSender(new Uri("https://localhost:8088"),
                "3E712E99-63C5-4C5A-841D-592DD070DA51",
                null,
                HttpEventCollectorSender.SendMode.Sequential,
                0,
                0,
                0,
                middleware.Plugin
            );
            ecSender.OnError += o => Console.WriteLine(o.Message);
            ecSender.Send(Guid.NewGuid().ToString(), "INFO", null, new { Foo = "Bar" });
            dynamic obj = new JObject();
            obj.Bar = "Baz";
            ecSender.Send(Guid.NewGuid().ToString(), "INFO", null, (JObject)obj);
            await ecSender.FlushAsync();
        }

        private static void EcSender_OnError(HttpEventCollectorException obj)
        {
            throw new NotImplementedException();
        }
    }
}
