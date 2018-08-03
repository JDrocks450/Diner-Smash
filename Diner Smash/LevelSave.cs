using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Diner_Smash
{
    [Serializable]
    public class LevelSave
    {
        [NonSerialized]
        public XDocument Source;
        public string WorkingDirectory;
        public Point LevelSize
        {
            get => new Point(SizeX, SizeY);
            set
            {
                SizeX = value.X;
                SizeY = value.Y;
            }
        }
        public int SizeY;
        public int SizeX;

        [NonSerialized]
        public List<GameObject> LoadedObjects = new List<GameObject>();

        public static LevelSave Load(string Path, ContentManager Content)
        {
            LevelSave l = new LevelSave();
            l.WorkingDirectory = Path;
            XDocument doc = XDocument.Load(Path);            
            return Load(doc, Content);
        }
        public static LevelSave Load(XDocument doc, ContentManager Content)
        {
            var l = new LevelSave();
            try
            {
                l.Source = doc;
            }
            catch (Exception ex)
            {
                return new LevelSave();
            }
            var e = doc.Root;
            l.LevelSize = new Point().Parse(e.Element("lSize").Value);
            var objs = new List<GameObject>();
            int id = 0;
            foreach (var n in e.Element("Objects").Elements())
            {
                objs.Add(new GameObject("").XmlDeserialize(n, Content));
                objs[id].ID = id;
                id++;
            }
            l.LoadedObjects = objs;
            return l;
        }

        public void Save(string Path = default)
        {
            if (Path == default)
                Path = WorkingDirectory;
            WorkingDirectory = Path;
            if (WorkingDirectory == null)
                throw new Exception("A path is mandatory");
            var d = new XDocument();
            XElement e;
            d.Add(e = new XElement("LevelFileRoot"));
            e.Add(new XElement("lSize", LevelSize)); //Level Size
            XElement o;
            e.Add(o = new XElement("Objects"));
            foreach (var obj in Main.Objects)
            {
                var skip = false;
                switch (obj.Identity)
                {
                    case ObjectContext.ObjectNameTable.Person:
                        skip = true;
                        break;
                }
                if (skip)
                    continue;
                XElement n;
                obj.XmlSerialize(n = new XElement($"obj{obj.ID}"));
                o.Add(n);
            }
            e.Save(Path.ToString());
        }

        public byte[] Serialize()
        {
            var t = Source.ToString();            
            return Encoding.ASCII.GetBytes(t);
        }

        public static LevelSave DeserializeFromServer(byte[] data)
        {
            var t = ASCIIEncoding.ASCII.GetString(data);
            var save = new LevelSave();
            save = Load(XDocument.Parse(t), Main.Manager);
            return save;
        }
    }
}
