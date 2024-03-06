using System;
using System.Web;
using Microsoft.AspNetCore.SignalR;

namespace Project_Lastimosa
{
    public class WebsocketServer : Hub
    {
        public async Task RecievedMessage(string user, string message)
        {
            await Clients.All.SendAsync(message);
        }
    }
}
