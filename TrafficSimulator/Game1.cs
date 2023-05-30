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
using SharpDX.Direct3D9;
using System.Drawing.Drawing2D;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ExtendedXmlSerializer.Configuration;
using ExtendedXmlSerializer;
using System.Reflection;
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

    public class TrafficLight
    {
        public bool isOpen = true;
        public Point start;
        public Point end;
        public Point drawPos;
        public TrafficLight(Point start, Point end, Point drawPos)
        {
            this.start = start;
            this.end = end;
            this.drawPos = drawPos;
        }
        public void switchLight()
        {
            isOpen = !isOpen;
        }

    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteBatch _roadsBatch;
        private SpriteBatch _sidewalksBatch;
        private SpriteBatch _tramBatch;
        private SpriteBatch _pedestrianBatch;
        private SpriteBatch _testingBatch; // to be separated;
        private List<Rectangle> roadList = new List<Rectangle>();
        private List<Point> boundaryPoints = new List<Point>();
        private List<Rectangle> sidewalkList = new List<Rectangle>();
        private List<Line> roadLineList = new List<Line>();
        private List<Line> sidewalkLineList = new List<Line>();
        private List<Rectangle> tramList = new List<Rectangle>();
        private List<Line> tramLineList = new List<Line>();
        private Texture2D rect; //Texture used to draw rectangles
        private Texture2D circle; //Texture used to draw circles
        private Dictionary<Point, List<Point>> roadStructure = new Dictionary<Point, List<Point>>();
        //simea
        private Dictionary<Point, List<Point>> tramStructure = new Dictionary<Point, List<Point>>();
        //private List<Point> startingPoints = new List<Point>();
        //private List<Point> endPoints = new List<Point>();
        private List<Point> tramStartingPoints = new List<Point>();
        //=======
        private List<Point> roadStartingPoints = new List<Point>();
        private List<Point> roadEndPoints = new List<Point>();
        private List<Point> sidewalkStartingPoints = new List<Point>();
        private List<Point> sidewalkEndPoints = new List<Point>();
        private List<Rectangle> pedCrossingsLights = new List<Rectangle>();
        private List<Rectangle> pedCrossingsNormal = new List<Rectangle>();
        //>>>>>>> master
        private Dictionary<Point, List<Point>> sidewalkStructure = new Dictionary<Point, List<Point>>();
        private const string svgPath = "..\\..\\..\\final.svg";
        private const int scale = 7;
        private Dictionary<Point, Dictionary<Point, List<Point>>> roadPaths;
        const int roadBruteDepth = 25;
        private Dictionary<Point, Dictionary<Point, List<Point>>> sidewalkPaths;
        const int sidewalkBruteDepth = 12;
        //NEW FEATURE TESTING
        private Dictionary<Point, TrafficLight>[] TrafficLightsZones;/* = new Dictionary<Point, TrafficLight>();*/
        private Dictionary<Point, TrafficLight> pedestriansLights = new Dictionary<Point, TrafficLight>();
        private static readonly Color TrafficLightsArea1Color = new Color(0, 128, 0);
        private static readonly Color TrafficLightsArea2Color = new Color(255, 0, 0);
        private static readonly Color pedCrossingColor = new Color(0xFF, 0xAA, 0xAA);
        private static readonly Color pedCrossingLightsColor = new Color(0xFF, 0x55, 0x55);

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1470;
            _graphics.PreferredBackBufferHeight = 832;
            //_graphics.PreferredBackBufferHeight = 1600 * 9 / 16;
            //_graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }
        private byte[] bytes;

        protected void SerializePaths(ref Dictionary<Point, Dictionary<Point, List<Point>>> paths, Dictionary<Point,
            List<Point>> structure, List<Point> startingPoints,
            List<Point> endPoints, int bruteDepth, string filename)
        {
            paths = createPossiblePaths(structure, startingPoints, endPoints, bruteDepth);
            var serializer = new ConfigurationContainer().UseOptimizedNamespaces().Create();
            string xml = serializer.Serialize(new XmlWriterSettings { Indent = true }, paths);
            bytes = Encoding.ASCII.GetBytes(xml);
            using (StreamWriter writer = new StreamWriter("../../../../" + filename + ".xml"))
            {
                writer.WriteLine(xml);
            }
        }
        protected Dictionary<Point, Dictionary<Point, List<Point>>> DeserializePaths(string filename)
        {
            Dictionary<Point, Dictionary<Point, List<Point>>> paths;
            using (XmlReader reader = XmlReader.Create(new StreamReader("../../../../" + filename + ".xml")))
            {
                var serializer = new ConfigurationContainer()
              .UseOptimizedNamespaces() //If you want to have all namespaces in root element
              .Create();
                paths = (Dictionary<Point, Dictionary<Point, List<Point>>>)serializer.Deserialize(reader);
            }
            return paths;
        }
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();

            TrafficLightsZones = new Dictionary<Point, TrafficLight>[2];
            for (int i = 0; i < TrafficLightsZones.Length; i++)
            {
                TrafficLightsZones[i] = new Dictionary<Point, TrafficLight>();
            }
            ReadSVG();
            Console.WriteLine("zaczynam pisac");
            foreach (Point punkt in boundaryPoints)
            {
                Console.WriteLine(punkt.X + " " + punkt.Y);
            }
            createMovementStructure(roadLineList, roadStructure, roadStartingPoints, roadEndPoints);
            createMovementStructure(sidewalkLineList, sidewalkStructure, sidewalkStartingPoints, sidewalkEndPoints);
            printRoadStructure();
            //<<<<<<< simea
            //createPossiblePaths();
            //tramStartingPoints.Add(new Point(1085, 406));
            //tramStartingPoints.Add(new Point(105, 455));
            setupTrams();
            //=======

            //#################################################################################################################
            //roadPaths = createPossiblePaths(roadStructure, roadStartingPoints, roadEndPoints, roadBruteDepth);
            //var serializer = new ConfigurationContainer().UseOptimizedNamespaces().Create();
            //string xml = serializer.Serialize(new XmlWriterSettings { Indent = true }, roadPaths);
            //bytes = Encoding.ASCII.GetBytes(xml);
            //using (StreamWriter writer = new StreamWriter("../../../../roadPaths.xml"))
            //{
            //    writer.WriteLine(xml);
            //}

            if (!Debugger.IsAttached)
            {
                SerializePaths(ref roadPaths, roadStructure, roadStartingPoints, roadEndPoints, roadBruteDepth, "roadPaths");
                SerializePaths(ref sidewalkPaths, sidewalkStructure, sidewalkStartingPoints, sidewalkEndPoints, sidewalkBruteDepth, "sidewalkPaths");
            }
            else
            {
                roadPaths = DeserializePaths("roadPaths");
                sidewalkPaths = DeserializePaths("sidewalkPaths");
            }

            //sidewalkPaths = createPossiblePaths(sidewalkStructure, sidewalkStartingPoints, sidewalkEndPoints, sidewalkBruteDepth);
            //>>>>>>> master
            //setupPedestrians();
            //setupCars();
            pedestrians = new PedestrianThread(100, sidewalkStartingPoints, sidewalkEndPoints, sidewalkPaths, sidewalkStructure, pedestriansLights, trams);
            pedestrianThread = new Thread(() => { pedestrians.Run(); });



            foreach (Thread thread in carThreads) thread.Start();
            pedestrianThread.Start();




            //tramStructure.Add(new Point(0, 0), new List<Point>());
            foreach (Tram tram in trams)
            {
                if (tram != null)
                {
                    //In general, the ThreadPool is optimized for short-lived, lightweight tasks that can be executed quickly, while the TaskScheduler is better suited for longer-running, more complex tasks Task was lagging
                    //Task.Factory.StartNew(() => car.Move(roadStructure));
                    Thread thread = new Thread(() => { tram.Move(); });
                    thread.Start();
                    tramThreads.Add(thread);
                }
            }
            Task.Factory.StartNew(ListenForClients);
            Task.Factory.StartNew(ReceiveCarData);
        }

        //private Car[] cars;
        private List<Car> cars = new List<Car>();
        private List<Thread> carThreads = new List<Thread>();
        //private Pedestrian[] pedestrians;
        private PedestrianThread pedestrians;
        private Thread pedestrianThread;
        //private void setupCars()
        //{
        //    int carsCount = roadStartingPoints.Count;
        //    //carsCount = 2;
        //    cars = new Car[carsCount];
        //    Random random = new Random();

        //    int i = 0;
        //    if (carsCount == 0) return;
        //    foreach (Point start in roadStartingPoints)
        //    {
        //        Random rand = new Random();
        //        cars[i] = new Car(start.X, start.Y, roadPaths);
        //        //cars[i].setDestination(roadEndPoints[rand.Next(roadEndPoints.Count)]);
        //        cars[i].setDestination(roadEndPoints[rand.Next(roadEndPoints.Count)]);
        //        cars[i].color = new Color(random.Next(256), random.Next(256), random.Next(256), 255);
        //        //TEMP
        //        cars[i].cars = cars;
        //        i++;
        //        if (i == carsCount) break;
        //    }
        //    foreach (Car car in cars)
        //    {
        //        if (car != null)
        //        {
        //            //In general, the ThreadPool is optimized for short-lived, lightweight tasks that can be executed quickly, while the TaskScheduler is better suited for longer-running, more complex tasks Task was lagging
        //            //Task.Factory.StartNew(() => car.Move(roadStructure));
        //            Thread thread = new Thread(() => { car.Move(roadStructure, roadStartingPoints, roadEndPoints, TrafficLightsZones, trams, pedestrians); });
        //            carThreads.Add(thread);
        //        }
        //    }

        //}

        public Tram[] trams;
        private List<Thread> tramThreads = new List<Thread>();
        private void setupTrams()
        {
            trams = new Tram[2];
            Random random = new Random();

            /*            int i = 0;
                        foreach (Point start in tramStartingPoints)
                        {
                            Random rand = new Random();
                            trams[i] = new Tram(start.X, start.Y, 200,0);
                            trams[i].color = new Color(random.Next(256), random.Next(256), random.Next(256), 255);
                            i++;
                            if (i == 2) break;
                        }*/
            Point a = new Point(1470, 406);
            Point b = new Point(0, 455);
            trams[0] = new Tram(a.X, a.Y, -200, 0);
            trams[0].color = new Color(random.Next(256), random.Next(256), random.Next(256), 255);

            trams[1] = new Tram(b.X, b.Y, 200, 0);
            trams[1].color = new Color(random.Next(256), random.Next(256), random.Next(256), 255);

        }
        //TO BE DELETED
        //private void setupPedestrians()
        //{
        //    int pedestrianCount = sidewalkStartingPoints.Count;
        //    pedestrianCount = 300;
        //    pedestrians = new Pedestrian[pedestrianCount];
        //    Random random = new Random();

        //    int i = 0;
        //    if (pedestrianCount == 0) return;
        //    Random rand = new Random();

        //    for (int j = 0; j < pedestrianCount; j++) {
        //        Point start = sidewalkStartingPoints[rand.Next(sidewalkStartingPoints.Count)];
        //        pedestrians[i] = new Pedestrian(start.X, start.Y, sidewalkPaths);
        //        //cars[i].setDestination(roadEndPoints[rand.Next(roadEndPoints.Count)]);
        //        pedestrians[i].setDestination(sidewalkEndPoints[rand.Next(sidewalkEndPoints.Count)]);
        //        pedestrians[i].color = new Color(random.Next(256), random.Next(256), random.Next(256), 255);
        //        //TEMP
        //        i++;
        //        if (i == pedestrianCount) break;
        //    }
        //    foreach (Pedestrian pedestrian in pedestrians)
        //    {
        //        Thread thread = new Thread(() => { pedestrian.Move(sidewalkStructure, sidewalkStartingPoints, sidewalkEndPoints); });
        //        thread.Start();
        //        pedestrianThreads.Add(thread);
        //    }

        //}

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _roadsBatch = new SpriteBatch(GraphicsDevice);
            _sidewalksBatch = new SpriteBatch(GraphicsDevice);
            _tramBatch = new SpriteBatch(GraphicsDevice);
            _testingBatch = new SpriteBatch(GraphicsDevice);
            _pedestrianBatch = new SpriteBatch(GraphicsDevice);
            rect = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            rect.SetData(new[] { Color.White });
            circle = this.Content.Load<Texture2D>("circle");
            // TODO: use this.Content to load your game content here
        }

        bool spacePressed = false;
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
            if (Keyboard.GetState().IsKeyDown(Keys.Space) && !spacePressed)
            {
                spacePressed = true;
                foreach (var area in TrafficLightsZones)
                {
                    foreach (var light in area)
                    {
                        light.Value.switchLight();
                    }
                }
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Space)) spacePressed = false;
            steerTrafficLights(gameTime);
            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        const double interval = 5;
        private double timeSinceLastSwitch = 0;
        private double cooldown = 0;
        private Dictionary<Point, TrafficLight> toBeSwitched;
        private void steerTrafficLights(GameTime gameTime)
        {
            if (cooldown == 0)
            {
                timeSinceLastSwitch += gameTime.ElapsedGameTime.TotalSeconds;
                if (timeSinceLastSwitch > interval)
                {
                    timeSinceLastSwitch = 0;
                    cooldown = 5;
                    foreach (var area in TrafficLightsZones)
                    {
                        if (area.First().Value.isOpen)
                        {
                            foreach (var light in area)
                            {
                                light.Value.switchLight();
                            }
                        }
                        else toBeSwitched = area;
                    }
                    foreach (var light in pedestriansLights) light.Value.switchLight();
                }
            }
            else
            {
                cooldown -= gameTime.ElapsedGameTime.TotalSeconds;
                if (cooldown <= 0 && pedestriansLights.Values.First().isOpen)
                {
                    cooldown = 1;
                    foreach (var light in pedestriansLights) light.Value.switchLight();
                }
                else if (cooldown <= 0)
                {
                    cooldown = 0;
                    foreach (var light in toBeSwitched)
                    {
                        light.Value.switchLight();
                    }
                }
            }

        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            foreach (Thread thread in carThreads)
            {
                thread.Interrupt();
            }
            foreach (Thread thread in tramThreads)
            {
                thread.Interrupt();
            }
            //foreach (Thread thread in pedestrianThreads)
            //{
            //    thread.Interrupt();
            //}
            pedestrianThread.Interrupt();
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
            DrawTram();
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
                _spriteBatch.Draw(rect, new Rectangle(car.position.X - car.Size.X / 2, car.position.Y - car.Size.Y / 2, car.Size.X, car.Size.Y), car.color);
                if (car.speedVect.X < 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - car.Size.X / 2, car.position.Y - car.Size.Y / 2, blinkerSize, blinkerSize), rightBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - car.Size.X / 2, car.position.Y - car.Size.Y / 2 + 15, blinkerSize, blinkerSize), leftBlinker);

                }
                else if (car.speedVect.X > 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - car.Size.X / 2 + car.Size.X - blinkerSize, car.position.Y - car.Size.Y / 2 + car.Size.Y - blinkerSize, blinkerSize, blinkerSize), rightBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - car.Size.X / 2 + car.Size.X - blinkerSize, car.position.Y - car.Size.Y / 2 + car.Size.Y - blinkerSize - 15, blinkerSize, blinkerSize), leftBlinker);
                }
                if (car.speedVect.Y < 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - car.Size.X / 2, car.position.Y - car.Size.Y / 2, blinkerSize, blinkerSize), leftBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - car.Size.X / 2 + 15, car.position.Y - car.Size.Y / 2, blinkerSize, blinkerSize), rightBlinker);
                }
                else if (car.speedVect.Y > 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - car.Size.X / 2 + car.Size.X - blinkerSize, car.position.Y - car.Size.Y / 2 + car.Size.Y - blinkerSize, blinkerSize, blinkerSize), leftBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(car.position.X - car.Size.X / 2 + car.Size.X - blinkerSize - 15, car.position.Y - car.Size.Y / 2 + car.Size.Y - blinkerSize, blinkerSize, blinkerSize), rightBlinker);
                }

            }

            foreach (Tram tram in trams)
            {
                Color leftBlinker = Color.Orange;
                Color rightBlinker = Color.Orange;
                int blinkerSize = 5;
                //Console.WriteLine(tram.position.ToString());
                //_spriteBatch.Draw(rect, new Rectangle(tram.position.X - 5, tram.position.Y - 5,10, 10), tram.color);
                _spriteBatch.Draw(rect, new Rectangle(tram.position.X - tram.Size.X / 2, tram.position.Y - tram.Size.Y / 2, tram.Size.X, tram.Size.Y), tram.color);
                if (tram.speedVect.X < 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(tram.position.X - tram.Size.X / 2, tram.position.Y - tram.Size.Y / 2, blinkerSize, blinkerSize), rightBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(tram.position.X - tram.Size.X / 2, tram.position.Y - tram.Size.Y / 2 + 25, blinkerSize, blinkerSize), leftBlinker);

                }
                else if (tram.speedVect.X > 0)
                {
                    _spriteBatch.Draw(rect, new Rectangle(tram.position.X - tram.Size.X / 2 + tram.Size.X - blinkerSize, tram.position.Y - tram.Size.Y / 2 + tram.Size.Y - blinkerSize, blinkerSize, blinkerSize), rightBlinker);
                    _spriteBatch.Draw(rect, new Rectangle(tram.position.X - tram.Size.X / 2 + tram.Size.X - blinkerSize, tram.position.Y - tram.Size.Y / 2 + tram.Size.Y - blinkerSize - 25, blinkerSize, blinkerSize), leftBlinker);
                }

            }

            //foreach (Car car in cars)
            //{
            //    _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10, car.position.Y - 10, car.Size.X, car.Size.Y), car.color);
            //}
            if (!Debugger.IsAttached)
            {
                foreach (Point start in roadStartingPoints)
                {
                    _spriteBatch.Draw(rect, new Rectangle(start.X - 5, start.Y - 5, 10, 10), Color.Green);
                }
                //foreach (Point start in sidewalkStartingPoints)
                //{
                //    _spriteBatch.Draw(rect, new Rectangle(start.X - 5, start.Y - 5, 10, 10), Color.Green);
                //}
                //foreach (Point end in sidewalkEndPoints)
                //{
                //    _spriteBatch.Draw(rect, new Rectangle(end.X - 5, end.Y - 5, 10, 10), Color.Red);
                //}
                //foreach (Point start in roadEndPoints)
                //{
                //    _spriteBatch.Draw(rect, new Rectangle(start.X - 5, start.Y - 5, 10, 10), Color.Red);
                //}
                foreach (Car car in cars)
                {
                    _spriteBatch.Draw(rect, new Rectangle(car.destination.X - 5, car.destination.Y - 5, 10, 10), car.color);
                }
            }
            _spriteBatch.End();

            _pedestrianBatch.Begin();
            int pedestrianSize = 10;
            foreach (Pedestrian pedestrian in pedestrians.pedestrians)
            {
                _pedestrianBatch.Draw(circle, new Rectangle(pedestrian.position.X - pedestrianSize / 2, pedestrian.position.Y - pedestrianSize / 2, pedestrianSize, pedestrianSize), pedestrian.color);
            }
            _pedestrianBatch.End();
            _testingBatch.Begin();
            foreach (var area in TrafficLightsZones)
            {
                foreach (var light in area)
                {
                    if (light.Value.isOpen)
                        _testingBatch.Draw(circle, new Rectangle(light.Value.drawPos.X - 10, light.Value.drawPos.Y - 10, 20, 20), Color.Green);
                    else
                        _testingBatch.Draw(circle, new Rectangle(light.Value.drawPos.X - 10, light.Value.drawPos.Y - 10, 20, 20), Color.Red);
                }
            }
            _testingBatch.End();
            base.Draw(gameTime);
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

            foreach (Rectangle coords in pedCrossingsLights)
            {
                if (pedestriansLights.Values.First().isOpen)
                    _sidewalksBatch.Draw(rect, coords, Color.LightGreen);
                else
                    _sidewalksBatch.Draw(rect, coords, pedCrossingColor);
            }
            foreach (Rectangle coords in pedCrossingsNormal) _sidewalksBatch.Draw(rect, coords, Color.LightGreen);
            foreach (Rectangle coords in sidewalkList)
            {
                if (pedCrossingsNormal.Contains(coords)) continue;
                if (pedCrossingsLights.Contains(coords)) continue;
                _sidewalksBatch.Draw(rect, coords, Color.Gray);
            }
            _sidewalksBatch.End();
        }

        private void DrawTram()
        {
            _tramBatch.Begin();
            foreach (Rectangle coords in tramList)
            {
                int width = coords.Width;
                int height = coords.Height / 3;
                int x = coords.X;
                int y1 = coords.Y;
                int y3 = coords.Y + 2 * height;
                _tramBatch.Draw(rect, new Rectangle(x, y1, width, height), Color.Brown);
                _tramBatch.Draw(rect, new Rectangle(x, y3, width, height), Color.Brown);
            }
            _tramBatch.End();
        }

        //private void createRoadStructure()
        //private void createRoadStructure()
        //{
        //    foreach (Line line in roadLineList)
        //    {
        //        if (!roadStructure.ContainsKey(line.start))
        //        {
        //            roadStructure.Add(line.start, new List<Point>());
        //        }
        //        roadStructure[line.start].Add(line.end);
        //    }

        //    foreach (Line line in roadLineList)
        //    {
        //        if (!roadStructure.ContainsKey(line.end)) roadEndPoints.Add(line.end);
        //        Boolean DupFound = false;
        //        foreach (var road in roadStructure)
        //        {
        //            if (road.Value.Contains(line.start))
        //            {
        //                DupFound = true;
        //                break;
        //            }
        //        }
        //        if (!DupFound) roadStartingPoints.Add(line.start);
        //    }
        //    if (!Debugger.IsAttached)
        //    {
        //        Console.WriteLine("Starting Points:");
        //        Console.WriteLine(roadStartingPoints.Count);
        //        foreach (Point start in roadStartingPoints) Console.WriteLine(start.ToString());
        //        Console.WriteLine("End");
        //    }
        //}

        private void createMovementStructure(List<Line> lineList, Dictionary<Point, List<Point>> structure, List<Point> startingPoints, List<Point> endPoints)
        {
            foreach (Line line in lineList)
            {
                if (!structure.ContainsKey(line.start))
                {
                    structure.Add(line.start, new List<Point>());
                }
                structure[line.start].Add(line.end);
            }

            foreach (Line line in lineList)
            {
                if (!structure.ContainsKey(line.end)) endPoints.Add(line.end);
                Boolean DupFound = false;
                foreach (var road in structure)
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

                    string strokeColor = Array.Find(styles, brush => brush.Contains("stroke:"));
                    string brushColorString = strokeColor.Split(":")[1];
                    brushColorString = brushColorString.Substring(1);
                    int r = int.Parse(brushColorString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    int g = int.Parse(brushColorString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    int b = int.Parse(brushColorString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    Color brushColor = new Color(r, g, b);
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
                    addRoad(startP, endP, brushWidth, brushColor);
                    if (brushColor == TrafficLightsArea1Color || brushColor == TrafficLightsArea2Color || brushColor == pedCrossingLightsColor)
                    {
                        int xDiff = (int)(endX - startX) / 2;
                        int yDiff = (int)(endY - startY) / 2;
                        int xCenter = (int)startX + xDiff;
                        int yCenter = (int)startY + yDiff;
                        if (brushColor == TrafficLightsArea1Color)
                            TrafficLightsZones[0].Add(startP, new TrafficLight(startP, endP, new Point(xCenter, yCenter)));
                        else if (brushColor == TrafficLightsArea2Color)
                        {
                            TrafficLightsZones[1].Add(startP, new TrafficLight(startP, endP, new Point(xCenter, yCenter)));
                            TrafficLightsZones[1][startP].switchLight();
                        }
                        else
                        {
                            //pedCrossings.Add(new Line((int)startX, (int)startY, (int)endX, (int)endY));
                            pedestriansLights.Add(startP, new TrafficLight(startP, endP, new Point(xCenter, yCenter)));
                            pedestriansLights[startP].switchLight();
                        }
                    }
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
            ReadData(doc.SelectNodes("//svg:g[@id='layer2']/svg:path", nsMgr));
        }

        private void addRoad(Point start, Point end, double brushWidth, Color color)
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
            else if (brushWidth == 21)
            {
                tramList.Add(coords);
                tramLineList.Add(new Line((int)start.X, (int)start.Y, (int)end.X, (int)end.Y));
            }
            if (color == pedCrossingColor) pedCrossingsNormal.Add(coords);
            else if (color == pedCrossingLightsColor) pedCrossingsLights.Add(coords);
        }






        //BRUTE FORCE
        //TO DO
        const int SidewalkBruteDepth = 25;
        private Dictionary<Point, Dictionary<Point, List<Point>>> createPossiblePaths(Dictionary<Point, List<Point>> structure, List<Point> startingPoints, List<Point> endPoints, int depth = 25)
        {
            Dictionary<Point, Dictionary<Point, List<Point>>> possiblePaths = new Dictionary<Point, Dictionary<Point, List<Point>>>();
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
                        (List<Point>, int) foundPath = checkPath(structure, (path, 0), end, depth, int.MaxValue);
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
            Console.WriteLine(iter);
            return possiblePaths;
        }
        public int iter = 0;
        private (List<Point>, int) checkPath(Dictionary<Point, List<Point>> structure, (List<Point>, int) path, Point destination, int depth, int bestLength)
        {
            iter++;
            if (depth == 0 /*|| path.Item2 > bestLength*/)
            {
                return (path.Item1, 0);
            }
            int shortestPath = bestLength;
            (List<Point>, int) result = (path.Item1, 0);
            try
            {
                foreach (Point point in structure[path.Item1.Last()])
                {
                    List<Point> points = new List<Point>(path.Item1);
                    int length = path.Item2 + Math.Abs(point.X - path.Item1.Last().X + point.Y - path.Item1.Last().Y);
                    points.Add(point);
                    if (point == destination)
                        return (points, path.Item2 + length);
                    (List<Point>, int) foundPath = checkPath(structure, (points, path.Item2 + length), destination, depth - 1, shortestPath);
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

        private UdpClient mainServer = new UdpClient(13131);
        private UdpClient carServer = new UdpClient(15000);
        private List<IPEndPoint> clients = new List<IPEndPoint>();

        protected void ListenForClients()
        {
            var serializer = new ConfigurationContainer()
            .UseOptimizedNamespaces() //If you want to have all namespaces in root element
            .Create();

            while (true)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = mainServer.Receive(ref endPoint);
                string receivedMess = Encoding.ASCII.GetString(buffer);
                if (receivedMess == "Con")
                {
                    byte[] port = BitConverter.GetBytes((int)((IPEndPoint)carServer.Client.LocalEndPoint).Port);
                    mainServer.Send(port, port.Length, endPoint);
                    clients.Add(endPoint);

                    Console.WriteLine(endPoint.ToString() + " connected to server");

                    Random rand = new Random();
                    Point start = roadStartingPoints[0];
                    Car newCar = new Car(start.X, start.Y);
                    newCar.cars = cars;
                    cars.Add(newCar);
                    byte[] startingPos = new byte[2 * sizeof(int)];
                    Buffer.BlockCopy(BitConverter.GetBytes(start.X), 0, startingPos, 0, sizeof(int));
                    Buffer.BlockCopy(BitConverter.GetBytes(start.Y), 0, startingPos, sizeof(int), sizeof(int));
                    mainServer.Send(startingPos, startingPos.Length, endPoint);
                    Point dest = roadEndPoints[0];
                    byte[] destination = new byte[2 * sizeof(int)];
                    newCar.setPath(roadPaths[start][dest]);
                    Buffer.BlockCopy(BitConverter.GetBytes(dest.X), 0, destination, 0, sizeof(int));
                    Buffer.BlockCopy(BitConverter.GetBytes(dest.Y), 0, destination, sizeof(int), sizeof(int));
                    mainServer.Send(destination, destination.Length, endPoint);
                }
                else if (receivedMess == "Dis")
                {
                    clients.Remove(endPoint);
                    Console.WriteLine(endPoint.ToString() + " disconnected from the server");
                }
            }
        }

        //private void setupCars()
        //{
        //    int carsCount = roadStartingPoints.Count;
        //    //carsCount = 2;
        //    cars = new Car[carsCount];
        //    Random random = new Random();

        //    int i = 0;
        //    if (carsCount == 0) return;
        //    foreach (Point start in roadStartingPoints)
        //    {
        //        Random rand = new Random();
        //        cars[i] = new Car(start.X, start.Y, roadPaths);
        //        //cars[i].setDestination(roadEndPoints[rand.Next(roadEndPoints.Count)]);
        //        cars[i].setDestination(roadEndPoints[rand.Next(roadEndPoints.Count)]);
        //        cars[i].color = new Color(random.Next(256), random.Next(256), random.Next(256), 255);
        //        //TEMP
        //        cars[i].cars = cars;
        //        i++;
        //        if (i == carsCount) break;
        //    }
        //    foreach (Car car in cars)
        //    {
        //        if (car != null)
        //        {
        //            //In general, the ThreadPool is optimized for short-lived, lightweight tasks that can be executed quickly, while the TaskScheduler is better suited for longer-running, more complex tasks Task was lagging
        //            //Task.Factory.StartNew(() => car.Move(roadStructure));
        //            Thread thread = new Thread(() => { car.Move(roadStructure, roadStartingPoints, roadEndPoints, TrafficLightsZones, trams, pedestrians); });
        //            carThreads.Add(thread);
        //        }
        //    }

        //}

        protected void ReceiveCarData()
        {
            while (true)
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = carServer.Receive(ref endPoint);
                string header = Encoding.ASCII.GetString(data, 0, 3);
                endPoint.Port -= 1;
                int index = clients.FindIndex(x => x.Equals(endPoint));
                switch (header)
                {
                    case "POS":
                        {
                            int xPos = BitConverter.ToInt32(data, 3);
                            int yPos = BitConverter.ToInt32(data, 3 + sizeof(int));

                            if (cars[index].IsMoveAllowed(TrafficLightsZones, roadStructure, trams, pedestrians))
                            {
                                cars[index].setPosition(xPos, yPos);
                                mainServer.Send(Encoding.ASCII.GetBytes("YES"), 3, clients[index]);
                            }
                            else mainServer.Send(Encoding.ASCII.GetBytes("NO"), 2, clients[index]);
                            break;
                        }
                    case "DES":
                        {
                            byte[] newStartDest = new byte[4 * sizeof(int)];
                            Random random = new Random();
                            Point newStart = roadStartingPoints[random.Next(roadStartingPoints.Count)];
                            Point newDest = roadEndPoints[random.Next(roadEndPoints.Count)];
                            cars[index].setPosition(newStart);
                            //cars[index].setDestination(newDest);
                            Buffer.BlockCopy(BitConverter.GetBytes(newStart.X), 0, newStartDest, 0, sizeof(int));
                            Buffer.BlockCopy(BitConverter.GetBytes(newStart.Y), 0, newStartDest, sizeof(int), sizeof(int));
                            Buffer.BlockCopy(BitConverter.GetBytes(newDest.X), 0, newStartDest, 2 * sizeof(int), sizeof(int));
                            Buffer.BlockCopy(BitConverter.GetBytes(newDest.Y), 0, newStartDest, 3 * sizeof(int), sizeof(int));
                            mainServer.Send(newStartDest, newStartDest.Length, clients[index]);
                            cars[index].setPath(roadPaths[newStart][newDest]);
                            break;
                        }
                    case "NEJ":
                        {
                            int xPos = BitConverter.ToInt32(data, 3);
                            int yPos = BitConverter.ToInt32(data, 3 + sizeof(int));
                            cars[index].setNextJunction(new Point(xPos, yPos));
                            break;
                        }
                }
                
            }
        }

        //protected void SendPainting()
        //{
        //    while (true)
        //    {
        //        Tuple<byte[], int> tmp = paintingData.Take();
        //        byte[] data = new byte[tmp.Item1.Length + 1];
        //        data[0] = (byte)tmp.Item2;
        //        Buffer.BlockCopy(tmp.Item1, 0, data, 1, tmp.Item1.Length);
        //        foreach (var client in clients)
        //        {
        //            paintSever.Send(data, data.Length, client);
        //        }

        //    }
        //}



        ////BRUTE FORCE
        ////TO DO
        //Dictionary<Point, Dictionary<Point, List<Point>>> possiblePaths;
        //const int bruteDepth = 25;
        //private void createPossiblePaths()
        //{
        //    possiblePaths = new Dictionary<Point, Dictionary<Point, List<Point>>>();
        //    Thread[] threads = new Thread[roadStartingPoints.Count];
        //    int i = 0;
        //    foreach (Point start in roadStartingPoints)
        //    {
        //        threads[i] = new Thread(() =>
        //        {
        //            foreach (Point end in roadEndPoints)
        //            {
        //                List<Point> path = new List<Point>();
        //                path.Add(start);
        //                (List<Point>, int) foundPath = checkPath((path, 0), end, bruteDepth, int.MaxValue);
        //                if (!possiblePaths.ContainsKey(start))
        //                {
        //                    possiblePaths.Add(start, new Dictionary<Point, List<Point>>());
        //                }
        //                possiblePaths[start].Add(end, foundPath.Item1);
        //            }
        //        });
        //        i++;
        //    }
        //    foreach (Thread thread in threads) thread.Start();
        //    foreach (Thread thread in threads)
        //    {
        //        thread.Join();
        //        Console.WriteLine("Thread joined");
        //    }
        //    Console.WriteLine(iter);
        //}
        //public int iter = 0;
        //private (List<Point>, int) checkPath((List<Point>, int) path, Point destination, int depth, int bestLength)
        //{
        //    iter++;
        //    if (depth == 0 /*|| path.Item2 > bestLength*/)
        //    {
        //        return (path.Item1, 0);
        //    }
        //    int shortestPath = bestLength;
        //    (List<Point>, int) result = (path.Item1, 0);
        //    try
        //    {
        //        foreach (Point point in roadStructure[path.Item1.Last()])
        //        {
        //            List<Point> points = new List<Point>(path.Item1);
        //            int length = path.Item2 + Math.Abs(point.X - path.Item1.Last().X + point.Y - path.Item1.Last().Y);
        //            points.Add(point);
        //            if (point == destination)
        //                return (points, path.Item2 + length);
        //            (List<Point>, int) foundPath = checkPath((points, path.Item2 + length), destination, depth - 1, shortestPath);
        //            if (foundPath.Item2 > 0)
        //            {
        //                if (foundPath.Item2 < shortestPath)
        //                {
        //                    shortestPath = foundPath.Item2;
        //                    result = foundPath;
        //                }
        //            }
        //        }
        //        return result;
        //    }
        //    catch
        //    {
        //        return (path.Item1, 0);
        //    }


        //}
    }
}