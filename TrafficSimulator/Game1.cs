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

    }
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteBatch _roadsBatch;
        private List<Rectangle> roadList = new List<Rectangle>();
        private List<Line> lineList = new List<Line>();
        private Texture2D rect; //Texture used to draw rectangles
        private Dictionary<Point, List<Point>> roadStructure = new Dictionary<Point, List<Point>>();
        private const string svgPath = "..\\..\\..\\final.svg";
        private const double scale = 7;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 1600 * 9 / 16;
            //_graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();
            ReadSVG();
            createRoadStructure();
            printRoadStructure();
            setupCars();
            foreach (Car car in cars)
            {
                Task.Factory.StartNew(() => car.Move(roadStructure));
                //Thread thread = new Thread(() => { car.Move(roadStructure); });
                //thread.Start(); 
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _roadsBatch = new SpriteBatch(GraphicsDevice);
            rect = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            rect.SetData(new[] { Color.White });
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            // TODO: Add your drawing code here
            DrawRoads();
            _spriteBatch.Begin();
            foreach (Car car in cars)
            {
                _spriteBatch.Draw(rect, new Rectangle(car.position.X - 10, car.position.Y - 10, car.Size.X, car.Size.Y), car.color);
            }
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private Car[] cars = new Car[5];

        private void setupCars()
        {
            float speed = 1000;
            cars[0] = new Car(811, 832, 0, -speed);
            cars[0].setDestination(roadStructure[cars[0].position].First());
            cars[1] = new Car(735, 832, 0, -speed);
            cars[1].setDestination(roadStructure[cars[1].position].First());
            cars[1].color = Color.Coral;
            cars[2] = new Car(340, 0, 0, speed);
            cars[2].setDestination(roadStructure[cars[2].position].First());
            cars[2].color = Color.Red;
            cars[3] = new Car(302, 0, 0, speed);
            cars[3].setDestination(roadStructure[cars[3].position].First());
            cars[3].color = Color.ForestGreen;
            cars[4] = new Car(1289, 0, 0, speed);
            cars[4].setDestination(roadStructure[cars[4].position].First());
            cars[4].color = Color.BlueViolet;
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

        private void createRoadStructure()
        {
            foreach (Line line in lineList)
            {
                if (!roadStructure.ContainsKey(line.start))
                {
                    roadStructure.Add(line.start, new List<Point>());
                }
                roadStructure[line.start].Add(line.end);
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


        private void ReadSVG()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(svgPath);

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("svg", "http://www.w3.org/2000/svg");

            XmlNodeList pathNodeList = doc.SelectNodes("//svg:path", nsMgr);
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
                    //double brushWidth = 5;
                    double brushWidth = Convert.ToDouble(brush.Split(":")[1]);
                    string[] startPoint = instructions[1].Split(',');
                    string[] endPoint = instructions[instructions.Length - 1].Split(',');
                    Console.WriteLine(instructions[0]);
                    Console.WriteLine(startPoint[0]);
                    Console.WriteLine(endPoint[0]);
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
                    addRoad(startX, startY, endX, endY, brushWidth);
                }
                else
                {
                    Console.WriteLine("Path was not found in SVG");
                }
            }
        }

        private void addRoad(double startX, double startY, double endX, double endY, double brushWidth)
        {
            startX *= scale;
            startY *= scale;
            endX *= scale;
            endY *= scale;
            startX = Math.Round(startX, 0);
            startY = Math.Round(startY, 0);
            endX = Math.Round(endX, 0);
            endY = Math.Round(endY, 0);
            brushWidth *= 7;
            Rectangle coords;
            if (startX > endX || startY > endY)
            {
                if (startX > endX)
                    coords = new Rectangle((int)(endX - brushWidth / 2), (int)(endY - brushWidth / 2), (int)(startX - endX + brushWidth), (int)(startY - endY + brushWidth));
                else
                    coords = new Rectangle((int)(endX - brushWidth / 2), (int)(endY - brushWidth / 2), (int)(startX - endX + brushWidth), (int)(startY - endY + brushWidth));
            }
            else
            {
                if (endX > startX)
                    coords = new Rectangle((int)(startX - brushWidth / 2), (int)(startY - brushWidth / 2), (int)(endX - startX + brushWidth), (int)(endY - startY + brushWidth));
                else
                    coords = new Rectangle((int)(startX - brushWidth / 2), (int)(startY - brushWidth / 2), (int)(endX - startX + brushWidth), (int)(endY - startY + brushWidth));
            }
            roadList.Add(coords);
            lineList.Add(new Line((int)startX, (int)startY, (int)endX, (int)endY));
        }
    }
}