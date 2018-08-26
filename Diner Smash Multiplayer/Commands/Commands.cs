namespace Server_Structure.Commands
{
    public enum Command : byte
    {
        SERVER_SHUTDOWN = 0,
        /// <summary>
        /// Kicks a client
        /// </summary>
        CLIENT_KICK = 1,
        LEVEL_LEVELCHANGED = 2,
        /// <summary>
        /// Client ASCII messaged all
        /// </summary>
        CLIENT_MESSAGE_ALL = 3,
        /// <summary>
        /// Client should create a player for client with ID...
        /// </summary>
        CLIENT_CREATEPLAYER = 4,
        /// <summary>
        /// Host sending new level
        /// </summary>
        LEVEL_NEWLEVEL = 5,
        /// <summary>
        /// Player navigated
        /// </summary>
        PLAYER_NAVIGATE = 6,
        /// <summary>
        /// Player interacted
        /// </summary>
        PLAYER_INTERACT = 7,
        /// <summary>
        /// Person was seated by player
        /// </summary>
        PERSON_SEAT = 8,
        /// <summary>
        /// Host has started the game.
        /// </summary>
        HOST_STARTGAME = 9,
        CLIENT_IDCHANGE = 10,
    }
}
