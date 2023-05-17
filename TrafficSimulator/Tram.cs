using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace TrafficSimulator
{
    public class Tram
    {
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
        public Tram[] siema;
        private bool isGoing = true;

        public Tram(int xPos, int yPos, float xSpeed, float ySpeed)
        {
            position = new Point(xPos, yPos);
            Console.WriteLine(position.ToString());
            speedVect = new Vector2(xSpeed, ySpeed);
            xSpeed = Math.Abs(xSpeed);
            ySpeed = Math.Abs(ySpeed);
            speed = Math.Max(xSpeed, ySpeed);
            posCopy = position;
            speedVectCopy = speedVect;
            Size.X = 100;
            Size.Y = 30;
            nextDest1 = new List<Point>();
            nextDest2 = new List<Point>();
            init();
            Console.WriteLine("Siema");
            destination = setDestination(position);
            destCopy = destination;
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
                            //destination = destCopy;
                        }
                        else
                            newDestination = nextDest1[i + 1];

                        if(i == 2 || i == 1)
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
                            //destination = destCopy;
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

                if(!isGoing)
                {
                    int sleepms = 3000;
                    Thread.Sleep(sleepms);
                    isGoing = true;
                    position.X -= (int)(speedVect.X) * sleepms / 1000;
                }

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
                Thread.Sleep(15);
            }
        }



    }
}





