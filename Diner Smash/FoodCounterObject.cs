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

        public delegate void OrderUpHandler(Food FoodOrder);
        public event OrderUpHandler OnOrderReady;

        public Queue<Menu> Orders = new Queue<Menu>();
        public Menu CurrentOrder;
        public Queue<Food> ReadyOrders = new Queue<Food>(5);

        float prepTime { get => Properties.Gameplay.Default.Kitchen_FoodPrepareTime; }

        public FoodCounterObject(string Name) : base(Name, ObjectNameTable.FoodCounter)
        {
            OnOrderReady += FoodCounterObject_OnOrderReady;
        }

        private void FoodCounterObject_OnOrderReady(Food FoodOrder)
        {
            ReadyOrders.Enqueue(FoodOrder);
        }

        public override void Load(ContentManager Content = null)
        {
            Texture = Content.Load<Texture2D>("Objects/Kitchen_FCounter");
            base.Load(Content);
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

        /// <summary>
        /// Called from POSObject -- Submits an order to the kitchen.
        /// </summary>
        /// <param name="menu"></param>
        public void SubmitOrder(Menu menu)
        {
            if (Orders.Count < 5)
                Orders.Enqueue(menu);
            else
                System.Windows.Forms.MessageBox.Show("The Kitchen is backed up! Deliver some ready " +
                    "orders first!", "Can't do that...");
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            foreach (var f in ReadyOrders)
                f.Draw(spriteBatch);
        }
    }
}
