using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Diner_Smash
{
    public class Person : GameObject
    {
        public const int MAX_FRAMES = 1;
        public readonly float MENUS_Time;
        public readonly float FOOD_EAT_Time;

        public enum PersonNameTable
        {
            GaryPizza,
        }
        public PersonNameTable PersonType;

        public int Frame
        {
            get => _frame;
            set
            {
                _frameChanged = true;
                if (value < MAX_FRAMES)
                    _frame = value;
            }                
        }
        public WelcomeMat ParentSpawner;

        Texture2D[] Frames;
        private bool _frameChanged;
        private int _frame;        

        public int Happiness
        {
            get;
            set;
        } = 10;

        public Person(string Name, PersonNameTable personName) : base(Name, ObjectNameTable.Person)
        {
            PersonType = personName;
            Draggable = true;
            IsRoutable = false;
            IsInteractable = false;
            switch (PersonType)
            {
                case PersonNameTable.GaryPizza:
                    MENUS_Time = 5;
                    FOOD_EAT_Time = 5;
                    break;
            }
        }

        public override void Load(ContentManager Content)
        {
            Frames = new Texture2D[MAX_FRAMES];
            string p = $"Entities/{Enum.GetName(typeof(PersonNameTable), PersonType)}";
            for (int i = 0; i < MAX_FRAMES; i++)
                Frames[i] = Content.Load<Texture2D>(p + "/frame" + i);
            Frame = 0;
        }

        public override void Update(GameTime gameTime)
        {
            if (_frameChanged)
            {
                Texture = Frames[Frame];
                var FrameSize = new Point(Texture.Width, Texture.Height);
                Width = (int)(FrameSize.X * Scale);
                Height = (int)(FrameSize.Y * Scale);
                _frameChanged = false;
            }
            base.Update(gameTime);
        }
    }

    public class PeopleGrouper
    {       
        public const int MAX_GROUPSIZE = 4;

        public PeopleGrouper()
        {

        }

        /// <summary>
        /// Gets an array of people in the group that is readonly.
        /// </summary>
        public Person[] People
        {
            get => _people.ToArray();
        }
        List<Person> _people;

        public void Clear()
        {
            _people.Clear();
        }

        /// <summary>
        /// Adds the specified people to the group.
        /// </summary>
        /// <param name="AddPeople"></param>
        public void Add(params Person[] AddPeople)
        {
            if (_people.Count + AddPeople.Length <= 4)
                _people.AddRange(AddPeople);
        }


    }
}
