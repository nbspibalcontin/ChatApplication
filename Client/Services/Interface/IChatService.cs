using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Services.Interface
{
    public interface IChatService
    {
        Task ConnectAsync();
        Task DisconnectAsync();
        Task<bool> CheckUsernameAvailabilityAsync(string userName);
        Task ConnectUserAsync(string userName);
        Task DisconnectUserAsync(string userName);
        Task SendMessageAsync(string userName, string message);
        Task RetrieveMessagesAsync();
        Task ReconnectUserAsync(string userName);
        Task ReconnectUserWithNewUsernameAsync(string oldUserName, string newUserName);
        void RegisterHubEvents();
        void InitializeConnection();
    }
}
