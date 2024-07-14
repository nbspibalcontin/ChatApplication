using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Data;
using Server.Entity;

namespace Server.Service
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private static readonly HashSet<string> ConnectedUsers = new HashSet<string>();
        private static readonly Dictionary<string, string> UserConnections = new Dictionary<string, string>();

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        //Connects a user to the chat and notifies all connected clients.
        public async Task ConnectUser(string userName)
        {
            try
            {
                // Add the username to the list of connected users
                ConnectedUsers.Add(userName);
                Console.WriteLine($"User '{userName}' added to ConnectedUsers.");

                // Map the user's connection ID to their username
                UserConnections[Context.ConnectionId] = userName;
                Console.WriteLine($"Mapped connection ID '{Context.ConnectionId}' to user '{userName}'.");

                // Notify all clients that a user has joined the chat
                await Clients.All.SendAsync("UserJoined", userName, ConnectedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while connecting user.");
                await SendSystemErrorMessage("An error occurred while joining the client. Please try again.");
            }
        }

        //Disconnect user
        public async Task DisconnectUser(string userName)
        {
            try
            {
                if (ConnectedUsers.Contains(userName))
                {
                    ConnectedUsers.Remove(userName);
                    UserConnections.Remove(Context.ConnectionId);

                    // Notify all clients that a user has left the chat
                    await Clients.All.SendAsync("UserDisconnected", userName, ConnectedUsers);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error disconnecting user {userName}: {ex.Message}");  
                await SendSystemErrorMessage("An error occurred while disconnecting the client. Please try again.");
            }
        }

        // Reconnect user
        public async Task ReconnectUser(string userName)
        {
            try
            {
                // Add the username to the list of connected users
                ConnectedUsers.Add(userName);
                Console.WriteLine($"User '{userName}' added to ConnectedUsers.");

                // Map the user's connection ID to their username
                UserConnections[Context.ConnectionId] = userName;
                Console.WriteLine($"Mapped connection ID '{Context.ConnectionId}' to user '{userName}'.");

                // Notify all clients that a user has joined the chat
                await Clients.All.SendAsync("ReconnectUser", userName, ConnectedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reconnecting user.");
                await SendSystemErrorMessage("An error occurred while reconnecting the client. Please try again.");
            }
        }

        //Reconnect user to the room with new userName
        public async Task ReconnectUserWithNewUsername(string oldUserName,string newUserName)
        {
            try
            {
                // Add the username to the list of connected users
                ConnectedUsers.Add(newUserName);
                Console.WriteLine($"User '{newUserName}' added to ConnectedUsers.");

                // Map the user's connection ID to their username
                UserConnections[Context.ConnectionId] = newUserName;
                Console.WriteLine($"Mapped connection ID '{Context.ConnectionId}' to user '{newUserName}'.");

                // Notify all clients that a user has joined the chat
                await Clients.All.SendAsync("ReconnectUserWithNewUsername", oldUserName, newUserName, ConnectedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reconnecting user.");
                await SendSystemErrorMessage("An error occurred while reconnecting the client. Please try again.");
            }
        }

        //Sends a message to all connected clients with the sender's username and the current timestamp.
        public async Task SendMessage(string userName, string message)
        {
            try
            {
                // Create a new chat message entity
                var chatMessage = new ChatMessage
                {
                    UserName = userName,
                    Message = message,
                    Timestamp = DateTime.UtcNow
                };

                using (var _dbContext = new AppDbContext())
                {
                    // Add the chat message to the database context and save changes
                    _dbContext.ChatMessages.Add(chatMessage);
                    await _dbContext.SaveChangesAsync();
                }

                // Retrieve all active connection IDs of currently connected users
                var connectionIds = UserConnections
                .Where(kv => ConnectedUsers.Contains(kv.Value))
                .Select(kv => kv.Key) // Select the connection IDs
                .ToList();

                Console.WriteLine("Active Connection IDs:");
                foreach (var connectionId in connectionIds)
                {
                    Console.WriteLine(connectionId);
                }

                // Send the message to all connected clients with the current timestamp
                await Clients.Clients(connectionIds).SendAsync("ReceiveMessage", userName, message, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending message");
                await SendSystemErrorMessage("An error occurred while sending your message. Please try again.");
            }
        }

        //Get the previous message from database
        public async Task RetrieveMessages()
        {
            try
            {
                using (var _dbContext = new AppDbContext())
                {
                    // Fetch the messages from the database
                    var messages = await _dbContext.ChatMessages
                                               .OrderByDescending(m => m.Timestamp)
                                               .Take(10) // Fetch the last 10 messages
                                               .ToListAsync();

                    // Send the messages to the client that requested them
                    await Clients.Caller.SendAsync("GetPreviousMessages", messages);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving messages.");
                await SendSystemErrorMessage("An error occurred while retrieving messages. Please try again.");
            }
        }

        // Helper method to send system error messages
        private async Task SendSystemErrorMessage(string message)
        {
            await Clients.Caller.SendAsync("ErrorMessage", message);
        }

        // Check the Availability of the username
        // It check the username is doesn't exist to the ConnectedUser
        public bool CheckUsernameAvailability(string userName)
        {
            return !ConnectedUsers.Contains(userName);
        }
    }
}
