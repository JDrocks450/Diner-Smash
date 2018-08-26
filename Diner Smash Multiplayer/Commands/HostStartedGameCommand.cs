using static Server_Structure.Server;

namespace Server_Structure.Commands
{
    /// <summary>
    /// This command tells joined clients to start updating objects.
    /// </summary>
    public class HostStartedGameCommand
    {
        public static void BroadcastGameStart()
        {
            var packet = DSPacket.Format(Command.HOST_STARTGAME, new byte[0]);
            BroadcastPacket(packet);
        }
    }
}
