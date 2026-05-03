namespace GamesPlatform.Client.Services
{
    public class NotificationService : INotificationService
    {
        public event Action<string, string>? OnNotify;

        public void Show(string message, string type = "info")
        {
            // Проверка на подписчиков перед вызовом
            OnNotify?.Invoke(message, type);
        }
    }
}