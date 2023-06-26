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
using Sprache;
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
        private Dictionary<Point, List<Point>> tramStructure = new Dictionary<Point, List<Point>>();

        private List<Point> tramStartingPoints = new List<Point>();
        private List<Point> roadStartingPoints = new List<Point>();
        private List<Point> roadEndPoints = new List<Point>();
        private List<Point> sidewalkStartingPoints = new List<Point>();
        private List<Point> sidewalkEndPoints = new List<Point>();
        private List<Rectangle> pedCrossingsLights = new List<Rectangle>();
        private List<Rectangle> pedCrossingsNormal = new List<Rectangle>();
        private Dictionary<Point, List<Point>> sidewalkStructure = new Dictionary<Point, List<Point>>();
        private const string svgPath = "..\\..\\..\\final.svg";
        private const int scale = 7;
        private Dictionary<Point, Dictionary<Point, List<Point>>> roadPaths;
        const int roadBruteDepth = 25;
        private Dictionary<Point, Dictionary<Point, List<Point>>> sidewalkPaths;
        const int sidewalkBruteDepth = 12;
        private Dictionary<Point, TrafficLight>[] TrafficLightsZones;
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
            //_graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }
        private byte[] bytes;

        protected void CreateSerializePaths(ref Dictionary<Point, Dictionary<Point, List<Point>>> paths, Dictionary<Point,
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
              .UseOptimizedNamespaces()
              .Create();
                paths = (Dictionary<Point, Dictionary<Point, List<Point>>>)serializer.Deserialize(reader);
            }
            return paths;
        }

        protected void CreateSerializeStructure(List<Line> lineList, Dictionary<Point, List<Point>> structure, List<Point> startingPoints, List<Point> endPoints, string filename)
        {
            createMovementStructure(lineList, structure, startingPoints, endPoints);
            var serializer = new ConfigurationContainer().UseOptimizedNamespaces().Create();
            string xml = serializer.Serialize(new XmlWriterSettings { Indent = true }, structure);
            bytes = Encoding.ASCII.GetBytes(xml);
            using (StreamWriter writer = new StreamWriter("../../../../" + filename + ".xml"))
            {
                writer.WriteLine(xml);
            }
        }
        protected override void Initialize()
        {
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
            CreateSerializeStructure(roadLineList, roadStructure, roadStartingPoints, roadEndPoints, "roadStructure");
            CreateSerializeStructure(sidewalkLineList, sidewalkStructure, sidewalkStartingPoints, sidewalkEndPoints, "sidewalkStructure");
            printRoadStructure();
            

            if (!Debugger.IsAttached)
            {
                CreateSerializePaths(ref roadPaths, roadStructure, roadStartingPoints, roadEndPoints, roadBruteDepth, "roadPaths");
                CreateSerializePaths(ref sidewalkPaths, sidewalkStructure, sidewalkStartingPoints, sidewalkEndPoints, sidewalkBruteDepth, "sidewalkPaths");
            }
            else
            {
                roadPaths = DeserializePaths("roadPaths");
                sidewalkPaths = DeserializePaths("sidewalkPaths");
            }
            
            SerializeSidewalks();

            pedestrians = new PedestrianContainer(100, sidewalkStartingPoints, sidewalkEndPoints, sidewalkPaths, sidewalkStructure, pedestriansLights, trams);
            //pedestrianThread = new Thread(() => { pedestrians.Run(); });
            //foreach (Thread thread in carThreads) thread.Start();
            //pedestrianThread.Start();
            //tramStructure.Add(new Point(0, 0), new List<Point>());
            /* foreach (Tram tram in trams)
             {
                 if (tram != null)
                 {
                     //In general, the ThreadPool is optimized for short-lived, lightweight tasks that can be executed quickly, while the TaskScheduler is better suited for longer-running, more complex tasks Task was lagging
                     //Task.Factory.StartNew(() => car.Move(roadStructure));
                     Thread thread = new Thread(() => { tram.Move(); });
                     thread.Start();
                     tramThreads.Add(thread);
                 }
             }*/
            mainServer.Start();
            carServer.Start();
            pedestrianServer.Start();
            tServer.Start();

            //Task.Factory.StartNew(ListenForClients);
            ConnectionListenerThread = new Thread(() => { ListenForClients(); });
            ConnectionListenerThread.Start();
            carListenerThread = new Thread(() => { ReceiveCarData(); });
            carListenerThread.Start();
            pedestrianListenerThread = new Thread(() => { ReceivePedData(); });
            pedestrianListenerThread.Start();
            tramListenerThread = new Thread(() => { ReceiveTramData(); });
            tramListenerThread.Start();
        }
        Thread ConnectionListenerThread;
        Thread carListenerThread;
        Thread pedestrianListenerThread;
        Thread tramListenerThread;
        private List<Car> cars = new List<Car>();
        //private List<Thread> carThreads = new List<Thread>();
        private PedestrianContainer pedestrians;
        //private Thread pedestrianThread;
       

        public List<Tram> trams = new List<Tram>();
        //private List<Thread> tramThreads = new List<Thread>();

       

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
        }

        bool spacePressed = false;
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
                //foreach (Thread thread in carThreads)
                //{
                //    thread.Interrupt();
                //}
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
            //foreach (Thread thread in carThreads)
            //{
            //    thread.Interrupt();
            //}
            //foreach (Thread thread in tramThreads)
            //{
            //    thread.Interrupt();
            //}

            //pedestrianThread.Interrupt();
            ConnectionListenerThread.Interrupt();
            carListenerThread.Interrupt();
            pedestrianListenerThread.Interrupt();
            tramListenerThread.Interrupt();
            base.OnExiting(sender, args);
        }

        public int distance(Point p1, Point p2)
        {
            return Math.Abs((p1.X - p2.X) - (p1.Y - p2.Y));
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

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

            
            if (!Debugger.IsAttached)
            {
                foreach (Point start in roadStartingPoints)
                {
                    _spriteBatch.Draw(rect, new Rectangle(start.X - 5, start.Y - 5, 10, 10), Color.Green);
                }
              
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
                    string[] startPoint = instructions[1].Split(',');
                    string[] endPoint = instructions[instructions.Length - 1].Split(',');
                   
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
            if (depth == 0)
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

        private TcpListener mainServer = new TcpListener(IPAddress.Any, 13131);
        private TcpListener carServer = new TcpListener(IPAddress.Any, 15000);
        private TcpListener pedestrianServer = new TcpListener(IPAddress.Any, 16000);
        private TcpListener tServer = new TcpListener(IPAddress.Any, 17000);
        private List<TcpClient> carClients = new List<TcpClient>();
        private List<TcpClient> pedClients = new List<TcpClient>();
        private List<TcpClient> tramClients = new List<TcpClient>();

        protected void ListenForClients()
        {
            Random rand = new Random();
            int rail = rand.Next(0, 2);

            try
            {
                while (true)
                {
                    TcpClient client = mainServer.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[3];
                    stream.Read(buffer, 0, buffer.Length);
                    string receivedMess = Encoding.ASCII.GetString(buffer);

                    if (receivedMess == "Con")
                    {
                        buffer = new byte[3];
                        stream.Read(buffer, 0, buffer.Length);
                        receivedMess = Encoding.ASCII.GetString(buffer);

                        if (receivedMess == "CAR")
                        {
                            byte[] port = BitConverter.GetBytes((int)((IPEndPoint)carServer.LocalEndpoint).Port);
                            stream.Write(port, 0, port.Length);

                            TcpClient carClient = carServer.AcceptTcpClient();
                            carClient.GetStream();
                            carThreadStop = true;
                            carListenerThread.Join();
                            carThreadStop = false;
                            carClients.Add(carClient);
                            carListenerThread = new Thread(() => { ReceiveCarData(); });
                            carListenerThread.Start();

                            Console.WriteLine(carClient.Client.RemoteEndPoint.ToString() + " connected to server");

                            Point start = roadStartingPoints[rand.Next(roadStartingPoints.Count)];
                            Car newCar = new Car(start.X, start.Y);
                            newCar.setCars(cars);
                            cars.Add(newCar);
                            byte[] startingPos = new byte[2 * sizeof(int)];
                            Buffer.BlockCopy(BitConverter.GetBytes(start.X), 0, startingPos, 0, sizeof(int));
                            Buffer.BlockCopy(BitConverter.GetBytes(start.Y), 0, startingPos, sizeof(int), sizeof(int));
                            carClient.GetStream().Write(startingPos, 0, startingPos.Length);
                            Point dest = roadEndPoints[rand.Next(roadEndPoints.Count)];
                            byte[] destination = new byte[2 * sizeof(int)];
                            newCar.setPath(roadPaths[start][dest]);
                            Buffer.BlockCopy(BitConverter.GetBytes(dest.X), 0, destination, 0, sizeof(int));
                            Buffer.BlockCopy(BitConverter.GetBytes(dest.Y), 0, destination, sizeof(int), sizeof(int));
                            carClient.GetStream().Write(destination, 0, destination.Length);
                        }
                        else if (receivedMess == "PED")
                        {
                            byte[] port = BitConverter.GetBytes((int)((IPEndPoint)pedestrianServer.LocalEndpoint).Port);
                            stream.Write(port, 0, port.Length);
                            TcpClient pedClient = pedestrianServer.AcceptTcpClient();
                            var test = pedClient.GetStream();
                            pedThreadStop = true;
                            pedestrianListenerThread.Join();
                            pedThreadStop = false;
                            pedClients.Add(pedClient);
                            pedestrianListenerThread = new Thread(() => { ReceivePedData(); });
                            pedestrianListenerThread.Start();

                            byte[] xPosBuffer = new byte[sizeof(int)];
                            byte[] yPosBuffer = new byte[sizeof(int)];

                            stream.Read(xPosBuffer, 0, 3);

                            for (int i = 0; i < 100; i++)
                            {
                                stream.Read(xPosBuffer, 0, xPosBuffer.Length);
                                stream.Read(yPosBuffer, 0, yPosBuffer.Length);
                                int x = BitConverter.ToInt32(xPosBuffer);
                                pedestrians.pedestrians[i].position.X = BitConverter.ToInt32(xPosBuffer);
                                pedestrians.pedestrians[i].position.Y = BitConverter.ToInt32(yPosBuffer);
                            }

                            Console.WriteLine(pedClient.Client.RemoteEndPoint.ToString() + " connected to server");
                        }
                        else if (receivedMess == "TRA")
                        {
                            byte[] port = BitConverter.GetBytes((int)((IPEndPoint)tServer.LocalEndpoint).Port);
                            stream.Write(port, 0, port.Length);
                            TcpClient tramClient = tServer.AcceptTcpClient();
                            var test = tramClient.GetStream();
                            tramThreadStop = true;
                            tramListenerThread.Join();
                            tramThreadStop = false;
                            tramClients.Add(tramClient);
                            tramListenerThread = new Thread(() => { ReceiveTramData(); });
                            tramListenerThread.Start();
                            Console.WriteLine(tramClient.Client.RemoteEndPoint.ToString() + " connected to server");

                            int xpos, ypos, xspeed;
                            if (rail == 0)
                            {
                                xpos = 1470;
                                ypos = 406;
                                xspeed = -200;
                            }
                            else
                            {
                                xpos = 0;
                                ypos = 455;
                                xspeed = 200;
                            }
                            rail = (rail + 1) % 2;
                            Tram newCar = new Tram(xpos, ypos, xspeed, 0);
                            newCar.color = new Color(rand.Next(256), rand.Next(256), rand.Next(256), 255);
                            trams.Add(newCar);
                            byte[] startingPos = new byte[3 * sizeof(int)];
                            Buffer.BlockCopy(BitConverter.GetBytes(xpos), 0, startingPos, 0, sizeof(int));
                            Buffer.BlockCopy(BitConverter.GetBytes(ypos), 0, startingPos, sizeof(int), sizeof(int));
                            Buffer.BlockCopy(BitConverter.GetBytes(xspeed), 0, startingPos, 2 * sizeof(int), sizeof(int));
                            tramClient.GetStream().Write(startingPos, 0, startingPos.Length);
                        }
                    }
                    else if (receivedMess == "Dis")
                    {
                        // TO DO: old, probably not functional
                        carClients.RemoveAll(c => c.Client.RemoteEndPoint.ToString() == client.Client.RemoteEndPoint.ToString());
                        Console.WriteLine(client.Client.RemoteEndPoint.ToString() + " disconnected from the server");
                        client.Close();
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
                return;
            }
        }


      

        bool carThreadStop = false;
        bool pedThreadStop = false;
        bool tramThreadStop = false;
        protected void ReceiveCarData()
        {
            try
            {
                while (!carThreadStop)
                {
                    TcpClient carClient = null;
                    bool dataAvailable = false;
                    if (carClients.Count == 0)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    while (!dataAvailable && !carThreadStop)
                    {
                        for (int i = 0; i < carClients.Count; i++)
                        {
                            if (carClients[i].GetStream().DataAvailable)
                            {
                                carClient = carClients[i];
                                dataAvailable = true;
                                break;
                            }
                        }
                    }
                    if (carThreadStop) return;

                    NetworkStream stream = carClient.GetStream();

                    byte[] buffer = new byte[3];
                    stream.Read(buffer, 0, buffer.Length);
                    string header = Encoding.ASCII.GetString(buffer);
                    int index = carClients.FindIndex(x => (x.Client.RemoteEndPoint).Equals(carClient.Client.RemoteEndPoint));

                    switch (header)
                    {
                        case "POS":
                            {
                                byte[] xPosBuffer = new byte[sizeof(int)];
                                stream.Read(xPosBuffer, 0, xPosBuffer.Length);
                                int xPos = BitConverter.ToInt32(xPosBuffer, 0);

                                byte[] yPosBuffer = new byte[sizeof(int)];
                                stream.Read(yPosBuffer, 0, yPosBuffer.Length);
                                int yPos = BitConverter.ToInt32(yPosBuffer, 0);

                                if (cars[index].IsMoveAllowed(TrafficLightsZones, roadStructure, trams, pedestrians))
                                {
                                    cars[index].setPosition(xPos, yPos);
                                    byte[] response = Encoding.ASCII.GetBytes("YE");
                                    stream.Write(response, 0, response.Length);
                                }
                                else
                                {
                                    byte[] response = Encoding.ASCII.GetBytes("NO");
                                    stream.Write(response, 0, response.Length);
                                }
                                break;
                            }
                        case "DES":
                            {
                                byte[] newStartDest = new byte[4 * sizeof(int)];
                                Random random = new Random();
                                Point newStart = roadStartingPoints[random.Next(roadStartingPoints.Count)];
                                Point newDest = roadEndPoints[random.Next(roadEndPoints.Count)];
                                cars[index].setPosition(newStart);
                                Buffer.BlockCopy(BitConverter.GetBytes(newStart.X), 0, newStartDest, 0, sizeof(int));
                                Buffer.BlockCopy(BitConverter.GetBytes(newStart.Y), 0, newStartDest, sizeof(int), sizeof(int));
                                Buffer.BlockCopy(BitConverter.GetBytes(newDest.X), 0, newStartDest, 2 * sizeof(int), sizeof(int));
                                Buffer.BlockCopy(BitConverter.GetBytes(newDest.Y), 0, newStartDest, 3 * sizeof(int), sizeof(int));
                                stream.Write(newStartDest, 0, newStartDest.Length);
                                cars[index].setPath(roadPaths[newStart][newDest]);
                                break;
                            }
                        case "NEJ":
                            {
                                byte[] xPosBuffer = new byte[sizeof(int)];
                                stream.Read(xPosBuffer, 0, xPosBuffer.Length);
                                int xPos = BitConverter.ToInt32(xPosBuffer, 0);

                                byte[] yPosBuffer = new byte[sizeof(int)];
                                stream.Read(yPosBuffer, 0, yPosBuffer.Length);
                                int yPos = BitConverter.ToInt32(yPosBuffer, 0);

                                cars[index].setNextJunction(new Point(xPos, yPos));
                                break;
                            }
                    }
                }
            }
            catch (ThreadInterruptedException e){
                return;
            }
        }



        protected void ReceivePedData()
        {
            try
            {
                while (!pedThreadStop)
                {
                    TcpClient pedClient = null;
                    bool dataAvailable = false;
                    if (pedClients.Count == 0)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    while (!dataAvailable && !pedThreadStop)
                    {
                        for (int i = 0; i < pedClients.Count; i++)
                        {
                            if (pedClients[i].GetStream().DataAvailable)
                            {
                                pedClient = pedClients[i];
                                dataAvailable = true;
                                break;
                            }
                        }
                    }
                    if (pedThreadStop) return;

                    NetworkStream stream = pedClient.GetStream();

                    byte[] buffer = new byte[3];
                    stream.Read(buffer, 0, buffer.Length);
                    string header = Encoding.ASCII.GetString(buffer);
                    int index = pedClients.FindIndex(x => (x.Client.RemoteEndPoint).Equals(pedClient.Client.RemoteEndPoint));
                    switch (header)
                    {
                        case "POS":
                            {
                                byte[] xPosBuffer = new byte[sizeof(int)];
                                byte[] yPosBuffer = new byte[sizeof(int)];
                                Point[] positions = new Point[100];
                                string reply = "";
                                for (int i = 0; i < 100; i++)
                                {
                                    stream.Read(xPosBuffer, 0, sizeof(int));
                                    stream.Read(yPosBuffer, 0, sizeof(int));
                                    positions[i].X = BitConverter.ToInt32(xPosBuffer);
                                    positions[i].Y = BitConverter.ToInt32(yPosBuffer);
                                    if (pedestrians.isMoveAllowed(pedestrians.pedestrians[i], 5, trams))
                                    {
                                        pedestrians.pedestrians[i].position = positions[i];
                                        reply += "Y";
                                    }
                                    else reply += "N";
                                }

                                stream.Write(Encoding.ASCII.GetBytes(reply), 0, reply.Length);
                                break;
                            }
                        case "NEJ":
                            {
                                Point[] nextJuncs = new Point[100];
                                for (int i = 0; i < 100; i++)
                                {
                                    byte[] nextJunXBuffer = new byte[sizeof(int)];
                                    byte[] nextJunYBuffer = new byte[sizeof(int)];
                                    stream.Read(nextJunXBuffer, 0, sizeof(int));
                                    stream.Read(nextJunYBuffer, 0, sizeof(int));
                                    nextJuncs[i].X = BitConverter.ToInt32(nextJunXBuffer);
                                    nextJuncs[i].Y = BitConverter.ToInt32(nextJunYBuffer);
                                    pedestrians.pedestrians[i].nextJunction = nextJuncs[i];
                                }
                                break;
                            }
                    }
                }
            }catch (ThreadInterruptedException e)
            {
                return;
            }
        }

        protected void ReceiveTramData()
        {
            try
            {
                while (!tramThreadStop)
                {
                    TcpClient tramClient = null;
                    bool dataAvailable = false;
                    if (tramClients.Count == 0)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    while (!dataAvailable && !tramThreadStop)
                    {
                        for (int i = 0; i < tramClients.Count; i++)
                        {
                            if (tramClients[i].GetStream().DataAvailable)
                            {
                                tramClient = tramClients[i];
                                dataAvailable = true;
                                break;
                            }
                        }
                    }
                    if (tramThreadStop) return;

                    NetworkStream stream = tramClient.GetStream();

                    byte[] buffer = new byte[3];
                    stream.Read(buffer, 0, buffer.Length);
                    string header = Encoding.ASCII.GetString(buffer);
                    int index = tramClients.FindIndex(x => (x.Client.RemoteEndPoint).Equals(tramClient.Client.RemoteEndPoint));
                    switch (header)
                    {
                        case "POS":
                            {
                                byte[] xPosBuffer = new byte[sizeof(int)];
                                byte[] yPosBuffer = new byte[sizeof(int)];
                                stream.Read(xPosBuffer, 0, sizeof(int));
                                stream.Read(yPosBuffer, 0, sizeof(int));
                                int xPos = BitConverter.ToInt32(xPosBuffer);
                                int yPos = BitConverter.ToInt32(yPosBuffer);
                                trams[index].setPosition(new Point(xPos, yPos));
                                break;
                            }
                    }

                }
            }catch(ThreadInterruptedException e)
            {
                return;
            }
        }

        protected void SerializeSidewalks()
        {
            var serializer = new ConfigurationContainer().UseOptimizedNamespaces().Create();
            string xml = serializer.Serialize(new XmlWriterSettings { Indent = true }, sidewalkEndPoints);
            bytes = Encoding.ASCII.GetBytes(xml);
            using (StreamWriter writer = new StreamWriter("../../../../sidewalkEndPoints.xml"))
            {
                writer.WriteLine(xml);
            }
            string xml2 = serializer.Serialize(new XmlWriterSettings { Indent = true }, sidewalkStartingPoints);
            bytes = Encoding.ASCII.GetBytes(xml2);
            using (StreamWriter writer = new StreamWriter("../../../../sidewalkStartingPoints.xml"))
            {
                writer.WriteLine(xml2);
            }
        }
    }
}