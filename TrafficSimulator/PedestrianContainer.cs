using Microsoft.Xna.Framework;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace TrafficSimulator
{
    public class PedestrianContainer
    {
        private int pedestrianCount = 0;
        public Pedestrian[] pedestrians = null;
        private List<Point> startingPoints;
        private List<Point> endPoints;
        private Dictionary<Point, List<Point>> sidewalkStructure;
        private Dictionary<Point, Dictionary<Point, List<Point>>> sidewalkPaths;
        private Stopwatch stopwatch = new Stopwatch();
        private Vector2 Size = new Vector2(10, 10);
        private Dictionary<Point, TrafficLight> trafficLights;
        private List<Tram> trams;
        public PedestrianContainer(int count, List<Point> startingPoints, List<Point> endPoints, Dictionary<Point, Dictionary<Point, List<Point>>> paths, Dictionary<Point, List<Point>> sidewalkStructure, Dictionary<Point, TrafficLight> trafficLights, List<Tram> tram)
        {
            pedestrianCount = count;
            this.startingPoints = startingPoints;
            this.endPoints = endPoints;
            this.sidewalkStructure = sidewalkStructure;
            this.trafficLights = trafficLights;
            sidewalkPaths = paths;
            pedestrians = new Pedestrian[pedestrianCount];
            trams = tram;
            SetupPedestrians();
        }

        private void SetupPedestrians()
        {
            Random rand = new Random();
            for (int i = 0; i < pedestrianCount; i++)
            {
                Point start = startingPoints[rand.Next(startingPoints.Count)];
                pedestrians[i] = new Pedestrian(start.X, start.Y, sidewalkPaths);
                pedestrians[i].setDestination(endPoints[rand.Next(endPoints.Count)]);
                pedestrians[i].color = new Color(rand.Next(256), rand.Next(256), rand.Next(256), 255);

                pedestrians[i].nextJunction = sidewalkStructure[pedestrians[i].position][rand.Next(sidewalkStructure[pedestrians[i].position].Count)];
                pedestrians[i].speedVect = new Vector2(Math.Sign(pedestrians[i].nextJunction.X - pedestrians[i].position.X) * pedestrians[i].speed, Math.Sign(pedestrians[i].nextJunction.Y - pedestrians[i].position.Y) * pedestrians[i].speed);
            }
        }

       

        public bool isMoveAllowed(Pedestrian pedestrian, int distanceBetween, List<Tram> trams)
        {
            return isLightGreen(pedestrian, distanceBetween) && !beforeTram(pedestrian, distanceBetween, trams);
        }
        private bool beforeTram(Pedestrian pedastrian, int distanceBetweenCars, List<Tram> trams)
        {
            foreach (Tram tram in trams)
            {
                double sp = 0;
                int ownPos = 0;
                int pos2 = 0;
                if (pedastrian.speedVect.Y != 0 && Math.Abs(tram.position.X - pedastrian.position.X) < tram.Size.X / 2 + this.Size.X / 2)
                {
                    sp = pedastrian.speedVect.Y;
                    ownPos = pedastrian.position.Y;
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
        private bool isLightGreen(Pedestrian pedestrian,int distanceBetween)
        {
           
                if (trafficLights.ContainsKey(pedestrian.nextJunction) && !trafficLights[pedestrian.nextJunction].isOpen && distance(pedestrian.nextJunction, pedestrian.position) < distanceBetween && distance(pedestrian.nextJunction, pedestrian.position) > distanceBetween / 2)
                {
                    return false;
                }
           
            return true;
        }

        public int distance(Point p1, Point p2)
        {
            return Math.Abs((p1.X - p2.X) - (p1.Y - p2.Y));
        }

            }
}
