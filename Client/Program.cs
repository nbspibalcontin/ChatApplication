using Client;
using Client.Services.Implementation;
using Client.Services.Interface;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private static IChatService _chatService;
    private static string userName;

    static async Task Main(string[] args)
    {
        _chatService = new ChatService();

        Console.WriteLine("Welcome to the simple chat application");
  
        // Initialize ChatService instance
        _chatService.InitializeConnection();

        // Handlers for receiving messages and updates from the server
        _chatService.RegisterHubEvents();

        // Connect to the server and join the chat
        await ConnectAndJoinChat();

        Console.ReadLine();
    }

    static async Task ConnectAndJoinChat()
    {
        try
        {
            // Start the connection to the SignalR server
            await _chatService.ConnectAsync();

            while (true)
            {
                Console.WriteLine("Enter 1 to join the chat or type 'exit' to quit");
                var input = Console.ReadLine()?.Trim().ToLower();

                switch (input)
                {
                    case "exit":
                        if (ConfirmExit())
                        {
                            await _chatService.DisconnectAsync();
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

        if (await _chatService.CheckUsernameAvailabilityAsync(userName))
        {
            await _chatService.ConnectUserAsync(userName);
            Console.WriteLine("\nYou have joined the chat. Type 'p' to see previous messages. Type 'discon' to disconnect from the chat");

            await StartChatInteraction(); // Start chat interaction after successful join
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
                    await _chatService.RetrieveMessagesAsync(); // Retrieve the previous messages
                    break;
                case "discon":
                    await PromptDisconnect(); // Ask the user for confirmation before disconnecting
                    break;
                default:
                    await _chatService.SendMessageAsync(userName, message); // Send the message to the server
                    break;
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
                    await _chatService.DisconnectUserAsync(userName);
                    await _chatService.DisconnectAsync();
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

    // Prompt if the user wants to reconnect 
    static async Task PromptReconnect()
    {
        while (true)
        {
            Console.WriteLine("Type 'recon' to reconnect, Type 'exit' to quit");
            var input = Console.ReadLine()?.ToLower();

            switch (input)
            {
                case "recon":
                    await ReconnectUser();
                    return; // Exit the method after reconnecting

                case "exit":
                     if (ConfirmExit())
                        {
                            Environment.Exit(0);
                        }
                        break;

                default:
                    Console.WriteLine("Please input a valid value");
                    break;
            }
        }
    }

    // Reconnect user to the chat room
    static async Task ReconnectUser()
    {
        try
        {
            // Reinitialize connection and register events
            _chatService.InitializeConnection();
            _chatService.RegisterHubEvents();
            await _chatService.ConnectAsync();

            while (true)
            {
                // Check if the username still available
                if (await _chatService.CheckUsernameAvailabilityAsync(userName))
                {
                    await _chatService.ReconnectUserAsync(userName); // Send a request to reconnect the user with the same username
                    break;
                }
                else
                {
                    string newUserName = await PromptForNewUsername();
                    if (!string.IsNullOrEmpty(newUserName))
                    {
                        await _chatService.ReconnectUserWithNewUsernameAsync(userName, newUserName); // Send a request to reconnect the user with the new username
                        userName = newUserName;
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

    // Prompt for the NewUsername
    static async Task<string> PromptForNewUsername()
    {
        while (true)
        {
            Console.WriteLine("\nYour previous username is already taken after you disconnect. Please choose another.");

            Console.Write("Enter a new username: ");
            var newUserName = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(newUserName))
            {
                Console.WriteLine("Username cannot be empty. Please enter a valid username.");
                continue;
            }

            // Check if the username is available
            if (await _chatService.CheckUsernameAvailabilityAsync(newUserName))
            {
                return newUserName;
            } 
        }
    }

    // Exit comfirmation
    static bool ConfirmExit()
    {
        while (true)
        {
            Console.WriteLine("Are you sure you want to quit? (y/n)");
            var confirmExit = Console.ReadLine()?.Trim().ToLower();

            switch (confirmExit)
            {
                case "y":
                    return true;

                case "n":
                    return false;

                default:
                    Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
                    break;
            }
        }
    }
}
