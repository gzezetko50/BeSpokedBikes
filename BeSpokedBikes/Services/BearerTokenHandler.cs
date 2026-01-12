using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BeSpokedBikes.Services
{
    public sealed class BearerTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string TokenCookieName = "auth_token";

        public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Store the token in a cookie; respect RememberMe for expiration
        public Task StoreTokenAsync(HttpContext httpContext, string token, bool rememberMe)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                // Allow cookie on HTTP in dev; stays Secure when using HTTPS
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict
            };

            if (rememberMe)
            {
                // persist for 7 days; adjust as needed
                options.Expires = System.DateTimeOffset.UtcNow.AddDays(7);
            }

            httpContext.Response.Cookies.Append(TokenCookieName, token, options);
            return Task.CompletedTask;
        }

        // Attach Authorization header for outgoing API calls
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ctx = _httpContextAccessor.HttpContext;
            var token = ctx?.Request.Cookies[TokenCookieName];

            if (!string.IsNullOrWhiteSpace(token) && request.Headers.Authorization is null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}