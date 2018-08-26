using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Server_Structure.Server;

namespace Server_Structure.Commands
{
    /// <summary>
    /// Contains commands that handle the creation of other players.
    /// </summary>
    public class CreatePlayerCommand
    {
        /// <summary>
        /// Tells the client to create a player for the context ID.
        /// </summary>
        /// <param name="SendTo"></param>
        /// <param name="PlayerID"></param>
        public static void CreatePlayer(ClientContext SendTo, int PlayerID)
        {
            var b = BitConverter.GetBytes(PlayerID);
            var send = DSPacket.Format(Command.CLIENT_CREATEPLAYER, b);
            SendPacketToClient(SendTo, send);
            WriteLine($"Player created on context: {SendTo.ID}, for ID: {PlayerID}");
        }

        /// <summary>
        /// Tells a client to create a player for every connection in the session.
        /// </summary>
        /// <param name="SendTo"></param>
        public static void BroadcastCreateAllPlayers(ClientContext SendTo)
        {
            foreach (var id in Connections.Select(x => x.ID))
                CreatePlayer(SendTo, id);
        }
    }
}
