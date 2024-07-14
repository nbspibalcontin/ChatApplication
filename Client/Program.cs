using Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private static HubConnection hubConnection;
    private static string userName;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to the simple chat application");

        // Initialize Hub Connection
        InitializeConnection();

        // Handlers for receiving messages and updates from the server
        RegisterHubEvents();

        // Connect to the server and join the chat
        await ConnectAndJoinChat();

        Console.ReadLine();
    }

    static void InitializeConnection()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/chathub")
            .Build();
    }

    static async Task ConnectAndJoinChat()
    {
        try
        {
            // Start the connection to the SignalR server
            await hubConnection.StartAsync();

            while (true)
            {
                Console.WriteLine("Enter 1 to join the chat or type 'exit' to quit");
                var input = Console.ReadLine()?.Trim().ToLower();

                switch (input)
                {
                    case "exit":
                        if (ConfirmExit())
                        {
                            await hubConnection.DisposeAsync();
                            Environment.Exit(0);
                        }
                        break;

                    case "1":
                        await JoinChat();
                        break;

                    default:
                        Console.WriteLine("Please input a valid value");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task JoinChat()
    {
        Console.Write("Enter your username: ");
        userName = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(userName))
        {
            Console.WriteLine("Username cannot be empty.");
            return;
        }

        if (await CheckUsernameAvailability(userName))
        {
            await hubConnection.SendAsync("ConnectUser", userName);
            Console.WriteLine("\nYou have joined the chat. Type 'p' to see previous messages. Type 'discon' to disconnect from the chat");

            await StartChatInteraction();
        }
        else
        {
            Console.WriteLine("\nUsername is already taken. Please choose another.");
        }
    }

    static async Task StartChatInteraction()
    {
        while (true)
        {
            Console.Write(">");
            var message = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine("Message cannot be empty.");
                continue;
            }

            switch (message.ToLower())
            {
                case "p":
                    await hubConnection.SendAsync("RetrieveMessages"); // Retrieve the previous messages
                    break;
                case "discon":
                    await PromptDisconnect(); // Ask the user for confirmation before disconnecting
                    break;
                default:
                    await SendMessageToServer(message);
                    break;
            }
        }
    }

    // Send the message to the server
    static async Task SendMessageToServer(string message)
    {
        try
        {
            await hubConnection.SendAsync("SendMessage", userName, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send message: {ex.Message}");
        }
    }

    static bool ConfirmExit()
    {
        Console.WriteLine("Are you sure you want to exit? (y/n)");
        var confirmExit = Console.ReadLine()?.Trim().ToLower();
        return confirmExit == "y";
    }

    // This method is responsible for receiving data from the server
    static void RegisterHubEvents()
    {
        // Handle incoming data when a user joins the chat, updating the console accordingly.
        hubConnection.On<string, IEnumerable<string>>("UserJoined", (userName, users) =>
        {
            Console.WriteLine($"\nUser joined: {userName}");
            DisplayConnectedUsers(users);
        });

        // Handle the disconnect user from the chat room
        hubConnection.On<string, IEnumerable<string>>("UserDisconnected", (userName, users) =>
        {
            Console.WriteLine($"\nUser disconnected: {userName}");
            DisplayConnectedUsers(users);
        });

        // Handle the reconnect user from the chat room
        hubConnection.On<string, IEnumerable<string>>("ReconnectUser", (userName, users) =>
        {
            Console.WriteLine($"{userName} reconnected with the room");
            DisplayConnectedUsers(users);
        });

        // Handle the reconnect user from the chat room with new Username
        hubConnection.On<string, IEnumerable<string>>("ReconnectUserWithNewUsername", (newUserName, users) =>
        {
            Console.WriteLine($"{userName} reconnected with new name {newUserName} as the previous one was taken.");
            DisplayConnectedUsers(users);
        });

        // Handle incoming data when a user sends a message, updating the console accordingly.
        hubConnection.On<string, string, DateTime>("ReceiveMessage", (userName, message, timestamp) =>
        {
            Console.WriteLine($"\n{timestamp.ToLocalTime()} {userName}: {message}");
        });

        // Handle incoming previous messages from the server.
        hubConnection.On<List<ChatMessage>>("GetPreviousMessages", (messages) =>
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
        hubConnection.On<string>("ErrorMessage", (message) =>
        {
            Console.WriteLine($"\n{message}");
        });

        // Check the WebSocket if running or closed unexpectedly
        hubConnection.Closed += async (exception) =>
        {
            Console.WriteLine($"Connection closed: {exception?.Message}");
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await hubConnection.StartAsync();
        };
    }

    // Display all the connected users
    static void DisplayConnectedUsers(IEnumerable<string> users)
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

    // Prompt if the user wants to disconnect 
    static async Task PromptDisconnect()
    {
        while (true)
        {
            Console.WriteLine("Are you sure you want to disconnect? (y/n)");
            var confirmDisconnect = Console.ReadLine()?.Trim().ToLower();

            switch (confirmDisconnect)
            {
                case "y":
                    await DisconnectUser();
                    await PromptReconnect();
                    return; // Exit method after disconnecting

                case "n":
                    Console.WriteLine("Disconnect canceled. Continuing chat.");
                    await StartChatInteraction(); // Return to chat interaction
                    return; // Exit method without disconnecting

                default:
                    Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
                    break;
            }
        }
    }

    // Disconnect the user from the chat room
    static async Task DisconnectUser()
    {
        try
        {
            if (hubConnection != null && hubConnection.State == HubConnectionState.Connected)
            {
                // Send a request to the server to disconnect the user
                await hubConnection.SendAsync("DisconnectUser", userName);
                Console.WriteLine($"{userName} is disconnected from the chat room");

                await hubConnection.StopAsync(); // Stop the HubConnection
                await hubConnection.DisposeAsync(); // Dispose the HubConnection
                hubConnection = null; // Set the connection to null
            }
            else
            {
                Console.WriteLine("No active connection to disconnect.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during disconnection: {ex.Message}");
        }
    }

    // Prompt if the user wants to reconnect 
    static async Task PromptReconnect()
    {
        while (true)
        {
            Console.WriteLine("Type 'recon' to reconnect, Type 'exit' to quit");
            var input = Console.ReadLine();

            if (input.ToLower() == "recon")
            {
                await ReconnectUser();
                break;
            }
            else if (input.ToLower() == "exit")
            {
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("Please input a valid value");
            }
        }
    }

    // Reconnect user to the chat room
    static async Task ReconnectUser()
    {
        try
        {
            if (hubConnection == null)
            {
                // Reinitialize connection and register events
                InitializeConnection();
                RegisterHubEvents();
            }

            if (hubConnection.State == HubConnectionState.Disconnected)
            {
                await hubConnection.StartAsync(); // Start the connection
            }

            while (true)
            {
                // Check if the username still available
                if (await CheckUsernameAvailability(userName))
                {
                    await hubConnection.SendAsync("ReconnectUser", userName); // Send a request to reconnect the user
                    break;
                }
                else
                {
                    string newUserName = await PromptForNewUsername();
                    if (!string.IsNullOrEmpty(newUserName))
                    {
                        userName = newUserName;
                        await hubConnection.SendAsync("ReconnectUserWithNewUsername", newUserName); // Send a request to reconnect the user
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during reconnection: {ex.Message}");
        }
    }

    static async Task<string> PromptForNewUsername()
    {
        while (true)
        {
            Console.WriteLine("\nUsername is already taken. Please choose another.");
            Console.Write("Enter a new username: ");
            var newUserName = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(newUserName))
            {
                Console.WriteLine("Username cannot be empty. Please enter a valid username.");
                continue;
            }

            if (newUserName == userName)
            {
                Console.WriteLine("Your previous username is already taken after you disconnect. Please choose another.");
                continue; // Prompt again for a different username
            }

            if (await CheckUsernameAvailability(newUserName))
            {
                return newUserName;
            }

            Console.WriteLine("Username is already taken. Please choose another.");
        }
    }

    // Check if the username is available
    static async Task<bool> CheckUsernameAvailability(string userName)
    {
        try
        {
            return await hubConnection.InvokeAsync<bool>("CheckUsernameAvailability", userName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking username availability: {ex.Message}");
            return false;
        }
    }
}
