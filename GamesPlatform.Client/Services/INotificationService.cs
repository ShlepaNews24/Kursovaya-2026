namespace GamesPlatform.Client.Services
{
    public interface INotificationService
    {
        event Action<string, string>? OnNotify;
        void Show(string message, string type = "info");
        Task<bool> Confirm(string message);
    }
}