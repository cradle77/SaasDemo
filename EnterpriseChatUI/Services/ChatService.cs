using EnterpriseChat.Shared;
using EnterpriseChat.Shared.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace EnterpriseChatUI.Services
{
    public class ChatService
    {
        private IHttpClientFactory _clientFactory;
        private AuthenticationStateProvider _authenticationStateProvider;

        public ChatService(IHttpClientFactory clientFactory, AuthenticationStateProvider authenticationStateProvider)
        {
            _clientFactory = clientFactory;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<IEnumerable<ChatRoom>> GetRoomsAsync()
        {
            var user = await _authenticationStateProvider.GetAuthenticationStateAsync();
            if (!user.User.Identity.IsAuthenticated)
            {
                return null;
            }

            var client = _clientFactory.CreateClient("EnterpriseChatApi");
            var response = await client.GetFromJsonAsync<IEnumerable<ChatRoom>>("rooms");

            return response;
        }

        public async Task<ChatRoom> GetRoomByIdAsync(int roomId)
        {
            var client = _clientFactory.CreateClient("EnterpriseChatApi");
            var response = await client.GetFromJsonAsync<ChatRoom>($"rooms/{roomId}");

            return response;
        }

        public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(int roomId)
        {
            var client = _clientFactory.CreateClient("EnterpriseChatApi");
            var response = await client.GetFromJsonAsync<IEnumerable<ChatMessage>>($"rooms/{roomId}/messages");

            return response;
        }

        public async Task<IEnumerable<ChatMessage>> AddMessageAsync(int roomId, string content)
        {
            var message = new ChatMessageViewModel()
            {
                Content = content,
            };

            var client = _clientFactory.CreateClient("EnterpriseChatApi");

            await client.PostAsJsonAsync($"rooms/{roomId}/messages", message);
            
            var response = await client.GetFromJsonAsync<IEnumerable<ChatMessage>>($"rooms/{roomId}/messages");

            return response;
        }
    }
}
