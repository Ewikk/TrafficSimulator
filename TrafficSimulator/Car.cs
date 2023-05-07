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
        private Vector2 speedVectCopy;
        public Point destination;
        public Point nextJunction;
        private Point posCopy;
        private Point destCopy;
        private Point nextDestCopy;
        public bool outOfMap;
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
            outOfMap = false;
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
            nextJunction = path.Dequeue();
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
            rotate();

        }

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


        public void Move(Dictionary<Point, List<Point>> roadStructure, List<Point> startingPoints, List<Point> endPoints)
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

                        //if (!roadStructure.ContainsKey(destination))
                        //{
                        //    outOfMap = true;
                        //}
                        //else
                        //{
                        //    List<Point> listOfNextDest = roadStructure[destination];
                        //    nextJunction = listOfNextDest[rand.Next(0, listOfNextDest.Count())];
                        //    setTurn();
                        //}

                        rotate();

                    }
                    Thread.Sleep(15);
                }
            }
            catch (ThreadInterruptedException){
                return;
            }
        }
    }
}

