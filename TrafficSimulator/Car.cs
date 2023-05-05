using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrafficSimulator
{
    public class Car
    {
        public int turn;
        public Point position;
        public float speed;
        public Vector2 speedVect;
        private Vector2 speedVectCopy;
        public Point destination;
        private Point posCopy;
        private Point destCopy;
        public Point Size;
        public Color color = Color.Blue;
        private Stopwatch stopwatch = new Stopwatch();
        public Car[] cars;
        public Car(CarSetup setup, Car[] cars)
        {
            position = new Point(setup.startX, setup.startY);
            speedVect = new Vector2(setup.velocityX, setup.velocityY);
            setup.velocityX = Math.Abs(setup.velocityX);
            setup.velocityY = Math.Abs(setup.velocityY);
            speed = Math.Max(setup.velocityX, setup.velocityY);
            posCopy = position;
            destCopy = destination;
            speedVectCopy = speedVect;
            stopwatch.Start();
            rotate();
            this.cars = cars;
        }

        public void setDestination(Point dest)
        {
            destination = dest;
        }

        public void setTurn()
        {
            turn = 0;
            if (speedVect.X > 0)
            {
                if (destination.Y < position.Y)
                {
                    turn = -1;
                }
                else if (destination.Y > position.Y)
                {
                    turn = 1;
                }
            }
            else if (speedVect.X < 0)
            {
                if (destination.Y < position.Y)
                {
                    turn = 1;
                }
                else if (destination.Y > position.Y)
                {
                    turn = -1;
                }

            }
            else if (speedVect.Y > 0)
            {
                if (destination.X < position.X)
                {
                    turn = 1;
                }
                else if (destination.X > position.X)
                {
                    turn = -1;
                }

            }
            else if (speedVect.Y < 0)
            {
                if (destination.X < position.X)
                {
                    turn = -1;
                }
                else if( destination.X > position.X)
                {
                    turn = 1;
                }

            }
            Console.WriteLine(turn);
            Console.WriteLine("\n");
        }

        public void rotate()
        {
            if (speedVect.X == 0)
            {
                Size.X = 20;
                Size.Y = 40;
            }
            else
            {
                Size.X = 40;
                Size.Y = 20;
            }
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


                foreach (Car car in cars)
                {
                    if (this != car)
                    {
                        // potrzeba wprowadzenia samefarow, czsami 2 samochody znikaja jednoczesnie
                        if ((this.position.X + this.Size.X / 2 <= car.position.X + car.Size.X / 2 &&
                            this.position.X + this.Size.X / 2 >= car.position.X - car.Size.X / 2 &&
                            this.position.Y + this.Size.Y / 2 <= car.position.Y + car.Size.Y / 2 &&
                            this.position.Y + this.Size.Y / 2 >= car.position.Y - car.Size.Y / 2)
                            ||
                            (this.position.X - this.Size.X / 2 <= car.position.X + car.Size.X / 2 &&
                            this.position.X - this.Size.X / 2 >= car.position.X - car.Size.X / 2 &&
                            this.position.Y - this.Size.Y / 2 <= car.position.Y + car.Size.Y / 2 &&
                            this.position.Y - this.Size.Y / 2 >= car.position.Y - car.Size.Y / 2))
                        {
                            //Console.WriteLine("kraksa");
                            position = posCopy;
                            destination = posCopy;
                            speedVect = speedVectCopy;
                            break;
                        }
                    }
                }


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
                        setTurn();

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
                        rotate();
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
