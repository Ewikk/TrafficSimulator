using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PedestrianProcess
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
        public Pedestrian(int xPos, int yPos)
        {
            position = new Point(xPos, yPos);
            init();
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
            speedVect = new Vector2(Math.Sign(nextJunction.X - position.X) * speed, Math.Sign(nextJunction.Y - position.Y) * speed);

        }

       
    }
}
