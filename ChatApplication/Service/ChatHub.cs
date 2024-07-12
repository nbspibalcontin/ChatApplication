using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication.Service
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        //Connects a user to the chat and notifies all connected clients.
        public async Task ConnectUser(string userName)
        {
            try
            {
                // Notify all clients that a user has joined the chat
                await Clients.All.SendAsync("JoinMessage",userName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while connecting user.");
            }
        }
    }
}
