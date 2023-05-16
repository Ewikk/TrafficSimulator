using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrafficSimulator
{
    public class Pedestrian
    {
        public Point position;
        public float speed = 100; //default speed
        public Vector2 speedVect;
        public Point destination;
        public Point nextJunction;
        public Point Size;
        public Color color = Color.Blue;
        private Stopwatch stopwatch = new Stopwatch();
        private Queue<Point> path;

        Dictionary<Point, Dictionary<Point, List<Point>>> possiblePaths;
        public Pedestrian(int xPos, int yPos, Dictionary<Point, Dictionary<Point, List<Point>>> possiblePaths)
        {
            position = new Point(xPos, yPos);
            init();
            this.possiblePaths = possiblePaths;
        }
        public Pedestrian(int xPos, int yPos, float xSpeed, float ySpeed)
        {
            position = new Point(xPos, yPos);
            speedVect = new Vector2(xSpeed, ySpeed);
            xSpeed = Math.Abs(xSpeed);
            ySpeed = Math.Abs(ySpeed);
            speed = Math.Max(xSpeed, ySpeed);
            init();
        }

        private void init()
        {
            stopwatch.Start();
        }

        public void setDestination(Point dest)
        {
            destination = dest;
            //path = new Queue<Point>(possiblePaths[position][dest]);
            //path.Dequeue();
            //nextJunction = path.Dequeue();
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
        }

        public void Move(Dictionary<Point, List<Point>> sidewalkStructure, List<Point> startingPoints, List<Point> endPoints/*, Dictionary<Point, TrafficLight>[] trafficLights*/)
        {
            Random rand = new Random();
            nextJunction = sidewalkStructure[position][rand.Next(sidewalkStructure[position].Count)];
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
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
                    //if (!IsMoveAllowed(trafficLights, sidewalkStructure))
                    //{
                    //    Thread.Sleep(50);
                    //    continue;
                    //}

                    int prevPosX = position.X;
                    int prevPosY = position.Y;
                    position.X += (int)(speedVect.X * time);
                    position.Y += (int)(speedVect.Y * time);

                    if (Math.Sign(prevPosX - nextJunction.X) != Math.Sign(position.X - nextJunction.X) ||
                        Math.Sign(prevPosY - nextJunction.Y) != Math.Sign(position.Y - nextJunction.Y))
                    {
                        try
                        {
                            int distance = (int)(nextJunction - position).ToVector2().Length();
                            position = nextJunction;
                            //nextJunction = path.Dequeue();
                            nextJunction = sidewalkStructure[position][rand.Next(sidewalkStructure[position].Count)];
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
                            position = startingPoints[rand.Next(startingPoints.Count)];
                            setDestination(endPoints[rand.Next(endPoints.Count)]);
                            nextJunction = sidewalkStructure[position][rand.Next(sidewalkStructure[position].Count)];
                            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
                        }
                    }
                    Thread.Sleep(15);
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
        }

    }
}
