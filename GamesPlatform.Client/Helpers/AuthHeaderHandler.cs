// DelegatingHandler для автоматического добавления токена к запросам
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace GamesPlatform.Client.Helpers
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly IJSRuntime _js;

        public AuthHeaderHandler(IJSRuntime js) => _js = js;

        // Перехват запроса и добавление Authorization-заголовка
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", "auth_token");
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            return await base.SendAsync(request, cancellationToken);
        }
    }
}