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

namespace TramProcess
{
    public class TramProcess
    {

        private UdpClient connectionServer = new UdpClient();
        private UdpClient dataServer = new UdpClient();
        private IPEndPoint dataServerEndPoint;
        private bool isConnected = false;
        public Point position;
        public float speed = 300; //default speed
        public Vector2 speedVect;
        public Point destination;
        private Vector2 speedVectCopy;
        private Point posCopy;
        private Point destCopy;
        public bool outOfMap;
        public Point Size;
        public Color color = Color.Blue;
        private Stopwatch stopwatch = new Stopwatch();
        private List<Point> nextDest1;
        private List<Point> nextDest2;
        private bool isGoing = true;
        public void Start()
        {

            Console.WriteLine("Siema");
            connectionServer.Connect("localhost", 13131);
            connectionServer.Send(Encoding.ASCII.GetBytes("ConTRA"), 6);
            dataServerEndPoint = new IPEndPoint(IPAddress.Any, 0);
            var port = connectionServer.Receive(ref dataServerEndPoint);
            dataServerEndPoint.Port = BitConverter.ToInt32(port, 0);
            dataServer.Connect(dataServerEndPoint);
            isConnected = true;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] startingPos = connectionServer.Receive(ref endPoint);
            int xPos = BitConverter.ToInt32(startingPos, 0);
            int yPos = BitConverter.ToInt32(startingPos, sizeof(int));
            int xSpeed = BitConverter.ToInt32(startingPos, 2 * sizeof(int));
            position = new Point(xPos, yPos);
            speedVect = new Vector2(xSpeed, 0);
            speed = Math.Max(200, 0);
            posCopy = position;
            speedVectCopy = speedVect;
            Size.X = 100;
            Size.Y = 30;
            nextDest1 = new List<Point>();
            nextDest2 = new List<Point>();

            init();
            destination = setDestination(position);
            destCopy = destination;

            Console.WriteLine("Success");
            Move();
        }





        private void init()
        {
            outOfMap = false;
            stopwatch.Start();

            Point a4 = new Point(0, 406);
            Point a3 = new Point(105, 406);
            Point a2 = new Point(1085, 406);
            Point a1 = new Point(1470, 406);

            nextDest1.Add(a1);
            nextDest1.Add(a2);
            nextDest1.Add(a3);
            nextDest1.Add(a4);

            Point b1 = new Point(0, 455);
            Point b2 = new Point(105, 455);
            Point b3 = new Point(1085, 455);
            Point b4 = new Point(1470, 455);

            nextDest2.Add(b1);
            nextDest2.Add(b2);
            nextDest2.Add(b3);
            nextDest2.Add(b4);
        }

        public Point setDestination(Point cos)
        {
            Point newDestination = new Point(0, 0);
            if (nextDest1.Contains(cos))
            {
                int i = 0;
                foreach (Point p in nextDest1)
                {
                    if (p == cos)
                    {
                        if (i == 3)
                        {
                            newDestination = destCopy;
                            position = posCopy;
                            destination = destCopy;
                        }
                        else
                            newDestination = nextDest1[i + 1];

                        if (i == 2 || i == 1)
                        {
                            isGoing = false;
                        }
                    }
                    i++;
                }
            }
            else if (nextDest2.Contains(cos))
            {
                int i = 0;
                foreach (Point p in nextDest2)
                {
                    if (p == cos)
                    {
                        if (i == 3)
                        {
                            newDestination = destCopy;
                            position = posCopy;
                            destination = destCopy;
                        }
                        else
                            newDestination = nextDest2[i + 1];

                        if (i == 2 || i == 1)
                        {
                            isGoing = false;
                        }
                    }
                    i++;
                }
            }
            return newDestination;
        }

        public void Move()
        {
            while (true)
            {

                if (!isGoing)
                {
                    int sleepms = 3000;
                    Thread.Sleep(sleepms);
                    isGoing = true;
                    position.X -= (int)(speedVect.X) * sleepms / 1000;
                }

                stopwatch.Stop();
                TimeSpan timeSpan = stopwatch.Elapsed;
                double time;

                time = timeSpan.TotalSeconds;
                stopwatch.Restart();
                stopwatch.Start();
                int prevPosX = position.X;
                int prevPosY = position.Y;
                if (isGoing)
                {

                    position.X += (int)(speedVect.X * time);
                    position.Y += (int)(speedVect.Y * time);
                }


                if (Math.Sign(prevPosX - destination.X) != Math.Sign(position.X - destination.X))
                {
                    position = destination;
                    destination = setDestination(destination);

                }

                byte[] newPos = new byte[3 * sizeof(int) + 3];
                Buffer.BlockCopy(Encoding.ASCII.GetBytes("POS"), 0, newPos, 0, 3);
                Buffer.BlockCopy(BitConverter.GetBytes(position.X), 0, newPos, 3, sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(position.Y), 0, newPos, 3 + sizeof(int), sizeof(int));
                dataServer.Send(newPos, newPos.Length);
/*                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] ans = connectionServer.Receive(ref endPoint);
                string reply = Encoding.ASCII.GetString(ans, 0, ans.Length);
                if (reply == "NO")
                {
                    Console.WriteLine("brak ruchu");
                    position.X = prevPosX; position.Y = prevPosY;
                }*/
                Thread.Sleep(15);

            }
        }
    }
}
