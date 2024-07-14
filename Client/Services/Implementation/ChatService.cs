using Client.Services.Interface;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Services.Implementation
{
    public class ChatService : IChatService
    {
        private HubConnection _hubConnection;      

        public void InitializeConnection()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/chathub")
                .Build();
        }

        // Send a Signal to the server to connect the user
        public async Task ConnectUserAsync(string userName)
        {
            await _hubConnection.SendAsync("ConnectUser", userName);
        }

        // Check if the username is already Exist to the ConnectedUsers
        public async Task<bool> CheckUsernameAvailabilityAsync(string userName)
        {
            return await _hubConnection.InvokeAsync<bool>("CheckUsernameAvailability", userName);
        }

        // Start the connection to the hub
        public async Task ConnectAsync()
        {
            //Check of the HubConnection is disconnect to the server
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync(); // Start the connection
            }
        }

        // Disconnect the connection to the Hub
        public async Task DisconnectAsync()
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
        }

        // Send a Signal to the server to disconnect the user
        public async Task DisconnectUserAsync(string userName)
        {
            await _hubConnection.SendAsync("DisconnectUser", userName);
        }

        // Send a Signal to the server to reconnect the user
        public async Task ReconnectUserAsync(string userName)
        {
            await _hubConnection.SendAsync("ReconnectUser", userName);
        }

        // Send a Signal to the server to reconnect the user with newUsername
        public async Task ReconnectUserWithNewUsernameAsync(string oldUserName, string newUserName)
        {
            await _hubConnection.SendAsync("ReconnectUserWithNewUsername", oldUserName, newUserName);
        }

        // Send a Signal to the server to retrieve the previous conversation
        public async Task RetrieveMessagesAsync()
        {
            await _hubConnection.SendAsync("RetrieveMessages");
        }

        // Send a Signal to the server to to send a message to all connected users
        public async Task SendMessageAsync(string userName, string message)
        {
            try
            {
                await _hubConnection.SendAsync("SendMessage", userName, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message: {ex.Message}");
            }
        }

        // This method is responsible for receiving data from the server then display it to the console
        public void RegisterHubEvents()
        {
            // Handle incoming data when a user joins the chat, updating the console accordingly.
            _hubConnection.On<string, IEnumerable<string>>("UserJoined", (userName, users) =>
            {
                Console.WriteLine($"\nUser joined: {userName}");
                DisplayConnectedUsers(users);
            });

            // Handle the disconnect user from the chat room
            _hubConnection.On<string, IEnumerable<string>>("UserDisconnected", (userName, users) =>
            {
                Console.WriteLine($"\nUser disconnected: {userName}");
                DisplayConnectedUsers(users);
            });

            // Handle the reconnect user from the chat room
            _hubConnection.On<string, IEnumerable<string>>("ReconnectUser", (userName, users) =>
            {
                Console.WriteLine($"{userName} reconnected with the room");
                DisplayConnectedUsers(users);
            });

            // Handle the reconnect user from the chat room with new Username
            _hubConnection.On<string, string, IEnumerable<string>>("ReconnectUserWithNewUsername", (oldUserName, newUserName, users) =>
            {
                Console.WriteLine($"{oldUserName} reconnected with new name {newUserName} as the previous one was taken.");
                DisplayConnectedUsers(users);
            });

            // Handle incoming data when a user sends a message, updating the console accordingly.
            _hubConnection.On<string, string, DateTime>("ReceiveMessage", (userName, message, timestamp) =>
            {
                Console.WriteLine($"\n{timestamp.ToLocalTime()} {userName}: {message}");
            });

            // Handle incoming previous messages from the server.
            _hubConnection.On<List<ChatMessage>>("GetPreviousMessages", (messages) =>
            {
                // Check if no messages found in the database
                if (messages == null || messages.Count == 0)
                {
                    Console.WriteLine("\nNo previous data found.");
                }
                else
                {
                    // Display each received chat message with its timestamp, username, and message.
                    Console.WriteLine("\nPrevious Messages:");
                    foreach (var message in messages)
                    {
                        Console.WriteLine($"{message.Timestamp.ToLocalTime()} {message.UserName}: {message.Message}");
                    }
                }
            });

            // Handle incoming friendly error messages from the server
            _hubConnection.On<string>("ErrorMessage", (message) =>
            {
                Console.WriteLine($"\n{message}");
            });

            // Check the WebSocket if running or closed unexpectedly
            _hubConnection.Closed += (exception) =>
            {
                Console.WriteLine($"Connection closed: {exception?.Message}");
                return Task.CompletedTask;
            };
        }

        // Display all the connected users
        public void DisplayConnectedUsers(IEnumerable<string> users)
        {
            if (users == null || !users.Any())
            {
                Console.WriteLine("No users connected.");
            }
            else
            {
                Console.WriteLine("Connected users:");
                foreach (var u in users)
                {
                    Console.WriteLine($"- {u}");
                }
            }
        }
    }
}
