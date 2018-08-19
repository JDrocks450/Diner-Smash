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
        public static ObjectContext.ObjectNameTable[] RequiredObjects = new ObjectContext.ObjectNameTable[]
        {
            ObjectContext.ObjectNameTable.FoodCounter,
            ObjectContext.ObjectNameTable.POS,
            ObjectContext.ObjectNameTable.Table,
            ObjectContext.ObjectNameTable.WelcomeMat,            
        };

        [NonSerialized]
        public XDocument Source;
        public string WorkingDirectory;
        public const string SAVENAME = @"\Content\level.xml";
        public Point LevelSize
        {
            get => new Point(SizeX, SizeY);
            set
            {
                SizeX = value.X;
                SizeY = value.Y;
            }
        }
        public int SizeY = 1000;
        public int SizeX = 1000;

        [NonSerialized]
        public List<GameObject> LoadedObjects = new List<GameObject>();

        public static LevelSave Load(ContentManager Content)
        {
            LevelSave l = new LevelSave();
            l.WorkingDirectory = Environment.CurrentDirectory + SAVENAME;
            XDocument doc = null;
            try
            {
                doc = XDocument.Load(l.WorkingDirectory);
            }
            catch { return new LevelSave(); }
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
            try
            {
                l.LevelSize = new Point().Parse(e.Element("lSize").Value);
                Main.GameScene.FloorMask = new Color(uint.Parse(e.Element("fColor").Value));
                if (e.Element("Lighting") != null)
                    Lighting.LoadLightingSettings(e.Element("Lighting"));
                var objs = new List<GameObject>();
                int id = 0;
                foreach (var n in e.Element("Objects").Elements())
                {
                    objs.Add(new GameObject("").XmlDeserialize(n, Content));
                    objs[id].ID = id;
                    id++;
                }
                Main.SourceLevel = l;
                l.LoadedObjects = objs;
            }
            catch (Exception en)
            {
                System.Windows.Forms.MessageBox.Show($"There was an error loading the save file. {Environment.NewLine + en}");
            }            
            return l;
        }

        /// <summary>
        /// Checks if every required object is included in the List of objects.
        /// </summary>
        /// <param name="ObjectsToCheck"></param>
        /// <returns></returns>
        public static bool VerifyRequiredObjectsMet(List<GameObject> ObjectsToCheck, out int MetObjects)
        {
            var objsMet = 0;
            foreach (var n in RequiredObjects)
                if(ObjectsToCheck.Select(x => x.Identity).Contains(n)) objsMet++;
            MetObjects = objsMet;
            return objsMet == RequiredObjects.Length;
        }

        public void Save(string Path = default)
        {
            if (!VerifyRequiredObjectsMet(Main.Objects, out int objsSat))
                if (System.Windows.Forms.MessageBox.Show
                    ($"Your Dining Room only has {objsSat} out of {RequiredObjects.Length} required objects: " +
                    $"{Environment.NewLine + " + "}" +
                    $"{string.Join(Environment.NewLine + " + ", RequiredObjects.Select(x => Enum.GetName(typeof(ObjectContext.ObjectNameTable), x)))}",
                    "Missing Required Objects", System.Windows.Forms.MessageBoxButtons.OKCancel)
                    == System.Windows.Forms.DialogResult.Cancel)
                    return;
            if (Path == default)
                Path = Environment.CurrentDirectory + SAVENAME;
            WorkingDirectory = Path;
            if (WorkingDirectory == null)
                throw new Exception("A path is mandatory");
            var d = new XDocument();
            XElement e;
            d.Add(e = new XElement("LevelFileRoot"));
            e.Add(new XElement("lSize", LevelSize)); //Level Size
            XElement o;
            e.Add(new XElement("fColor", Main.GameScene.FloorMask.PackedValue));
            var lightE = new XElement("Lighting");
            Lighting.SaveLightingSettings(ref lightE);
            e.Add(lightE);
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
