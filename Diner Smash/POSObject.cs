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
    public class POSObject : GameObject
    {
        public static POSObject LevelDefault
        {
            get => (POSObject)Main.Objects.Where(x => x is POSObject)?.First() ?? null;
        }

        public override Point InteractionPoint
        {
            get
            {
                return new Point(BoundingRectangle.Center.X, BoundingRectangle.Bottom + Player.ControlledCharacter?.PathFinder.Height ?? -1);
            }
        }

        public POSObject(string Name) : base(Name, ObjectNameTable.POS)
        {

        }

        public override void Load(ContentManager Content = null)
        {
            Texture = Content.Load<Texture2D>("Objects/APodium");
            base.Load(Content);
        }

        public override bool Interact(Player Focus, bool Force = false)
        {            
            if (!base.Interact(Focus, Force))
                return false;
            if (!Focus.Hands.Where(x => x is Menu).Any())
            {
                Interacting = false;
                return false;
            }
            var query = Focus.Hands.Where(x => x.Value is Menu);
            if (!query.Any())
                return false;
            var menu = (Menu)query.First().Value;
            Focus.RemoveObjectFromHand(menu);
            SubmitOrder(menu);
            Interacting = false;
            return true;
        }

        public bool SubmitOrder(Menu Order)
        {
            if (FoodCounterObject.LevelDefault is null)
                return false;
            FoodCounterObject.LevelDefault.SubmitOrder(Order);
            return true;
        }
    }
}
