using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Diner_Smash.UserInterface;

namespace Diner_Smash
{
    public class PathHelper
    {
        public class NavNode
        {
            public NavNode()
            {

            }

            public NavNode(Point Start, Point Here, Point Destination)
            {
                Location = Here;
                Begin = Start;
                RouteDestination = Destination;
            }

            public Point Begin;
            public Point RouteDestination;
            public Point Location
            {
                get => new Point(X, Y);
                set
                {
                    X = value.X;
                    Y = value.Y;
                }
            }
            public int X;
            public int Y;
            public int F
            {
                get => G + H;
            }
            public int G
            {
                get => GetGRating(Begin);
            }
            public int H
            {
                get => GetHRating(RouteDestination);
            }
            public List<NavNode> PossibleNodes = new List<NavNode>();

            StackPanel stack = new StackPanel(default, false);
            public void Update()
            {
                var mouserect = new Rectangle(Main.MousePosition.ToPoint(), new Point(1, 1));
                var collision = new Rectangle(Location, new Point(30));
                stack.CreateImage(Main.BaseTexture, Color.Black, default);
                stack.AddRange(InterfaceComponent.HorizontalLock.Left, new InterfaceComponent().CreateText($"F: {F}", Color.White, new Point(10)),
                    new InterfaceComponent().CreateText($"G: {G}", Color.White, new Point(10)),
                    new InterfaceComponent().CreateText($"H: {H}", Color.White, new Point(10)));                
                if (collision.Intersects(mouserect))
                    Main.UILayer.Components.Add(stack);
            }

            public int GetHRating(Point Destination)
            {
                return Math.Abs(Destination.X - Location.X) + Math.Abs(Destination.Y - Location.Y);
            }
            public int GetGRating(Point Start)
            {
                return Math.Abs(Start.X - Location.X) + Math.Abs(Start.Y - Location.Y);
            }
        }

        public int Width
        {
            get; set;
        }

        public int Height
        {
            get; set;
        }

        public Point GetSize
        {
            get => new Point(Width, Height);
        }

        public List<GameObject> Objects;

        public PathHelper(ref List<GameObject> Source, int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;
            Objects = Source;
        }

        public List<NavNode> GetMap()
        {
            nodes.Clear();
            var map = new List<Point>();
            foreach(var obj in Main.Objects.Where(x => x.IsRoutable))
            {
                var rect = obj.BoundingRectangle;
                map.Add(rect.Location - GetSize);
                map.Add(new Point(rect.Right, rect.Top) + new Point(Width, -Height));
                map.Add(new Point(rect.Right, rect.Bottom) + new Point(Width, Height));
                map.Add(new Point(rect.Left, rect.Bottom) + new Point(-Width, Height));
            }
            var PathMap = new List<NavNode>();
            foreach (var p in map)
                PathMap.Add(new NavNode() { Location = p });
            foreach(var n in PathMap)            
                n.PossibleNodes = PathMap.Where(x => x != n && !LOSCheck(n, x)).ToList();
            CleanMap(ref PathMap);
            nodes = PathMap.ToList();
            return PathMap;
        }

        /// <summary>
        /// Cleans out nodes that are unreachable
        /// </summary>
        /// <param name="Map"></param>
        public void CleanMap(ref List<NavNode> Map)
        {
            Map.RemoveAll(node =>
                node.X < 0 ||
                node.X > Main.SourceLevel.LevelSize.X ||
                node.Y < 0 ||
                node.Y > Main.SourceLevel.LevelSize.Y);
        }

        public bool LOSCheck(NavNode Next, NavNode Current)
        {
            retry:
            try
            {
                foreach (var obj in Main.Objects.Where(x => x.IsRoutable))
                {
                    var v = obj.BoundingRectangle.Intersects(Current.Location, Next.Location);
                    if (v)
                        return true;
                }
            }
            catch { goto retry; }
            return false;
        }

        Color RouteLineColor = Color.Red;
        List<NavNode> nodes = new List<NavNode>();

        public void Update()
        {
            if (false)
            foreach (var n in nodes)
                n.Update();
        }

        public bool Successful = false;
        public List<NavNode> Route(Point Start, Point End)
        {
            var start = new NavNode(Start, Start, End);
            var target = new NavNode(Start, End, End);
            var totalList = new List<NavNode>();
            var closedList = new List<NavNode>();

            totalList.Add(start);
            totalList.Add(target);
            var Map = GetMap();
            foreach (var walkableNode in Map)
            {
                //Not in open list
                if (totalList.FirstOrDefault(l => l.Location == walkableNode.Location) == null)                                   
                    totalList.Add(walkableNode);                
            }            
            
            foreach (var walkableNode in totalList)
            {
                //setup NavNode    
                walkableNode.Begin = Start;
                walkableNode.RouteDestination = End;
                walkableNode.PossibleNodes = totalList.Where(x => x != walkableNode && !LOSCheck(walkableNode, x)).ToList();
            }
            closedList.Add(start);
            int i = 0;
            while(totalList.Count > i)
            {                
                var n = totalList[i];                
                if (n.Location == End)
                {
                    RouteLineColor = Color.Green;
                    closedList.Add(n);
                    Successful = true;
                    break;
                }
                if (!n.PossibleNodes.Any())
                {
                    RouteLineColor = Color.Red;
                    Successful = false;
                    break;
                }
                NavNode node = default;
                try
                {
                    again:
                    var result = n.PossibleNodes.Where(x => !closedList.Contains(x)); //nodes not taken
                    var num = result.Min(x => x.F); //find seemingly lowest distance node
                    var multiresult = n.PossibleNodes.FindAll(x => x.F == num); //pick out any same distance nodes
                    num = multiresult.Min(x => x.H);
                    node = multiresult.Find(x => x.H == num); //find the closest node to destination
                    if (!totalList.Contains(node))
                    {
                        n.PossibleNodes.Remove(node);
                        goto again;
                    }
                }
                catch
                {
                    RouteLineColor = Color.Red;
                    Successful = false;
                    break;
                }
                totalList.RemoveAt(i);
                i = totalList.IndexOf(node);
                if (!closedList.Contains(n))
                    closedList.Add(n);
            }
            nodes = totalList;
            return closedList;
        }

        public void DEBUG_DrawMap(SpriteBatch batch)
        {
            foreach (var Node in GetMap())
            {
                foreach (var p in Node.PossibleNodes)
                    batch.DrawLine(Node.Location, p.Location, Color.White);
                batch.Draw(Main.BaseTexture, new Rectangle(Node.Location, new Point(25)), Color.Blue);
            }
        }

        public void DEBUG_DrawRoute(SpriteBatch batch, Point Start, Point End)
        {
            var path = Route(Start, End);
            DEBUG_DrawRoute(batch, path);
        }

        public void DEBUG_DrawRoute(SpriteBatch batch, List<NavNode> path)
        {
            if (path == null || path.Count == 0)
                return;
            var prev = path.First();
            foreach (var point in path)
            {
                if (prev != point)
                    batch.DrawLine(prev.Location, point.Location, RouteLineColor, 3);
                foreach (var p in point.PossibleNodes)
                    batch.DrawLine(point.Location, p.Location, Color.White);
                prev = point;
                var color = Color.DeepSkyBlue;
                if (point.Location == point.Begin)
                    color = Color.Green;
                if (point.Location == point.RouteDestination)
                    color = Color.LightGreen;
                batch.Draw(Main.BaseTexture, new Rectangle(point.Location, new Point(30)), color);
            }
        }
    }
}
