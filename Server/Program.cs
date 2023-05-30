using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        private UdpClient _udpConnectionServer;
        private List<IPEndPoint> _udpClients;

        private static void Main()
        {
            var program = new Program
            {
                _udpConnectionServer = new UdpClient(7777),
                _udpClients = new List<IPEndPoint>(),
            };

            var watchingTask = Task.Factory.StartNew(() => program.WatchClients());

            Console.WriteLine($"Server started at port {((IPEndPoint)program._udpConnectionServer.Client.LocalEndPoint).Port}");
        }

        private void WatchClients()
        {
            try
            {
                while (true)
                {
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] bytes = _udpConnectionServer.Receive(ref endPoint);
                    string message = Encoding.ASCII.GetString(bytes);
                    if (message.Equals("connect"))
                    {
                        Console.WriteLine($"{endPoint} connected");
                        _udpClients.Add(endPoint);
                        //_udpConnectionServer.Send(messageBack, messageBack.Length, endPoint);
                    }
                    else if (message.Equals("disconnect"))
                    {
                        _udpClients.Remove(endPoint);
                        Console.WriteLine($"{endPoint} disconnected");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }

}
