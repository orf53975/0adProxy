﻿using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UdpSessions;

namespace UDPTest
{
    class Program
    {
        class ListenOptions
        {
            [Option('p', "port", Required = true, HelpText = "Port to listen on")]
            public int Port { get; set; }
        }

        class SendOptions
        {
            [Option('h', "host", Required = true, HelpText = "Host (ip) to send to")]
            public string Host { get; set; }

            [Option('p', "port", Required = true, HelpText = "Port to send to")]
            public int Port { get; set; }

            [Option('i', "interval", DefaultValue = 1000, HelpText = "Default frequency in milliseconds to send packets at")]
            public int Interval { get; set; }
        }

        class Options
        {
            [VerbOption("listen", HelpText = "Listen for UDP packets")]
            public ListenOptions ListenVerb { get; set; }

            [VerbOption("send", HelpText = "Send UDP packets")]
            public SendOptions SendVerb { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
        static void Main(string[] args)
        {
            Options options = new Options();

            if(args.Length == 0)
            {
                Console.WriteLine(options.GetUsage());
                return;
            }

            if (!Parser.Default.ParseArguments(args, options, (verb, subOptions) => { }))
            {
                Console.WriteLine(options.GetUsage());
                return;
            }

            Console.WriteLine(JsonConvert.SerializeObject(options));

            if(options.ListenVerb != null)
            {
                Listen(options.ListenVerb);
            }

            if (options.SendVerb != null)
            {
                Send(options.SendVerb);
            }

            while(Console.ReadLine() != "exit")
            {
                Console.WriteLine("Type exit to exit");
            }
        }

        static void Listen(ListenOptions options)
        {
            Console.WriteLine("Listening on port " + options.Port);

            UdpListener listener = new UdpListener(options.Port);
        }

        static void Send(SendOptions options)
        {
            string destName = options.Host + ":" + options.Port;
            Console.WriteLine("Sending to " + destName + " every " + options.Interval + "ms");

            Task.Run(() =>
            {
                while (true)
                {
                    UdpSender sender = new UdpSender(options.Host, options.Port);

                    sender.Send(new byte[] { 72, 73 });

                    Console.WriteLine("Sent packet to " + destName + " from " + sender.LocalEndPoint.ToString());

                    Thread.Sleep(options.Interval);
                }
            });
        }
    }
}