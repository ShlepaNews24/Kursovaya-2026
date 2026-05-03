namespace GamesPlatform.Client.Services
{
    public interface INotificationService
    {
        // Событие для подписки компонентов на уведомления
        event Action<string, string>? OnNotify;
        
        // Метод вызова уведомления (type: success/error/info/warning)
        void Show(string message, string type = "info");
    }
}