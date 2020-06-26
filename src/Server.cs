using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Server
{
    public class Server : BaseScript
    {
        public Server()
        {
            EventHandlers["Server:SyncInventory"] += new Action<int, string, bool>((key, value, delete) =>
            {
                TriggerClientEvent("Client:SyncInventory", key, value, delete);
            });
        }
    }
}
