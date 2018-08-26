using System;
using static Server_Structure.Server;

namespace Server_Structure.Commands
{
    public class PlayerNavigationCommand
    {
        /// <summary>
        /// Client tells the server that the player has navigated to a specific point.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        public static void PlayerNavigated(ClientContext context, byte[] data)
        {
            var b = DSPacket.Format(Command.PLAYER_NAVIGATE, data);
            BroadcastPacket(b);
            var x = BitConverter.ToInt32(data, 4);
            var y = BitConverter.ToInt32(data, 8);
            WriteLine($"Client: {context.ID} Navigates to X:{x}, Y:{y}");
        }
    }
}
