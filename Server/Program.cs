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
        private UdpClient _udpPaintServer;
        private List<IPEndPoint> _udpClients;
        private BlockingCollection<KeyValuePair<byte, byte[]>> _paintCollection;

        private static void Main()
        {
            var program = new Program
            {
                _udpConnectionServer = new UdpClient(7777),
                _udpPaintServer = new UdpClient(7778),
                _udpClients = new List<IPEndPoint>(),
                _paintCollection = new BlockingCollection<KeyValuePair<byte, byte[]>>(
                    new ConcurrentBag<KeyValuePair<byte, byte[]>>())
            };

            var watchingTask = Task.Factory.StartNew(() => program.WatchClients());
            var collectPaintDataTask = Task.Factory.StartNew(() => program.CollectPaintData());
            var sendPaintDataTask = Task.Factory.StartNew(() => program.SendPaintData());

            Console.WriteLine($"Server started at port {((IPEndPoint)program._udpConnectionServer.Client.LocalEndPoint).Port}");

            Task.WaitAll(watchingTask, collectPaintDataTask, sendPaintDataTask);
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
                        byte[] messageBack = BitConverter.GetBytes((short)((IPEndPoint)_udpPaintServer.Client.LocalEndPoint).Port);
                        _udpConnectionServer.Send(messageBack, messageBack.Length, endPoint);
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
