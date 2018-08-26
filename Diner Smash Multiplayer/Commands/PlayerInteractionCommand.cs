using System;
using static Server_Structure.Server;

namespace Server_Structure.Commands
{
    public class PlayerInteractionCommand
    {
        /// <summary>
        /// Client sent the server that it was interacting with an object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        public static void PlayerInteracted(ClientContext context, byte[] data)
        {
            var b = DSPacket.Format(Command.PLAYER_INTERACT, data);
            BroadcastPacket(b);
            var x = BitConverter.ToInt32(data, 0);
            var y = BitConverter.ToInt32(data, 4);
            WriteLine($"Player: {x}, Interacts with Object: {y}");
        }
    }
}
