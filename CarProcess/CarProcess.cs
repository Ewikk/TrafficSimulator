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
        private string ipAdress = "FILL IN";
        private TcpClient connectionServer = new TcpClient();
        private TcpClient dataServer = new TcpClient();
        private bool isConnected = false;
        private Dictionary<Point, Dictionary<Point, List<Point>>> roadPaths;
        private NetworkStream connectionStream;
        private NetworkStream dataStream;
        public void Start()
        {
            using (XmlReader reader = XmlReader.Create(new StreamReader("../../../../roadPaths.xml")))
            {
                var serializer = new ConfigurationContainer()
                  .UseOptimizedNamespaces()
                  .Create();
                roadPaths = (Dictionary<Point, Dictionary<Point, List<Point>>>)serializer.Deserialize(reader);
            }

            connectionServer.Connect(ipAdress, 13131);
            connectionStream = connectionServer.GetStream();
            connectionStream.Write(Encoding.ASCII.GetBytes("ConCAR"), 0, 6);
            byte[] receiveBuffer = new byte[4];
            connectionStream.Read(receiveBuffer, 0, 4);
            int dataServerPort = BitConverter.ToInt32(receiveBuffer, 0);

            dataServer.Connect(ipAdress, dataServerPort);
            dataStream = dataServer.GetStream();

            isConnected = true;

            byte[] startingPos = new byte[8];
            dataStream.Read(startingPos, 0, 8);
            int xPos = BitConverter.ToInt32(startingPos, 0);
            int yPos = BitConverter.ToInt32(startingPos, 4);
            position = new Point(xPos, yPos);

            byte[] destination = new byte[8];
            dataStream.Read(destination, 0, 8);
            xPos = BitConverter.ToInt32(destination, 0);
            yPos = BitConverter.ToInt32(destination, 4);
            setDestination(new Point(xPos, yPos));

            Console.WriteLine("Success");
            Move();
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

            NetworkStream dataStream = dataServer.GetStream();

            byte[] nextJun = new byte[12];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("NEJ"), 0, nextJun, 0, 3);
            Buffer.BlockCopy(BitConverter.GetBytes(nextJunction.X), 0, nextJun, 3, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(nextJunction.Y), 0, nextJun, 7, 4);
            dataStream.Write(nextJun, 0, nextJun.Length);

            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
        }


        public int distance(Point p1, Point p2)
        {
            return Math.Abs((p1.X - p2.X) - (p1.Y - p2.Y));
        }


        public void Move()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (true)
            {
                Thread.Sleep(15);

                stopwatch.Stop();
                TimeSpan timeSpan = stopwatch.Elapsed;
                double time = timeSpan.TotalSeconds;
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

                        byte[] nextJun = new byte[12];
                        Buffer.BlockCopy(Encoding.ASCII.GetBytes("NEJ"), 0, nextJun, 0, 3);
                        Buffer.BlockCopy(BitConverter.GetBytes(nextJunction.X), 0, nextJun, 3, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(nextJunction.Y), 0, nextJun, 7, 4);
                        dataStream.Write(nextJun, 0, nextJun.Length);

                        speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);

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
                        dataStream.Write(Encoding.ASCII.GetBytes("DES"), 0, 3);

                        byte[] newStartDest = new byte[16];
                        dataStream.Read(newStartDest, 0, 16);
                        int startX = BitConverter.ToInt32(newStartDest, 0);
                        int startY = BitConverter.ToInt32(newStartDest, 4);
                        int endX = BitConverter.ToInt32(newStartDest, 8);
                        int endY = BitConverter.ToInt32(newStartDest, 12);
                        position.X = startX;
                        position.Y = startY;
                        setDestination(new Point(endX, endY));

                        turn = 0;
                    }
                }

                byte[] newPos = new byte[12];
                Buffer.BlockCopy(Encoding.ASCII.GetBytes("POS"), 0, newPos, 0, 3);
                Buffer.BlockCopy(BitConverter.GetBytes(position.X), 0, newPos, 3, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(position.Y), 0, newPos, 7, 4);
                dataStream.Write(newPos, 0, newPos.Length);

                byte[] ans = new byte[2];
                int retries = 5;
                string reply = "";
                while (retries-- > 0)
                {
                    if (dataStream.DataAvailable)
                    {
                        dataStream.Read(ans, 0, 2);
                        reply = Encoding.ASCII.GetString(ans, 0, ans.Length);
                        break;
                    }
                    else Thread.Sleep(5);
                }
                if (retries == 0) reply = "NO";

                if (reply == "NO")
                {
                    position.X = prevPosX;
                    position.Y = prevPosY;
                }
            }
        }


    }
}
