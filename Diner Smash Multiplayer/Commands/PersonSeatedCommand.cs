using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Server_Structure.Server;

namespace Server_Structure.Commands
{
    public class PersonSeatedCommand
    {
        /// <summary>
        /// The client tells the server that it has seated a person at a table.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        public static void PersonSeated(ClientContext context, byte[] data)
        {
            var b = DSPacket.Format(Command.PERSON_SEAT, data);
            BroadcastPacket(b, BroadcastAudience.All); //Tell all clients to seat a person at table ID in seat ID.
            var SenderID = BitConverter.ToInt32(data, 0);
            var pID = BitConverter.ToInt32(data, 4);
            var table = BitConverter.ToInt32(data, 8);
            WriteLine($"Player: {SenderID}, Seats Person: {pID} at Table: {table}");
        }
    }
}
