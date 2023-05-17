using Microsoft.Xna.Framework;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace TrafficSimulator
{
    public class PedestrianThread
    {
        private int pedestrianCount = 0;
        public Pedestrian[] pedestrians = null;
        private List<Point> startingPoints;
        private List<Point> endPoints;
        private Dictionary<Point, List<Point>> sidewalkStructure;
        private Dictionary<Point, Dictionary<Point, List<Point>>> sidewalkPaths;
        private Stopwatch stopwatch = new Stopwatch();
        private Vector2 Size = new Vector2(10, 10);
        private Dictionary<Point, TrafficLight> trafficLights;
        public PedestrianThread(int count, List<Point> startingPoints, List<Point> endPoints, Dictionary<Point, Dictionary<Point, List<Point>>> paths, Dictionary<Point, List<Point>> sidewalkStructure, Dictionary<Point, TrafficLight> trafficLights)
        {
            pedestrianCount = count;
            this.startingPoints = startingPoints;
            this.endPoints = endPoints;
            this.sidewalkStructure = sidewalkStructure;
            this.trafficLights = trafficLights;
            sidewalkPaths = paths;
            pedestrians = new Pedestrian[pedestrianCount];
            SetupPedestrians();
        }

        private void SetupPedestrians()
        {
            Random rand = new Random();
            for (int i = 0; i < pedestrianCount; i++)
            {
                Point start = startingPoints[rand.Next(startingPoints.Count)];
                pedestrians[i] = new Pedestrian(start.X, start.Y, sidewalkPaths);
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
                    for (int i = 0; i < pedestrianCount; i++)
                    {
                        //if (CollisionDetected(pedestrians[i], 10)) continue;
                        if (!isLightGreen(pedestrians[i], 5)) continue;
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
                    Thread.Sleep(25);
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
        }

        private bool isLightGreen(Pedestrian pedestrian,int distanceBetween)
        {
           
                if (trafficLights.ContainsKey(pedestrian.nextJunction) && !trafficLights[pedestrian.nextJunction].isOpen && distance(pedestrian.nextJunction, pedestrian.position) < distanceBetween && distance(pedestrian.nextJunction, pedestrian.position) > distanceBetween / 2)
                {
                    return false;
                }
           
            return true;
        }

        public int distance(Point p1, Point p2)
        {
            return Math.Abs((p1.X - p2.X) - (p1.Y - p2.Y));
        }

        private bool CollisionDetected(Pedestrian pedestrian, int distanceBetween)
        {
            foreach (Pedestrian pedestrian2 in pedestrians)
            {
                if (pedestrian == pedestrian2)
                    continue;

                double sp = 0;
                int ownPos = 0;
                int pos2 = 0;
                //TO simplify
                bool collison = false;
                if (pedestrian.speedVect.X != 0 && Math.Abs(pedestrian2.position.Y - pedestrian.position.Y) < pedestrian2.Size.Y / 2 + Size.Y / 2)
                {
                    sp = pedestrian.speedVect.X;
                    ownPos = pedestrian.position.X;
                    pos2 = pedestrian2.position.X;

                    if (sp > 0 && pos2 - ownPos < pedestrian2.Size.X / 2 + this.Size.X / 2 + distanceBetween && pos2 - ownPos > 0 ||
                        sp < 0 && ownPos - pos2 < pedestrian2.Size.X / 2 + this.Size.X / 2 + distanceBetween && ownPos - pos2 > 0)
                    {
                        return true;
                    }
                }
                else if (pedestrian.speedVect.Y != 0 && Math.Abs(pedestrian2.position.X - pedestrian.position.X) < pedestrian2.Size.X / 2 + this.Size.X / 2)
                {
                    sp = pedestrian.speedVect.Y;
                    ownPos = pedestrian.position.Y;
                    pos2 = pedestrian2.position.Y;

                    if (sp > 0 && pos2 - ownPos < pedestrian2.Size.Y / 2 + this.Size.Y / 2 + distanceBetween && pos2 - ownPos > 0 ||
                       sp < 0 && ownPos - pos2 < pedestrian2.Size.Y / 2 + this.Size.Y / 2 + distanceBetween && ownPos - pos2 > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }



            }
}
