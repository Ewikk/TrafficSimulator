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
        public Tram[] siema;

        public Tram(int xPos, int yPos, float xSpeed, float ySpeed)
        {
            position = new Point(xPos, yPos);
            Console.WriteLine(position.ToString());
            speedVect = new Vector2(xSpeed, ySpeed);
            xSpeed = Math.Abs(xSpeed);
            ySpeed = Math.Abs(ySpeed);
            speed = Math.Max(xSpeed, ySpeed);
            posCopy = position;
            destination = setDestination(position);
            destCopy = destination;
            speedVectCopy = speedVect;
            Size.X = 80;
            Size.Y = 30;
            init();
        }
        private void init()
        {
            outOfMap = false;
            stopwatch.Start();
        }

        public Point setDestination(Point cos)
        {
            Point a1 = new Point(1085, 406);
            Point b1 = new Point(105, 455);
            Point b2 = new Point(1085, 455);
            Point a2 = new Point(105, 406);

            Point newDestination = new Point(0, 0);
            if (cos == a1)
                newDestination = a2;
            else if (cos == a2)
                newDestination = a1;
            else if (cos == b1)
                newDestination = b2;
            else if (cos == b2)
                newDestination = b1;
            return newDestination;
        }

        public void Move()
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
                int prevPosX = position.X;
                int prevPosY = position.Y;
                position.X += (int)(speedVect.X * time);
                position.Y += (int)(speedVect.Y * time);

                if (Math.Sign(prevPosX - destination.X) != Math.Sign(position.X - destination.X))
                {
                    position = destination;
                    destination = setDestination(destination);
                    speedVect.X = -speedVect.X;

 /*                       if (position.X != destination.X)
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
                        }*/
/*                        position = posCopy;
                        destination = posCopy;*/
                        //speedVect = -speedVectCopy;
                }
                Thread.Sleep(15);
            }
        }



    }
}





