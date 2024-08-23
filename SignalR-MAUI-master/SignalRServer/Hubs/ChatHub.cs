using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SignalRServer.Hubs
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, string> userConnections = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string> phoneNumberToUserId = new ConcurrentDictionary<string, string>();

        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null && httpContext.Request.Headers.TryGetValue("PhoneNumber", out var phoneNumber))
            {
                var userId = Context.ConnectionId; // kullanıcı id olarak bağlantı kimliğini kullanıyoruz
                userConnections[userId] = Context.ConnectionId;
                phoneNumberToUserId[phoneNumber] = userId; // telefon numarasını kullanıcı ID'si ile eşle
                Console.WriteLine($"User connected: {userId} with phone number: {phoneNumber}");
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null && httpContext.Request.Headers.TryGetValue("PhoneNumber", out var phoneNumber))
            {
                if (phoneNumberToUserId.TryRemove(phoneNumber, out var userId))
                {
                    userConnections.TryRemove(userId, out _);
                    Console.WriteLine($"User disconnected: {userId}");
                }
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessageToUserByPhoneNumber(string phoneNumber, string user, string message)
        {
            if (phoneNumberToUserId.TryGetValue(phoneNumber, out var userId) && userConnections.TryGetValue(userId, out string? connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", user, message);
            }
        }

        public async Task SendMessageToAll(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
