using System;
using Nancy.Hosting.Self;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace IHCPAS.FhirApi
{

    class Program
    {
        private string _url = "http://localhost";
        private int _port = 12349;
        private readonly NancyHost _nancy;

        public Program()
        {
            var uri = new Uri($"{_url}:{_port}/");
            _nancy = new NancyHost(uri);
        }

        private void Start()
        {
            _nancy.Start();
            Console.WriteLine($"Started listening port {_port}");
            Console.ReadKey();
            _nancy.Stop();
        }

        static void Main()
        {
            var p = new Program();
            p.Start();
        }
    }
}

