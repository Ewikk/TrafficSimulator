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

namespace TrafficSimulator
{
    public class Car
    {
        public int turn;
        public Point position;
        public float speed = 300; //default speed
        public Vector2 speedVect;
        private Vector2 speedVectCopy;
        public Point destination;
        public Point nextJuntion;
        private Point posCopy;
        private Point destCopy;
        public Point Size;
        public Color color = Color.Blue;
        private Stopwatch stopwatch = new Stopwatch();
        public Car[] cars;



        Dictionary<Point, Dictionary<Point, List<Point>>> possiblePaths;
        public Car(int xPos, int yPos, Dictionary<Point, Dictionary<Point, List<Point>>> possiblePaths)
        {
            position = new Point(xPos, yPos);
            init();
            this.possiblePaths = possiblePaths;
        }
        public Car(int xPos, int yPos, float xSpeed, float ySpeed)
        {
            position = new Point(xPos, yPos);
            speedVect = new Vector2(xSpeed, ySpeed);
            xSpeed = Math.Abs(xSpeed);
            ySpeed = Math.Abs(ySpeed);
            speed = Math.Max(xSpeed, ySpeed);
            init();
        }
        //car is not supposed to see all the others 
        public Car(CarSetup setup, Car[] cars)
        {
            position = new Point(setup.startX, setup.startY);
            speedVect = new Vector2(setup.velocityX, setup.velocityY);
            setup.velocityX = Math.Abs(setup.velocityX);
            setup.velocityY = Math.Abs(setup.velocityY);
            speed = Math.Max(setup.velocityX, setup.velocityY);
            init();
            this.cars = cars;
        }

        private void init()
        {
            posCopy = position;
            destCopy = destination;
            speedVectCopy = speedVect;
            stopwatch.Start();
            rotate();
        }

        private Queue<Point> path;

        public void setDestination(Point dest)
        {
            destination = dest;
            path = new Queue<Point>(possiblePaths[position][dest]);
            path.Dequeue();
            nextJuntion = path.Dequeue();
            speedVect = new Vector2(Math.Sign(nextJuntion.X - position.X) * speed, Math.Sign(nextJuntion.Y - position.Y) * speed);
            rotate();
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
                else if (destination.X > position.X)
                {
                    turn = 1;
                }

            }
            //Console.WriteLine(turn);
            //Console.WriteLine("\n");
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

        double distanceFromLastTurn = 0;
        private void chooseNextJunction(Dictionary<Point, List<Point>> roadStructure)
        {
            List<Point> list = roadStructure[position];
            double biggestDiff = 0;
            double smallestDist = Double.MaxValue;
            double smallestContDist = Double.MaxValue;
            int xChange, yChange;
            Point newNextJuntion = list.First();
            double distance1 = Math.Sqrt(Math.Pow(position.X - destination.X, 2) + Math.Pow(position.Y - destination.Y, 2));
            Boolean found = false;
            foreach (Point point in list)
            {
                double distance2 = Math.Sqrt(Math.Pow(point.X - destination.X, 2) + Math.Pow(point.Y - destination.Y, 2));
                if(distance2 < distance1 || distance2 < smallestDist)
                {
                    try
                    {
                        foreach (Point cont in roadStructure[point])
                        {
                            double distance3 = Math.Sqrt(Math.Pow(cont.X - destination.X, 2) + Math.Pow(cont.Y - destination.Y, 2));
                            if (distance3 < distance2 && distance3 < smallestContDist && distance3 < smallestDist)
                            {
                                smallestContDist = distance3;
                                smallestDist = distance2;
                                newNextJuntion = point;
                            }
                        }
                    }
                    catch
                    {
                        if(point == destination)
                        {
                            newNextJuntion = point;
                        }
                    }
                        
                }
                //double distance3 = Double.MaxValue;
                //try
                //{
                //    foreach (Point cont in roadStructure[point])
                //    {
                //        distance3 = Math.Sqrt(Math.Pow(cont.X - destination.X, 2) + Math.Pow(point.Y - destination.X, 2));
                //        if (Math.Sign(position.X - cont.X) == Math.Sign(position.X - nextJuntion.X) && Math.Sign(position.Y - cont.Y) == Math.Sign(position.Y - nextJuntion.Y))
                //        {
                //            smallestDist = distance3;
                //            newNextJuntion = point;
                //            found = true; break;
                //        }
                //    }
                //}
                //catch
                //{
                //    newNextJuntion = point;
                //}
                //if (!found)
                //{
                //    if (distance2 <= distance1 && distance2 < smallestDist);
                //        newNextJuntion = point;
                //}
            }
            nextJuntion = newNextJuntion;
            speedVect = new Vector2(Math.Sign(nextJuntion.X - position.X) * speed, Math.Sign(nextJuntion.Y - position.Y) * speed);
            rotate();
        }

        public void Move(Dictionary<Point, List<Point>> roadStructure, List<Point> startingPoints, List<Point> endPoints)
        {
            //chooseNextJunction(roadStructure);
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


                //foreach (Car car in cars)
                //{
                //    if (this != car)
                //    {
                //        // potrzeba wprowadzenia samefarow, czsami 2 samochody znikaja jednoczesnie
                //        //co ty pierdolisz
                //        if ((this.position.X + this.Size.X / 2 <= car.position.X + car.Size.X / 2 &&
                //            this.position.X + this.Size.X / 2 >= car.position.X - car.Size.X / 2 &&
                //            this.position.Y + this.Size.Y / 2 <= car.position.Y + car.Size.Y / 2 &&
                //            this.position.Y + this.Size.Y / 2 >= car.position.Y - car.Size.Y / 2)
                //            ||
                //            (this.position.X - this.Size.X / 2 <= car.position.X + car.Size.X / 2 &&
                //            this.position.X - this.Size.X / 2 >= car.position.X - car.Size.X / 2 &&
                //            this.position.Y - this.Size.Y / 2 <= car.position.Y + car.Size.Y / 2 &&
                //            this.position.Y - this.Size.Y / 2 >= car.position.Y - car.Size.Y / 2))
                //        {
                //            //Console.WriteLine("kraksa");
                //            position = posCopy;
                //            destination = posCopy;
                //            speedVect = speedVectCopy;
                //            break;
                //        }
                //    }
                //}


                if (Math.Sign(prevPosX - nextJuntion.X) != Math.Sign(position.X - nextJuntion.X) ||
                    Math.Sign(prevPosY - nextJuntion.Y) != Math.Sign(position.Y - nextJuntion.Y))
                {
                    try
                    {
                        Random rand = new Random();
                        //List<Point> listOfDest = roadStructure[nextJuntion];


                        //Point newNextJunction = listOfDest[rand.Next(0, listOfDest.Count())];


                        int distance = (int)(nextJuntion - position).ToVector2().Length();
                        position = nextJuntion;
                        //chooseNextJunction(roadStructure);



                        //nextJuntion = roadStructure[position][rand.Next(roadStructure[position].Count)]
                        nextJuntion = path.Dequeue();
                        speedVect = new Vector2(Math.Sign(nextJuntion.X - position.X) * speed, Math.Sign(nextJuntion.Y - position.Y) * speed);
                        rotate();
                        //setDestination(newDestination);
                        setTurn();

                        if (position.X != nextJuntion.X)
                        {
                            speedVect.X = Math.Sign(nextJuntion.X - position.X) * speed;
                            position.X += Math.Sign(nextJuntion.X - position.X) * distance;
                            speedVect.Y = 0;
                        }
                        else
                        {
                            speedVect.X = 0;
                            speedVect.Y = Math.Sign(nextJuntion.Y - position.Y) * speed;
                            position.Y += Math.Sign(nextJuntion.Y - position.Y) * distance;
                        }
                    }
                    catch
                    {
                        Random rand = new Random();
                        position = startingPoints[rand.Next(startingPoints.Count)];
                        setDestination(endPoints[rand.Next(endPoints.Count)]);
                        //chooseNextJunction(roadStructure);
                    }


                }
                Thread.Sleep(50);
            }
        }
    }
}
