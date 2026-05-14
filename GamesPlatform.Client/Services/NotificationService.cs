using Microsoft.JSInterop;

namespace GamesPlatform.Client.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IJSRuntime _js;
        public event Action<string, string>? OnNotify;

        public NotificationService(IJSRuntime js)
        {
            _js = js;
        }

        public void Show(string message, string type = "info")
        {
            OnNotify?.Invoke(message, type);
        }

        public async Task<bool> Confirm(string message)
        {
            return await _js.InvokeAsync<bool>("confirm", message);
        }
    }
}