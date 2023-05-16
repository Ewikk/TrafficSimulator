using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrafficSimulator
{
    class PedestrianThread
    {
        private int pedestrianCount = 0;
        Pedestrian[] pedestrians = null;
        List<Point> startringPoints;
        List<Point> endPoints;
        Dictionary<Point, List<Point>> sidewalkStructure;
        Dictionary<Point, Dictionary<Point, List<Point>>> sidewalkPaths;
        private Stopwatch stopwatch = new Stopwatch();
        public PedestrianThread(int count, List<Point> startingPoints, List<Point> endPoints, Dictionary<Point, Dictionary<Point, List<Point>>> paths, Dictionary<Point, List<Point>> sidewalkStructure) {
            pedestrianCount = count;
            this.startringPoints = startingPoints;
            this.endPoints = endPoints;
            this.sidewalkStructure = sidewalkStructure;
            sidewalkPaths = paths;
            pedestrians = new Pedestrian[pedestrianCount];
            SetupPedestrians();
        }

        public void Run()
        {
            stopwatch.Start();
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
                foreach (Pedestrian pedestrian in pedestrians)
                {
                    pedestrian.Move(sidewalkStructure, startringPoints, endPoints);
                }
            }
        }

        private void SetupPedestrians()
        {
            Random rand = new Random();
            for(int i = 0; i < pedestrianCount; i++)
            {
                    Point start = startringPoints[rand.Next(startringPoints.Count)];
                    pedestrians[i] = new Pedestrian(start.X, start.Y, sidewalkPaths);
                    pedestrians[i].setDestination(endPoints[rand.Next(endPoints.Count)]);
                    pedestrians[i].color = new Color(rand.Next(256), rand.Next(256), rand.Next(256), 255);
            }
        }


    }
}
