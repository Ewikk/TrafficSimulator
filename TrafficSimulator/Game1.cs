using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.Xml;
using System;
using System.DirectoryServices;
using System.Collections.Generic;
using SharpDX.Direct2D1;
using SpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using SharpDX.MediaFoundation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
//using System.Drawing;


namespace TrafficSimulator
{
    public struct Line
    {
        public Point start;
        public Point end;
        public Line(int startX, int startY, int endX, int endY)
        {
            start = new Point(startX, startY);
            end = new Point(endX, endY);
        }

        public static Boolean operator ==(Line line1, Line line2)
        {
            if (line1.start == line2.start && line1.end == line2.end)
                return true;
            return false;
        }
        public static Boolean operator !=(Line line1, Line line2)
        {
            if (line1 == line2) return false;
            return true;
        }

    }

    public struct CarSetup
    {
        public int startX;
        public int startY;

        public float velocityX;
        public float velocityY;

        public CarSetup(int sX, int sY, float vX, float vY)
        {
            startX = sX;
            startY = sY;
            velocityX = vX;
            velocityY = vY;
        }

    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteBatch _roadsBatch;
        private SpriteBatch _sidewalksBatch;
        private List<Rectangle> roadList = new List<Rectangle>();
        private List<Point> boundaryPoints = new List<Point>();
        private List<Rectangle> sidewalkList = new List<Rectangle>();
        private List<Line> roadLineList = new List<Line>();
        private List<Line> sidewalkLineList = new List<Line>();
        private Texture2D rect; //Texture used to draw rectangles
        private Dictionary<Point, List<Point>> roadStructure = new Dictionary<Point, List<Point>>();
        private List<Point> startingPoints = new List<Point>();
        private List<Point> endPoints = new List<Point>();
        private Dictionary<Point, List<Point>> sidewalkStructure = new Dictionary<Point, List<Point>>();
        private const string svgPath = "..\\..\\..\\final.svg";
        private const int scale = 7;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1450;
            //_graphics.PreferredBackBufferHeight = 750;
            _graphics.PreferredBackBufferHeight = 1600 * 9 / 16;
            //_graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();
            ReadSVG();
            Console.WriteLine("zaczynam pisac");
            foreach (Point punkt in boundaryPoints)
            {
                Console.WriteLine(punkt.X + " " + punkt.Y);
            }
            createRoadStructure();
            printRoadStructure();
            createPossiblePaths();
            setupCars();

            foreach (Car car in cars)
            {
                if (car != null)
                {
                    //In general, the ThreadPool is optimized for short-lived, lightweight tasks that can be executed quickly, while the TaskScheduler is better suited for longer-running, more complex tasks Task was lagging
                    //Task.Factory.StartNew(() => car.Move(roadStructure));
                    Thread thread = new Thread(() => { car.Move(roadStructure, startingPoints, endPoints); });
                    thread.Start();
                    carThreads.Add(thread);
                }
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _roadsBatch = new SpriteBatch(GraphicsDevice);
            _sidewalksBatch = new SpriteBatch(GraphicsDevice);
            rect = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            rect.SetData(new[] { Color.White });
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
                foreach (Thread thread in carThreads)
                {
                    thread.Interrupt();
                }
            }

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            foreach (Thread thread in carThreads)
            {
                thread.Interrupt();
            }
            base.OnExiting(sender, args);
        }

        public int distance(Point p1, Point p2)
        {
            return Math.Abs((p1.X - p2.X) - (p1.Y - p2.Y));
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            // TODO: Add your drawing code here
            DrawRoads();
            DrawSidewalks();
            _spriteBatch.Begin();
            foreach (Car car in cars)
            {
                Color leftBlinker = Color.Orange;
                Color rightBlinker = Color.Orange;
                int diss = distance(car.position, car.nextJunction);
                if (diss < 200)
                {
                    if (car.turn == 1)
                        rightBlinker = Color.Red;
                    else if (car.turn == -1)
                        leftBlinker = Color.Red;
                    else
                    {
                        leftBlinker = Color.Green;
                        rightBlinker = Color.Green;
                    }
                }

                int blinkerSize = 5;
                _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10, car.position.Y - 10, car.Size.X, car.Size.Y), car.color);
                if (car.speedVect.X < 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10, car.position.Y - 10, blinkerSize, blinkerSize), rightBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10, car.position.Y - 10 + 15, blinkerSize, blinkerSize), leftBlinker);

                }
                else if (car.speedVect.X > 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10 + car.Size.X - blinkerSize, car.position.Y - 10 + car.Size.Y - blinkerSize, blinkerSize, blinkerSize), rightBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10 + car.Size.X - blinkerSize, car.position.Y - 10 + car.Size.Y - blinkerSize - 15, blinkerSize, blinkerSize), leftBlinker);
                }
                if (car.speedVect.Y < 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10, car.position.Y - 10, blinkerSize, blinkerSize), leftBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10 + 15, car.position.Y - 10, blinkerSize, blinkerSize), rightBlinker);
                }
                else if (car.speedVect.Y > 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10 + car.Size.X - blinkerSize, car.position.Y - 10 + car.Size.Y - blinkerSize, blinkerSize, blinkerSize), leftBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10 + car.Size.X - blinkerSize - 15, car.position.Y - 10 + car.Size.Y - blinkerSize, blinkerSize, blinkerSize), rightBlinker);
                }

            }
            //foreach (Car car in cars)
            //{
            //    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10, car.position.Y - 10, car.Size.X, car.Size.Y), car.color);
            //}
            if (!Debugger.IsAttached)
            {
                foreach (Point start in startingPoints)
                {
                    _spriteBatch.Draw(rect, new Rectangle(start.X - 5, start.Y - 5, 10, 10), Color.Green);
                }
                //foreach (Point start in endPoints)
                //{
                //    _spriteBatch.Draw(rect, new Rectangle(start.X - 5, start.Y - 5, 10, 10), Color.Red);
                //}
                foreach (Car car in cars)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.destination.X - 5, car.destination.Y - 5, 10, 10), car.color);
                }
            }
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private Car[] cars;
        private List<Thread> carThreads = new List<Thread>();
        private void setupCars()
        {
            int carsCount = startingPoints.Count;
            cars = new Car[carsCount];
            Random random = new Random();

            int i = 0;
            foreach (Point start in startingPoints)
            {
                Random rand = new Random();
                cars[i] = new Car(start.X, start.Y, possiblePaths);
                //cars[i].setDestination(endPoints[rand.Next(endPoints.Count)]);
                cars[i].setDestination(endPoints[rand.Next(endPoints.Count)]);
                cars[i].color = new Color(random.Next(256), random.Next(256), random.Next(256), 255); ;
                i++;
                if (i == carsCount) break;
            }

        }

        private void DrawRoads()
        {
            _roadsBatch.Begin();
            foreach (Rectangle coords in roadList)
            {
                _roadsBatch.Draw(rect, coords, Color.Black);
            }
            _roadsBatch.End();
        }

        private void DrawSidewalks()
        {
            _sidewalksBatch.Begin();
            foreach (Rectangle coords in sidewalkList)
            {
                _sidewalksBatch.Draw(rect, coords, Color.Gray);
            }
            _sidewalksBatch.End();
        }

        private void createRoadStructure()
        {
            foreach (Line line in roadLineList)
            {
                if (!roadStructure.ContainsKey(line.start))
                {
                    roadStructure.Add(line.start, new List<Point>());
                }
                roadStructure[line.start].Add(line.end);
            }

            foreach (Line line in roadLineList)
            {
                if (!roadStructure.ContainsKey(line.end)) endPoints.Add(line.end);
                Boolean DupFound = false;
                foreach (var road in roadStructure)
                {
                    if (road.Value.Contains(line.start))
                    {
                        DupFound = true;
                        break;
                    }
                }
                if (!DupFound) startingPoints.Add(line.start);
            }
            if (!Debugger.IsAttached)
            {
                Console.WriteLine("Starting Points:");
                Console.WriteLine(startingPoints.Count);
                foreach (Point start in startingPoints) Console.WriteLine(start.ToString());
                Console.WriteLine("End");
            }
        }

        private void printRoadStructure()
        {
            foreach (var point in roadStructure)
            {
                Console.WriteLine(point.Key);
                List<Point> list = point.Value;
                foreach (Point endPoint in list)
                {
                    Console.WriteLine("--" + endPoint);
                }
                Console.WriteLine();
            }
        }


        private void ReadData(XmlNodeList pathNodeList)
        {
            foreach (XmlNode pathNode in pathNodeList)
            {

                XmlAttribute dAttribute = pathNode.Attributes["d"];
                XmlAttribute styleAttribute = pathNode.Attributes["style"];
                if (dAttribute != null)
                {
                    string dValue = dAttribute.Value;
                    string style = styleAttribute.Value;
                    string[] instructions = dValue.Split(' ');
                    string[] styles = style.Split(';');
                    string brush = Array.Find(styles, brush => brush.Contains("stroke-width:"));
                    String sd = brush.Split(":")[1];
                    double brushWidth = Convert.ToDouble(sd);

                    /*                    Console.WriteLine(sd);*/
                    string[] startPoint = instructions[1].Split(',');
                    string[] endPoint = instructions[instructions.Length - 1].Split(',');
                    /*                    Console.WriteLine(instructions[0]);
                                        Console.WriteLine(startPoint[0]);
                                        Console.WriteLine(endPoint[0]);*/
                    startPoint[0] = startPoint[0].Replace('.', ',');
                    startPoint[1] = startPoint[1].Replace('.', ',');
                    endPoint[0] = endPoint[0].Replace('.', ',');


                    double startX, startY, endX = -1, endY = -1;
                    startX = Convert.ToDouble(startPoint[0]);
                    startY = Convert.ToDouble(startPoint[1]);
                    if (Array.Find(instructions, element => element.Equals("V")) != null)
                    {
                        endX = startX;
                        endY = Convert.ToDouble(endPoint[0]);
                    }
                    else if (Array.Find(instructions, element => element.Equals("v")) != null)
                    {
                        endX = startX;
                        double offsetY = Convert.ToDouble(endPoint[0]);
                        endY = startY + offsetY;
                    }
                    else if (Array.Find(instructions, element => element.Equals("H")) != null)
                    {
                        endX = Convert.ToDouble(endPoint[0]);
                        endY = startY;
                    }
                    else if (Array.Find(instructions, element => element.Equals("h")) != null)
                    {
                        double offsetX = Convert.ToDouble(endPoint[0]);
                        endX = startX + offsetX;
                        endY = startY;
                    }
                    else { continue; };
                    startX *= scale;
                    startY *= scale;
                    endX *= scale;
                    endY *= scale;

                    startX = Math.Round(startX, 0);
                    startY = Math.Round(startY, 0);
                    endX = Math.Round(endX, 0);
                    endY = Math.Round(endY, 0);

                    Point startP = new Point((int)startX, (int)startY);
                    Point endP = new Point((int)endX, (int)endY);

                    if (!boundaryPoints.Contains(startP))
                    {
                        boundaryPoints.Add(startP);
                    }
                    if (!boundaryPoints.Contains(endP))
                    {
                        boundaryPoints.Add(endP);
                    }
                    addRoad(startP, endP, brushWidth);
                }
                else
                {
                    Console.WriteLine("Path was not found in SVG");
                }
            }
        }

        private void ReadSVG()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(svgPath);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("svg", "http://www.w3.org/2000/svg");

            ReadData(doc.SelectNodes("//svg:g[@id='layer1']/svg:path", nsMgr));
            ReadData(doc.SelectNodes("//svg:g[@id='layer5']/svg:path", nsMgr));
        }

        private void addRoad(Point start, Point end, double brushWidth)
        {
            brushWidth *= 7;
            Rectangle coords;
            if (start.X > end.X || start.Y > end.Y)
            {
                if (start.X > end.X)
                    coords = new Rectangle((int)(end.X - brushWidth / 2), (int)(end.Y - brushWidth / 2), (int)(start.X - end.X + brushWidth), (int)(start.Y - end.Y + brushWidth));
                else
                    coords = new Rectangle((int)(end.X - brushWidth / 2), (int)(end.Y - brushWidth / 2), (int)(start.X - end.X + brushWidth), (int)(start.Y - end.Y + brushWidth));
            }
            else
            {
                if (end.X > start.X)
                    coords = new Rectangle((int)(start.X - brushWidth / 2), (int)(start.Y - brushWidth / 2), (int)(end.X - start.X + brushWidth), (int)(end.Y - start.Y + brushWidth));
                else
                    coords = new Rectangle((int)(start.X - brushWidth / 2), (int)(start.Y - brushWidth / 2), (int)(end.X - start.X + brushWidth), (int)(end.Y - start.Y + brushWidth));
            }

            if (brushWidth == 35)
            {
                roadList.Add(coords);
                roadLineList.Add(new Line((int)start.X, (int)start.Y, (int)end.X, (int)end.Y));
            }
            else if (brushWidth == 14)
            {
                sidewalkList.Add(coords);
                sidewalkLineList.Add(new Line((int)start.X, (int)start.Y, (int)end.X, (int)end.Y));
            }
        }




        //BRUTE FORCE
        //TO DO
        Dictionary<Point, Dictionary<Point, List<Point>>> possiblePaths;
        const int bruteDepth = 20;
        private void createPossiblePaths()
        {
            possiblePaths = new Dictionary<Point, Dictionary<Point, List<Point>>>();
            Thread[] threads = new Thread[startingPoints.Count];
            int i = 0;
            foreach (Point start in startingPoints)
            {
                threads[i] = new Thread(() =>
                {
                    foreach (Point end in endPoints)
                    {
                        List<Point> path = new List<Point>();
                        path.Add(start);
                        (List<Point>, int) foundPath = checkPath((path, 0), end, bruteDepth);
                        if (!possiblePaths.ContainsKey(start))
                        {
                            possiblePaths.Add(start, new Dictionary<Point, List<Point>>());
                        }
                        possiblePaths[start].Add(end, foundPath.Item1);
                    }
                });
                i++;
            }
            foreach (Thread thread in threads) thread.Start();
            foreach (Thread thread in threads)
            {
                thread.Join();
                Console.WriteLine("Thread joined");
            }
        }
        private (List<Point>, int) checkPath((List<Point>, int) path, Point destination, int depth)
        {
            if (depth == 0)
            {
                return (path.Item1, 0);
            }
            int shortestPath = int.MaxValue;
            (List<Point>, int) result = (path.Item1, 0);
            try
            {
                foreach (Point point in roadStructure[path.Item1.Last()])
                {
                    List<Point> points = new List<Point>(path.Item1);
                    int length = path.Item2 + Math.Abs(point.X - path.Item1.Last().X + point.Y - path.Item1.Last().Y);
                    points.Add(point);
                    if (point == destination)
                        return (points, path.Item2 + length);
                    (List<Point>, int) foundPath = checkPath((points, path.Item2 + length), destination, depth - 1);
                    if (foundPath.Item2 > 0)
                    {
                        if (foundPath.Item2 < shortestPath)
                        {
                            shortestPath = foundPath.Item2;
                            result = foundPath;
                        }
                    }
                }
                return result;
            }
            catch
            {
                return (path.Item1, 0);
            }


        }
    }
}