﻿using Microsoft.Xna.Framework;
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


namespace PedestrianProcess
{
    public class PedestrianProcess
    {
        private string ipAdress = "FILL IN";
        private int pedestrianCount = 100;
        public Pedestrian[] pedestrians = null;
        private List<Point> startingPoints;
        private List<Point> endPoints;
        private Dictionary<Point, List<Point>> sidewalkStructure;
        private Stopwatch stopwatch = new Stopwatch();
        private Vector2 Size = new Vector2(10, 10);



        private TcpClient connectionServer = new TcpClient();
        private TcpClient dataServer = new TcpClient();
        private bool isConnected = false;
        private NetworkStream connectionStream;
        private NetworkStream dataStream;
        public void Start()
        {
            Deserialize();
            connectionServer.Connect(ipAdress, 13131);
            connectionStream = connectionServer.GetStream();
            connectionStream.Write(Encoding.ASCII.GetBytes("ConPED"), 0, 6);
            byte[] receiveBuffer = new byte[4];
            connectionStream.Read(receiveBuffer, 0, 4);
            int dataServerPort = BitConverter.ToInt32(receiveBuffer, 0);
            dataServer.Connect(ipAdress, dataServerPort);
            dataStream = dataServer.GetStream();
            isConnected = true;

            SetupPedestrians();
            Point[] poses = new Point[pedestrianCount];
            for (int i = 0; i < pedestrianCount; i++)
            {
                poses[i] = pedestrians[i].position;
            }
            byte[] bytes = new byte[3 + poses.Length * 2 * sizeof(int)];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("POS"), 0, bytes, 0, 3);
            for (int i = 0; i < pedestrianCount; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(pedestrians[i].position.X), 0, bytes, 3 + i * 2 * sizeof(int), sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(pedestrians[i].position.Y), 0, bytes, 3 + i * 2 * sizeof(int) + sizeof(int), sizeof(int));
            }
            connectionStream.Write(bytes, 0, bytes.Length);

            Run();
            Console.WriteLine("Success");

        }

        protected void Deserialize()
        {
            using (XmlReader reader = XmlReader.Create(new StreamReader("../../../../sidewalkStructure.xml")))
            {
                var serializer = new ConfigurationContainer()
              .UseOptimizedNamespaces()
              .Create();
                sidewalkStructure = (Dictionary<Point, List<Point>>)serializer.Deserialize(reader);
            }
            using (XmlReader reader = XmlReader.Create(new StreamReader("../../../../sidewalkStartingPoints.xml")))
            {
                var serializer = new ConfigurationContainer()
              .UseOptimizedNamespaces()
              .Create();
                startingPoints = (List<Point>)serializer.Deserialize(reader);
            }
            using (XmlReader reader = XmlReader.Create(new StreamReader("../../../../sidewalkEndPoints.xml")))
            {
                var serializer = new ConfigurationContainer()
              .UseOptimizedNamespaces()
              .Create();
                endPoints = (List<Point>)serializer.Deserialize(reader);
            }
        }

        private void SetupPedestrians()
        {
            pedestrians = new Pedestrian[pedestrianCount];
            Random rand = new Random();
            for (int i = 0; i < pedestrianCount; i++)
            {
                Point start = startingPoints[rand.Next(startingPoints.Count)];
                pedestrians[i] = new Pedestrian(start.X, start.Y);
                pedestrians[i].setDestination(endPoints[rand.Next(endPoints.Count)]);
                pedestrians[i].color = new Color(rand.Next(256), rand.Next(256), rand.Next(256), 255);

                pedestrians[i].nextJunction = sidewalkStructure[pedestrians[i].position][rand.Next(sidewalkStructure[pedestrians[i].position].Count)];
                pedestrians[i].speedVect = new Vector2(Math.Sign(pedestrians[i].nextJunction.X - pedestrians[i].position.X) * pedestrians[i].speed, Math.Sign(pedestrians[i].nextJunction.Y - pedestrians[i].position.Y) * pedestrians[i].speed);
            }
        }

        public void Run()
        {
            stopwatch.Start();
            try
            {
                while (true)
                {
                    Thread.Sleep(10);
                    stopwatch.Stop();
                    TimeSpan timeSpan = stopwatch.Elapsed;
                    double time;
                    if (Debugger.IsAttached)
                    {
                        time = 0.05;
                    }
                    else
                    {
                        time = timeSpan.TotalSeconds;
                    }
                    stopwatch.Restart();
                    stopwatch.Start();
                    Random rand = new Random();
                    Point[] prevPoses = new Point[pedestrianCount];
                    for (int i = 0; i < pedestrianCount; i++)
                    {
                        prevPoses[i] = pedestrians[i].position;
                    }
                    for (int i = 0; i < pedestrianCount; i++)
                    {

                        int prevPosX = pedestrians[i].position.X;
                        int prevPosY = pedestrians[i].position.Y;
                        pedestrians[i].position.X += (int)(pedestrians[i].speedVect.X * time);
                        pedestrians[i].position.Y += (int)(pedestrians[i].speedVect.Y * time);

                        if (Math.Sign(prevPosX - pedestrians[i].nextJunction.X) != Math.Sign(pedestrians[i].position.X - pedestrians[i].nextJunction.X) ||
                            Math.Sign(prevPosY - pedestrians[i].nextJunction.Y) != Math.Sign(pedestrians[i].position.Y - pedestrians[i].nextJunction.Y))
                        {
                            try
                            {
                                int distance = (int)(pedestrians[i].nextJunction - pedestrians[i].position).ToVector2().Length();
                                pedestrians[i].position = pedestrians[i].nextJunction;
                                pedestrians[i].nextJunction = sidewalkStructure[pedestrians[i].position][rand.Next(sidewalkStructure[pedestrians[i].position].Count)];
                                pedestrians[i].speedVect = new Vector2(Math.Sign(pedestrians[i].nextJunction.X - pedestrians[i].position.X) * pedestrians[i].speed, Math.Sign(pedestrians[i].nextJunction.Y - pedestrians[i].position.Y) * pedestrians[i].speed);

                                if (pedestrians[i].position.X != pedestrians[i].nextJunction.X)
                                {
                                    pedestrians[i].speedVect.X = Math.Sign(pedestrians[i].nextJunction.X - pedestrians[i].position.X) * pedestrians[i].speed;
                                    pedestrians[i].position.X += Math.Sign(pedestrians[i].nextJunction.X - pedestrians[i].position.X) * distance;
                                    pedestrians[i].speedVect.Y = 0;
                                }
                                else
                                {
                                    pedestrians[i].speedVect.X = 0;
                                    pedestrians[i].speedVect.Y = Math.Sign(pedestrians[i].nextJunction.Y - pedestrians[i].position.Y) * pedestrians[i].speed;
                                    pedestrians[i].position.Y += Math.Sign(pedestrians[i].nextJunction.Y - pedestrians[i].position.Y) * distance;
                                }
                            }
                            catch
                            {
                                pedestrians[i].position = startingPoints[rand.Next(startingPoints.Count)];
                                pedestrians[i].setDestination(endPoints[rand.Next(endPoints.Count)]);
                                pedestrians[i].nextJunction = sidewalkStructure[pedestrians[i].position][rand.Next(sidewalkStructure[pedestrians[i].position].Count)];
                                pedestrians[i].speedVect = new Vector2(Math.Sign(pedestrians[i].nextJunction.X - pedestrians[i].position.X) * pedestrians[i].speed, Math.Sign(pedestrians[i].nextJunction.Y - pedestrians[i].position.Y) * pedestrians[i].speed);
                            }
                        }

                    }
                    Point[] newPoses = new Point[pedestrianCount];
                    for (int i = 0; i < pedestrianCount; i++)
                    {
                        newPoses[i] = pedestrians[i].position;
                    }
                    byte[] bytes = new byte[3 + newPoses.Length * 2 * sizeof(int)];
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes("POS"), 0, bytes, 0, 3);
                    for (int i = 0; i < pedestrianCount; i++)
                    {
                        Buffer.BlockCopy(BitConverter.GetBytes(pedestrians[i].position.X), 0, bytes, 3 + i * 2 * sizeof(int), sizeof(int));
                        Buffer.BlockCopy(BitConverter.GetBytes(pedestrians[i].position.Y), 0, bytes, 3 + i * 2 * sizeof(int) + sizeof(int), sizeof(int));
                    }
                    dataStream.Write(bytes, 0, bytes.Length);
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] ans = new byte[pedestrianCount];
                    string message = "";
                    dataStream.Read(ans, 0, pedestrianCount);
                    message = Encoding.ASCII.GetString(ans, 0, ans.Length);

                    for (int i = 0; i < pedestrianCount; i++)
                    {
                        if (message[i] == 'N')
                            pedestrians[i].position = prevPoses[i];
                    }
                    byte[] nextJun = new byte[3 + pedestrianCount * 2 * sizeof(int)];
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes("NEJ"), 0, nextJun, 0, 3);
                    for (int i = 0; i < pedestrianCount; i++)
                    {
                        Buffer.BlockCopy(BitConverter.GetBytes(pedestrians[i].nextJunction.X), 0, nextJun, 3 + i * 2 * sizeof(int), sizeof(int));
                        Buffer.BlockCopy(BitConverter.GetBytes(pedestrians[i].nextJunction.Y), 0, nextJun, 3 + i * 2 * sizeof(int) + sizeof(int), sizeof(int));
                    }
                    dataStream.Write(nextJun, 0, nextJun.Length);
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
        }


        public int distance(Point p1, Point p2)
        {
            return Math.Abs((p1.X - p2.X) - (p1.Y - p2.Y));
        }




    }
}
