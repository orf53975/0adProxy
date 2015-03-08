﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using UdpSessions;

namespace ServerProxy
{
    class Program
    {
        static int proxyPort = 20599;
        static int clientPort = 20595;

        static UdpListener server = new UdpListener(clientPort);

        static TcpListener hostConn;
        static NetworkStream hostStream;

        static void ReconnToHostProxy()
        {
            try
            {
                hostConn.Stop();
            }
            catch (Exception e) { }

            hostConn = new TcpListener(IPAddress.Any, proxyPort);
            hostConn.Start();
            Console.WriteLine("Waiting for host to connect");
            var hostClient = hostConn.AcceptTcpClient();
            hostStream = hostClient.GetStream();
            Console.WriteLine("Received conn from " + hostClient.Client.RemoteEndPoint.ToString());

            server.OnMessage = (session, bytes) =>
            {
                Console.WriteLine(session + "\t received " + bytes.Length);

                try
                {
                    lock (hostStream)
                    {
                        //Send the packets to the host proxy
                        hostStream.WriteInt(session.Port);
                        hostStream.WriteInt(bytes.Length);
                        hostStream.Write(bytes);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error when sending to host proxy, assuming disconnect, reconnecting");
                    ReconnToHostProxy();
                }
            };
        }

        static void Main(string[] args)
        {
            ReconnToHostProxy();

            Task.Run(() =>
            {
                while (true)
                {
                    int port = -1;
                    byte[] packet = null;
                    try
                    {
                        //Read the response from the host proxy
                        port = hostStream.ReadInt();
                        int packetSize = hostStream.ReadInt();

                        packet = hostStream.ReadBytes(packetSize);
                        Console.WriteLine(port + "\t read " + packet.Length + " response packet");
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Error when sending to host proxy, assuming disconnect, reconnecting");
                        ReconnToHostProxy();
                        continue;
                    }

                    var session = server.FindSession(port);
                    if(session == null)
                    {
                        Console.WriteLine(port + "\t received packet from session not connected to server, ignoring");
                        continue;
                    }
                    session.Send(packet);
                }
            });

            Console.Read();
        }
    }
}
