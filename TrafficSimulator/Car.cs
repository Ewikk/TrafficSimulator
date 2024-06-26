﻿using Microsoft.Xna.Framework;
using SharpDX.MediaFoundation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace TrafficSimulator
{
    public class Car
    {
        public int turn;
        public Point position;
        public float speed = 200;
        public Vector2 speedVect;
        public Point destination;
        public Point nextJunction;
        public bool outOfMap;
        public Point Size;
        public Color color = Color.Blue;
        private Stopwatch stopwatch = new Stopwatch();


        private List<Car> cars;



        Dictionary<Point, Dictionary<Point, List<Point>>> possiblePaths;
        public Car(int xPos, int yPos, Dictionary<Point, Dictionary<Point, List<Point>>> possiblePaths)
        {
            position = new Point(xPos, yPos);
            init();
            this.possiblePaths = possiblePaths;
        }

        public Car(int xPos, int yPos)
        {
            position = new Point(xPos, yPos);
            Random random = new Random();
            color = new Color(random.Next(256), random.Next(256), random.Next(256), 255);
            init();
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
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
            rotate();
        }

        public void setPosition(int x, int y)
        {
            position.X = x; position.Y = y;
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
            rotate();
        }
        public void setPosition(Point position)
        {
            this.position = position;
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
            rotate();
        }

        private Queue<Point> path;

        public void setDestination(Point dest)
        {
            destination = dest;
            path = new Queue<Point>(possiblePaths[position][dest]);
            path.Dequeue();
            setNextJunction(path.Dequeue());
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
            rotate();
        }

        public void setNextJunction(Point nextJunction)
        {
            path.Dequeue();
            this.nextJunction = nextJunction;
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);
            rotate();
        }

        public void setPath(List<Point> path)
        {
            this.path = new Queue<Point>(path);
        }

        public void setCars(List<Car> cars)
        {
            this.cars = cars;
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



        public bool IsMoveAllowed(Dictionary<Point, TrafficLight>[] trafficLights, Dictionary<Point, List<Point>> roadStructure, List<Tram> trams, PedestrianContainer pedestrianManager)

        {
            List<Point> rownorzedne = new List<Point>();
            rownorzedne.Add(new Point(1289, 617));
            rownorzedne.Add(new Point(1289, 580));
            rownorzedne.Add(new Point(1327, 617));
            rownorzedne.Add(new Point(1327, 580));

            rownorzedne.Add(new Point(1289, 163));
            rownorzedne.Add(new Point(1289, 126));
            rownorzedne.Add(new Point(1327, 163));
            rownorzedne.Add(new Point(1327, 126));

            int distanceBetweenCars = (int)Math.Sqrt(speed * 1.5);
            if (rownorzedne.Contains(nextJunction))
            {
                return !CarCollisionDetected(distanceBetweenCars) && !rightHand(roadStructure, 2 * distanceBetweenCars) && !isPedestrianAhead(pedestrianManager);
            }
            else
            {
                return !CarCollisionDetected(distanceBetweenCars) && isLightGreen(trafficLights, 40) && !ClosetoTram(distanceBetweenCars, trams);
            }
        }

        private bool isLightGreen(Dictionary<Point, TrafficLight>[] trafficLights, int distanceBetweenCars)
        {
            foreach (var area in trafficLights)
            {
                if (area.ContainsKey(nextJunction) && !area[nextJunction].isOpen && distance(nextJunction, position) < distanceBetweenCars && distance(nextJunction, position) > distanceBetweenCars / 2)
                {
                    return false;
                }
            }
            return true;
        }


        private bool isPedestrianAhead(PedestrianContainer pedestrianManager)
        {
            foreach (Pedestrian pedestrian in pedestrianManager.pedestrians)
            {
                if ((distance(position, pedestrian.position) < 55 && distance(position, pedestrian.position) > 10 && speedVect.X != 0 && Math.Sign(speedVect.X) == Math.Sign(pedestrian.position.X - position.X) && Math.Abs(pedestrian.position.Y - position.Y) < 20)
                || (distance(position, pedestrian.position) < 55 && distance(position, pedestrian.position) > 10 && speedVect.Y != 0 && Math.Sign(speedVect.Y) == Math.Sign(pedestrian.position.Y - position.Y) && Math.Abs(pedestrian.position.X - position.X) < 20))
                    return true;
            }
            return false;
        }

        private bool ClosetoTram(int distanceBetweenCars, List<Tram> trams)
        {
            int lowerTracksY = 455;
            int upperTracksY = 406;

            if (speedVect.Y < 0 && position.Y + distanceBetweenCars > lowerTracksY + Size.Y / 2 && position.Y < lowerTracksY + Size.Y / 2 + distanceBetweenCars)
            {
                foreach (Car car in cars)
                {
                    if (this == car)
                        continue;
                    Point dis = distancePoint(this, position, car);
                    if (dis.Y < Size.Y + Size.Y / 2 + 80 && dis.Y >= 0 && dis.X == 0)
                        return true;
                }
            }
            else if (speedVect.Y > 0 && position.Y < upperTracksY - Size.Y / 2 + 35 + distanceBetweenCars && position.Y + 35 + distanceBetweenCars > upperTracksY - Size.Y / 2)
            {
                foreach (Car car in cars)
                {
                    if (this == car)
                        continue;
                    Point dis = distancePoint(this, position, car);
                    if (dis.Y < Size.Y + Size.Y / 2 + 90 + distanceBetweenCars && dis.Y >= 0 && dis.X == 0)
                        return true;
                }

            }
            return beforeTram(distanceBetweenCars, trams);
        }

        private bool beforeTram(int distanceBetweenCars, List<Tram> trams)
        {
            foreach (Tram tram in trams)
            {
                double sp = 0;
                int ownPos = 0;
                int pos2 = 0;
                if (speedVect.Y != 0 && Math.Abs(tram.position.X - position.X) < tram.Size.X / 2 + this.Size.X / 2)
                {
                    sp = speedVect.Y;
                    ownPos = position.Y;
                    pos2 = tram.position.Y;

                    if (sp > 0 && pos2 - ownPos < tram.Size.Y / 2 + this.Size.Y / 2 + distanceBetweenCars && pos2 - ownPos > 0 ||
                       sp < 0 && ownPos - pos2 < tram.Size.Y / 2 + this.Size.Y / 2 + distanceBetweenCars && ownPos - pos2 > 0)
                    {
                        return true;
                    }
                }

            }
            return false;

        }

        private bool rightHand(Dictionary<Point, List<Point>> roadStructure, int distanceBetweenCars)
        {
            Point next = this.nextJunction;
            Point nextForward = new Point(0, 0);
            try
            {
                foreach (Point P in roadStructure[next])
                {
                    int x = P.X - next.X;
                    int y = P.Y - next.Y;
                    if ((this.speedVect.X > 0 && x > 0) || (this.speedVect.Y > 0 && y > 0))
                    {
                        nextForward.X = P.X;
                        nextForward.Y = P.Y;
                        break;
                    }
                    else if ((this.speedVect.X < 0 && x < 0) || (this.speedVect.Y < 0 && y < 0))
                    {
                        nextForward.X = next.X;
                        nextForward.Y = next.Y;
                        break;
                    }
                }
            }
            catch { return false; }

            foreach (Car car in cars)
            {
                if (this == car)
                    continue;

                Point distP = distancePoint(this, nextForward, car);
                if (distP.X > 0 && distP.X < distanceBetweenCars && distP.Y == 0 && distance(position, next) < distanceBetweenCars)
                {
                    return true;
                }
            }
            return false;
        }
        private bool CarCollisionDetected(int distanceBetweenCars)
        {
            foreach (Car car in cars)
            {
                if (this == car)
                    continue;

                double sp = 0;
                int ownPos = 0;
                int pos2 = 0;
                bool collison = false;
                if (speedVect.X != 0 && Math.Abs(car.position.Y - position.Y) < car.Size.Y / 2 + this.Size.Y / 2)
                {
                    sp = speedVect.X;
                    ownPos = position.X;
                    pos2 = car.position.X;

                    if (sp > 0 && pos2 - ownPos < car.Size.X / 2 + this.Size.X / 2 + distanceBetweenCars && pos2 - ownPos > 0 ||
                        sp < 0 && ownPos - pos2 < car.Size.X / 2 + this.Size.X / 2 + distanceBetweenCars && ownPos - pos2 > 0)
                    {
                        collison = true;
                    }
                }
                else if (speedVect.Y != 0 && Math.Abs(car.position.X - position.X) < car.Size.X / 2 + this.Size.X / 2)
                {
                    sp = speedVect.Y;
                    ownPos = position.Y;
                    pos2 = car.position.Y;

                    if (sp > 0 && pos2 - ownPos < car.Size.Y / 2 + this.Size.Y / 2 + distanceBetweenCars && pos2 - ownPos > 0 ||
                       sp < 0 && ownPos - pos2 < car.Size.Y / 2 + this.Size.Y / 2 + distanceBetweenCars && ownPos - pos2 > 0)
                    {
                        collison = true;
                    }
                }

                if (collison)
                {
                    if ((this.position.X + this.Size.X / 2 < car.position.X + car.Size.X / 2 &&
                                this.position.X + this.Size.X / 2 > car.position.X - car.Size.X / 2 &&
                                this.position.Y + this.Size.Y / 2 < car.position.Y + car.Size.Y / 2 &&
                                this.position.Y + this.Size.Y / 2 > car.position.Y - car.Size.Y / 2)
                                ||
                                (this.position.X - this.Size.X / 2 < car.position.X + car.Size.X / 2 &&
                                this.position.X - this.Size.X / 2 > car.position.X - car.Size.X / 2 &&
                                this.position.Y - this.Size.Y / 2 < car.position.Y + car.Size.Y / 2 &&
                                this.position.Y - this.Size.Y / 2 > car.position.Y - car.Size.Y / 2))
                    {

                    }
                    return collison;
                }

            }
            return false;
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

        public Point distancePoint(Car car, Point point, Car nextCar)
        {
            int x = 0;
            int y = 0;
            if (car.speedVect.X > 0)
            {
                x = nextCar.position.Y - point.Y;
                y = nextCar.position.X - point.X;
            }
            else if (car.speedVect.X < 0)
            {
                x = point.Y - nextCar.position.Y;
                y = point.X - nextCar.position.X;
            }
            else if (car.speedVect.Y > 0)
            {
                y = nextCar.position.Y - point.Y;
                x = point.X - nextCar.position.X;
            }
            else if (car.speedVect.Y < 0)
            {
                y = point.Y - nextCar.position.Y;
                x = nextCar.position.X - point.X;
            }
            Point distanceP = new Point(x, y);
            return distanceP;
        }

    }
}

