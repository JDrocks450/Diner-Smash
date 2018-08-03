using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server_Structure
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine("Strike S for Server or C for Client");
                if (Console.ReadKey().Key == ConsoleKey.C)
                    new Client().StartGameClient(Client.GetlocalIP());
                else
                    Server.StartServer(IPAddress.Any, true);
            }
            else if (args.Contains("server"))
                Server.StartServer(Client.GetlocalIP(), true);
        }
    }
}
