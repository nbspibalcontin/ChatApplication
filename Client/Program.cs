using Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;

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
                var input = Console.ReadLine();

                if (input.ToLower() == "exit")
                {
                    Console.WriteLine("Are you sure you want to exit? (y/n)");
                    var confirmExit = Console.ReadLine()?.Trim().ToLower();
                    if (confirmExit == "y")
                    {
                        await hubConnection.DisposeAsync();
                        Environment.Exit(0);
                    }
                }
                else if (input == "1")
                {
                    Console.Write("Enter your username: ");
                    userName = Console.ReadLine();

                    if (!string.IsNullOrEmpty(userName))
                    {
                        //Check if the username is already taken.
                        bool isAvailable = await CheckUsernameAvailability(userName);
                        if (isAvailable)
                        {
                            await hubConnection.SendAsync("ConnectUser", userName);
                            Console.WriteLine("\nYou have joined the chat. Type 'p' to see previous messages. Type 'discon' to disconnect from the chat");

                            await StartChatInteraction(userName);
                        }
                        else
                        {
                            Console.WriteLine("\nUsername is already taken. Please choose another.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Username cannot be empty.");
                    }
                }
                else
                {
                    Console.WriteLine("Please input a valid value");
                }   
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task StartChatInteraction(string user)
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
                    await hubConnection.SendAsync("RetrieveMessages"); //Retrieve the previous message
                    break;
                case "discon":
                    await PromptDisconnect(); // Ask the user for confirmation before disconnecting
                    break;
                default:
                    await SendMessageToServer(user, message);
                    break;
            }
        }
    }

    // Send the message to the server
    static async Task SendMessageToServer(string user, string message)
    {
        try
        {
            await hubConnection.SendAsync("SendMessage", user, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send message: {ex.Message}");
        }
    }

    //This responsible for recieving data from server
    static void RegisterHubEvents()
    {
        //Handles incoming data when a user joins the chat, updating the console accordingly.
        hubConnection.On<string, IEnumerable<string>>("UserJoined", (user, users) =>
        {
            Console.WriteLine($"\nUser joined: {user}");
            DisplayConnectedUsers(users);
        });

        //Handle the disconnect user from the chat room
        hubConnection.On<string, IEnumerable<string>>("UserDisconnected", (user, users) =>
        {
            Console.WriteLine($"\nUser disconnected: {user}");
            DisplayConnectedUsers(users);
        });

        //Handles incoming data when a user send message, updating the console accordingly.
        hubConnection.On<string, string, DateTime>("ReceiveMessage", (user, message, timestamp) =>
        {
            Console.WriteLine($"\n{timestamp.ToLocalTime()} {user}: {message}");
        });

        //Handle incoming previous messages from the server.
        hubConnection.On<List<ChatMessage>>("GetPreviousMessages", (messages) =>
        {
            //Check if no messages found in the database
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

        //Handles incoming friendly error message from the server
        hubConnection.On<string>("ErrorMessage", (message) =>
        {
            Console.WriteLine($"\n{message}");
        });

        //Check the Websocket if running or close unexpectedly
        hubConnection.Closed += async (exception) =>
        {
            Console.WriteLine($"Connection closed: {exception?.Message}");
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await hubConnection.StartAsync();
        };
    }

    //Check if the username is available
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

    //Display all the connected users
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

    //Prompt if the user want to disconnect 
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
                    await StartChatInteraction(userName); // Return to chat interaction
                    return; // Exit method without disconnecting

                default:
                    Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
                    break;
            }
        }
    }

    //Disconncect the user from the chat room
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

    //Prompt if the user want to reconnect 
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

    //Reconnect user to the chat room
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

            await hubConnection.SendAsync("ConnectUser", userName); // Send a request to reconnect the user
            Console.WriteLine($"{userName} is reconnected to the chat room");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during reconnection: {ex.Message}");
        }
    }
}
