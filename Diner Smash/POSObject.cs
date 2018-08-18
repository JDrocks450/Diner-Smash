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
        public override Point InteractionPoint
        {
            get
            {
                return new Point(BoundingRectangle.Center.X, BoundingRectangle.Bottom + Main.Player?.PathFinder.Height ?? -1);
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
            var menu = (Menu)Focus.Hands.Where(x => x is Menu).First();
            Focus.RemoveObjectFromHand(menu);
            SubmitOrder(Focus, menu);
            Interacting = false;
            return true;
        }

        public void SubmitOrder(Player Focus, Menu Order)
        {
            //Before Kitchen is implemented, just give the player the food.
            Focus.PlaceObjectInHand(new Food(Order.TableID));
        }
    }
}
