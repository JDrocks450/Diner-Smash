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
    public class FoodCounterObject : GameObject
    {
        public static FoodCounterObject LevelDefault
        {
            get => (FoodCounterObject)Main.Objects.Where(x => x is FoodCounterObject).First() ?? null;
        }
        public const int MAX_ORDERS = 5;

        public override Point InteractionPoint
        {
            get
            {
                return new Point(BoundingRectangle.Center.X, BoundingRectangle.Bottom + Player.ControlledCharacter?.PathFinder.Height ?? -1);
            }
        }

        public delegate void OrderUpHandler(Food FoodOrder);
        public event OrderUpHandler OnOrderReady;

        public Queue<Menu> Orders = new Queue<Menu>();
        public Menu CurrentOrder;

        float prepTime { get => Properties.Gameplay.Default.Kitchen_FoodPrepareTime; }

        public FoodCounterObject(string Name) : base(Name, ObjectNameTable.FoodCounter)
        {
            OnOrderReady += FoodCounterObject_OnOrderReady;
        }

        private void FoodCounterObject_OnOrderReady(Food FoodOrder)
        {
            FoodOrder.Load(Main.Manager);
            PlaceObjectInSlot(FoodOrder);
        }

        public override void Load(ContentManager Content = null)
        {
            PlacementSlots.Clear();
            Texture = Content.Load<Texture2D>("Objects/Kitchen_FCounter");
            base.Load(Content);
            var placementSlotInterval = (Size.X - 100) / MAX_ORDERS;
            for (int i = 0; i < MAX_ORDERS; i++)
                PlacementSlots.Add(new Point(placementSlotInterval * (i+1), (Size.Y / 4)+25), null);
        }

        TimeSpan timeSinceOrderStarted;
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (CurrentOrder == null && Orders.Count > 0)
                CurrentOrder = Orders.Dequeue();
            if (CurrentOrder != null)
                timeSinceOrderStarted += gameTime.ElapsedGameTime;
            if (prepTime <= timeSinceOrderStarted.TotalSeconds)
            {
                OnOrderReady?.Invoke(new Food(CurrentOrder.TableID));
                CurrentOrder = null;
                timeSinceOrderStarted = TimeSpan.Zero;
            }
        }

        public override bool Interact(Player Focus, bool Force = false)
        {
            if (!base.Interact(Focus, Force))
                return false;
            var r = PlacementSlots[PlacementSlots.Where(x => x.Value != null).First().Key];
            if (r != null && Focus.PlaceObjectInHand(r))
                RemoveObjectFromSlot(r);
            else return false;
            return true;
        }

        /// <summary>
        /// Called from POSObject -- Submits an order to the kitchen.
        /// </summary>
        /// <param name="menu"></param>
        public void SubmitOrder(Menu menu)
        {
            if (Orders.Count < MAX_ORDERS)
                Orders.Enqueue(menu);
            else
                System.Windows.Forms.MessageBox.Show("The Kitchen is backed up! Deliver some ready " +
                    "orders first!", "Can't do that...");
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }
    }
}
