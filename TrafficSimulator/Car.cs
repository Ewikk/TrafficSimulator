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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace TrafficSimulator
{
    public class Car
    {
        public int turn;
        public Point position;
        public float speed = 300; //default speed
        public Vector2 speedVect;
        public Point destination;
        public Point nextJunction;
        public bool outOfMap;
        public Point Size;
        public Color color = Color.Blue;
        private Stopwatch stopwatch = new Stopwatch();


        //TEMP
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
        private void init()
        {
            outOfMap = false;
            stopwatch.Start();
            rotate();
        }

        private Queue<Point> path;

        public void setDestination(Point dest)
        {
            destination = dest;
            path = new Queue<Point>(possiblePaths[position][dest]);
            path.Dequeue();
            nextJunction = path.Dequeue();
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
            rotate();
        }


        public void rotate()
        {
            if (speedVect.X == 0)
            {
                Size.X = 20;
                Size.Y = 30;
            }
            else
            {
                Size.X = 30;
                Size.Y = 20;
            }
        }

        public int distance(Point p1, Point p2)
        {
            return Math.Abs((p1.X - p2.X) - (p1.Y - p2.Y));
        }


        public void Move(Dictionary<Point, List<Point>> roadStructure, List<Point> startingPoints, List<Point> endPoints, Dictionary<Point, TrafficLight>[] trafficLights)
        {
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
                    if (!IsMoveAllowed(trafficLights))
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    int prevPosX = position.X;
                    int prevPosY = position.Y;
                    position.X += (int)(speedVect.X * time);
                    position.Y += (int)(speedVect.Y * time);
                    /*foreach (Car car in cars)
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
                                Console.WriteLine("kraksa");
                                position = posCopy;
                                destination = posCopy;

                                Random rand = new Random();

                                List<Point> listOfNextDest = roadStructure[destination];
                                nextJunction = listOfNextDest[rand.Next(0, listOfNextDest.Count())];
                                setTurn();

                                speedVect = speedVectCopy;
                                rotate();
                                break;
                            }
                        }
                    }*/


                    if (Math.Sign(prevPosX - nextJunction.X) != Math.Sign(position.X - nextJunction.X) ||
                        Math.Sign(prevPosY - nextJunction.Y) != Math.Sign(position.Y - nextJunction.Y))
                    {
                        Random rand = new Random();
                        try
                        {
                            int distance = (int)(nextJunction - position).ToVector2().Length();
                            position = nextJunction;
                            nextJunction = path.Dequeue();
                            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
                            rotate();
                            setTurn();

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
                            turn = 0;
                        }
                        rotate();
                    }
                    Thread.Sleep(15);
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
        }

        private bool IsMoveAllowed(Dictionary<Point, TrafficLight>[] trafficLights)
        {
            return !CarCollisionDetected() && isLightGreen(trafficLights);
        }

        private bool isLightGreen(Dictionary<Point, TrafficLight>[] trafficLights)
        {
            foreach (var area in trafficLights)
            {
                if (area.ContainsKey(nextJunction) && !area[nextJunction].isOpen && distance(nextJunction, position) < 20)
                {
                    return false;
                }
            }
            return true;
        }
        private bool CarCollisionDetected()
        {
            foreach (Car car in cars)
            {
                if (this == car)
                    continue;

                double sp = 0;
                int ownPos = 0;
                int pos2 = 0;
                //TO simplify
                if (speedVect.X != 0 && position.Y == car.position.Y)
                {
                    sp = speedVect.X;
                    ownPos = position.X;
                    pos2 = car.position.X;
                }
                else if (speedVect.Y != 0 && position.X == car.position.X)
                {
                    sp = speedVect.Y;
                    ownPos = position.Y;
                    pos2 = car.position.Y;
                }
                if (sp > 0 && pos2 - ownPos < 50 && pos2 - ownPos > 0 ||
                       sp < 0 && ownPos - pos2 < 50 && ownPos - pos2 > 0)
                {
                    return true;
                }
            }
            return false;
        }

        //must be a better way to do this//just cosmetic
        public void setTurn()
        {
            turn = 0;
            if (path.Count == 0) return;
            if (speedVect.X > 0)
            {
                if (path.Peek().Y > nextJunction.Y)
                {
                    turn = 1;
                }
                else if (path.Peek().Y < nextJunction.Y)
                {
                    turn = -1;
                }
            }
            else if (speedVect.X < 0)
            {
                if (path.Peek().Y > nextJunction.Y)
                {
                    turn = -1;
                }
                else if (path.Peek().Y < nextJunction.Y)
                {
                    turn = 1;
                }

            }
            else if (speedVect.Y > 0)
            {
                if (path.Peek().X > nextJunction.X)
                {
                    turn = -1;
                }
                else if (path.Peek().X < nextJunction.X)
                {
                    turn = 1;
                }

            }
            else if (speedVect.Y < 0)
            {
                if (path.Peek().X > nextJunction.X)
                {
                    turn = 1;
                }
                else if (path.Peek().X < nextJunction.X)
                {
                    turn = -1;
                }

            }
        }

    }
}

