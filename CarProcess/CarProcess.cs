using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.Xml;
using System;
using System.DirectoryServices;
using System.Collections.Generic;
using SharpDX.Direct2D1;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using SharpDX.MediaFoundation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using SharpDX.Direct3D9;
using System.Drawing.Drawing2D;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ExtendedXmlSerializer.Configuration;
using ExtendedXmlSerializer;
using Sprache;

namespace CarProcess
{
    public class CarProcess
    {

        private UdpClient connectionServer = new UdpClient();
        private UdpClient dataServer = new UdpClient();
        private IPEndPoint dataServerEndPoint;
        private bool isConnected = false;
        private Dictionary<Point, Dictionary<Point, List<Point>>> roadPaths;
        public void Start()
        {
            using (XmlReader reader = XmlReader.Create(new StreamReader("../../../../roadPaths.xml")))
            {
                var serializer = new ConfigurationContainer()
              .UseOptimizedNamespaces() //If you want to have all namespaces in root element
              .Create();
                roadPaths = (Dictionary<Point, Dictionary<Point, List<Point>>>)serializer.Deserialize(reader);
            }
            connectionServer.Connect("localhost", 13131);
            connectionServer.Send(Encoding.ASCII.GetBytes("ConCAR"), 6);
            dataServerEndPoint = new IPEndPoint(IPAddress.Any, 0);
            var port = connectionServer.Receive(ref dataServerEndPoint);
            dataServerEndPoint.Port = BitConverter.ToInt32(port, 0);
            dataServer.Connect(dataServerEndPoint);
            isConnected = true;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] startingPos = connectionServer.Receive(ref endPoint);
            int xPos = BitConverter.ToInt32(startingPos, 0);
            int yPos = BitConverter.ToInt32(startingPos, sizeof(int));
            position = new Point(xPos, yPos);
            IPEndPoint endPoint1 = new IPEndPoint(IPAddress.Any, 0);
            byte[] destination = connectionServer.Receive(ref endPoint1);
            xPos = BitConverter.ToInt32(destination, 0);
            yPos = BitConverter.ToInt32(destination, sizeof(int));
            setDestination(new Point(xPos, yPos));
            

            Console.WriteLine("Success");
            Move();
            //Task.Factory.StartNew(ReceivePrintData);
        }

        public int turn;
        public Point position;
        public float speed = 200; //default speed
        public Vector2 speedVect;
        public Point destination;
        public Point nextJunction;
        public bool outOfMap;
        public Point Size;
        public Color color = Color.Blue;
        private Stopwatch stopwatch = new Stopwatch();

        //private void init()
        //{
        //    outOfMap = false;
        //    stopwatch.Start();
        //    rotate();
        //}

        private Queue<Point> path;

        public void setDestination(Point dest)
        {
            destination = dest;
            path = new Queue<Point>(roadPaths[position][dest]);
            path.Dequeue();
            nextJunction = path.Dequeue();
            IPEndPoint endPoint1 = new IPEndPoint(IPAddress.Any, 0);
            byte[] nextJun = new byte[2 * sizeof(int) + 3];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("NEJ"), 0, nextJun, 0, 3);
            Buffer.BlockCopy(BitConverter.GetBytes(nextJunction.X), 0, nextJun, 3, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(nextJunction.Y), 0, nextJun, 3 + sizeof(int), sizeof(int));
            dataServer.Send(nextJun, nextJun.Length);
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
            //rotate();
        }

        public int distance(Point p1, Point p2)
        {
            return Math.Abs((p1.X - p2.X) - (p1.Y - p2.Y));
        }


        public void Move()
        {
            stopwatch.Start();
            while (true)
            {
                Thread.Sleep(15);
                stopwatch.Stop();
                TimeSpan timeSpan = stopwatch.Elapsed;
                double time;

                time = timeSpan.TotalSeconds;
                stopwatch.Restart();
                stopwatch.Start();
                int prevPosX = position.X;
                int prevPosY = position.Y;
                position.X += (int)(speedVect.X * time);
                position.Y += (int)(speedVect.Y * time);
                if (Math.Sign(prevPosX - nextJunction.X) != Math.Sign(position.X - nextJunction.X) ||
                    Math.Sign(prevPosY - nextJunction.Y) != Math.Sign(position.Y - nextJunction.Y))
                {
                    Random rand = new Random();
                    try
                    {
                        int distance = (int)(nextJunction - position).ToVector2().Length();
                        position = nextJunction;
                        nextJunction = path.Dequeue();
                        IPEndPoint endPoint1 = new IPEndPoint(IPAddress.Any, 0);
                        byte[] nextJun = new byte[2 * sizeof(int) + 3];
                        Buffer.BlockCopy(Encoding.ASCII.GetBytes("NEJ"), 0, nextJun, 0, 3);
                        Buffer.BlockCopy(BitConverter.GetBytes(nextJunction.X), 0, nextJun, 3, sizeof(int));
                        Buffer.BlockCopy(BitConverter.GetBytes(nextJunction.Y), 0, nextJun, 3 + sizeof(int), sizeof(int));
                        dataServer.Send(nextJun, nextJun.Length);
                        speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
                        //rotate();
                        //setTurn();

                        if (position.X != nextJunction.X)
                        {
                            speedVect.X = Math.Sign(nextJunction.X - position.X) * speed;
                            position.X += Math.Sign(nextJunction.X - position.X) * distance;
                            speedVect.Y = 0;
                        }
                        else
                        {
                            speedVect.X = 0;
                            speedVect.Y = Math.Sign(nextJunction.Y - position.Y) * speed;
                            position.Y += Math.Sign(nextJunction.Y - position.Y) * distance;
                        }
                    }
                    catch
                    {
                        dataServer.Send(Encoding.ASCII.GetBytes("DES"), 3);
                        IPEndPoint endPoint1 = new IPEndPoint(IPAddress.Any, 0);
                        byte[] newStartDest = connectionServer.Receive(ref endPoint1);
                        int startX = BitConverter.ToInt32(newStartDest, 0);
                        int startY = BitConverter.ToInt32(newStartDest, sizeof(int));
                        int endX = BitConverter.ToInt32(newStartDest, 2*sizeof(int));
                        int endY = BitConverter.ToInt32(newStartDest, 3*sizeof(int));
                        position.X = startX;
                        position.Y = startY;
                        setDestination(new Point(endX, endY));

                        //position = startingPoints[rand.Next(startingPoints.Count)];
                        // setDestination(endPoints[rand.Next(endPoints.Count)]);
                        turn = 0;
                    }
                    
                }
                byte[] newPos = new byte[2 * sizeof(int)+3];
                Buffer.BlockCopy(Encoding.ASCII.GetBytes("POS"), 0, newPos, 0, 3);
                    Buffer.BlockCopy(BitConverter.GetBytes(position.X), 0, newPos, 3, sizeof(int));
                    Buffer.BlockCopy(BitConverter.GetBytes(position.Y), 0, newPos, 3+sizeof(int), sizeof(int));
                    dataServer.Send(newPos, newPos.Length);
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] ans = connectionServer.Receive(ref endPoint);
                    string reply = Encoding.ASCII.GetString(ans, 0, ans.Length);
                    if (reply == "NO")
                    {
                        position.X = prevPosX; position.Y = prevPosY;
                    }
                    //rotate();

            }
        }
    }
}
