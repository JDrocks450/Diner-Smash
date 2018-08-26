using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Server_Structure.Server;


namespace Server_Structure.Commands
{
    public class LevelChangedCommand
    {
        /// <summary>
        /// The server updates the level using the new data and forces each client to update the level.
        /// </summary>
        /// <param name="context"></param>
        public static bool UpdateLevel(ClientContext context, byte[] buffer)
        {
            if (!context.Operator)
            {
                WriteLine("Client: " + context.ID + " is not an operator and therefore cannot change the server level.");
                return false;
            }
            SERVER_LevelFile = buffer;            
            return true;
        }

        /// <summary>
        /// Sends the current level to client, forcing it to update it's level.
        /// </summary>
        /// <param name="context"></param>
        public static void SendLevel(ClientContext context)
        {
            if (SERVER_LevelFile == null)
            {
                WriteLine("A LevelSave is not present and therefore the SendLevel command was aborted.");
                return;
            }
            WriteLine($"Sending level data to context: {context.ID}");
            SendPacketToClient(context, DSPacket.Format(Command.LEVEL_LEVELCHANGED, SERVER_LevelFile));
        }

        /// <summary>
        /// Tells every client to change the level to the currently stored level in the Server.
        /// </summary>
        public static void BroadcastSendLevel()
        {
            BroadcastPacket(DSPacket.Format(Command.LEVEL_LEVELCHANGED, SERVER_LevelFile));
        }
    }
}
