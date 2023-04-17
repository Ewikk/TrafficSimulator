using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrafficSimulator
{
    public class Car
    {
        public Point position;
        public float speed;
        public Vector2 speedVect;
        private Vector2 speedVectCopy;
        private Point destination;
        private Point posCopy;
        private Point destCopy;
        public Color color = Color.Blue;
        private Stopwatch stopwatch = new Stopwatch();
        public Car(int xPos, int yPos, float xSpeed, float ySpeed)
        {
            position = new Point(xPos, yPos);
            speedVect = new Vector2(xSpeed, ySpeed);
            xSpeed = Math.Abs(xSpeed);
            ySpeed = Math.Abs(ySpeed);
            speed = Math.Max(xSpeed, ySpeed);
            posCopy = position;
            destCopy = destination;
            speedVectCopy = speedVect;
            stopwatch.Start();
        }

        public void setDestination(Point dest)
        {
            destination = dest;
        }

        public void Move(Dictionary<Point, List<Point>> roadStructure)
        {
            while (true)
            {
                stopwatch.Stop();
                TimeSpan timeSpan = stopwatch.Elapsed;
                double time = timeSpan.TotalSeconds;
                stopwatch.Restart();
                stopwatch.Start();
                int prevPosX = position.X;
                int prevPosY = position.Y;
                position.X += (int)(speedVect.X * time);
                position.Y += (int)(speedVect.Y * time);
                if (Math.Sign(prevPosX - destination.X) != Math.Sign(position.X - destination.X) ||
                    Math.Sign(prevPosY - destination.Y) != Math.Sign(position.Y - destination.Y))
                {
                    try
                    {
                        Random rand = new Random();
                        List<Point> listOfDest = roadStructure[destination];
                        Point newDestination = listOfDest[rand.Next(0, listOfDest.Count())];
                        int distance = (int)(destination - position).ToVector2().Length();
                        position = destination;
                        destination = newDestination;

                        if (position.X != destination.X)
                        {
                            speedVect.X = Math.Sign(destination.X - position.X) * speed;
                            position.X += Math.Sign(destination.X - position.X) * distance;
                            speedVect.Y = 0;
                        }
                        else
                        {
                            speedVect.X = 0;
                            speedVect.Y = Math.Sign(destination.Y - position.Y) * speed;
                            position.Y += Math.Sign(destination.Y - position.Y) * distance;
                        }
                    }
                    catch
                    {
                        position = posCopy;
                        destination = posCopy;
                        speedVect = speedVectCopy;
                    }


                }
                Thread.Sleep(1);
            }
        }
    }
}
