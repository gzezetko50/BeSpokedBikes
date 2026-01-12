using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeSpokedBikes.Services;

namespace BeSpokedBikes.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ApiClient _apiClient;
        private readonly BearerTokenHandler _tokenHandler;

        public LoginModel(ApiClient apiClient, BearerTokenHandler tokenHandler)
        {
            _apiClient = apiClient;
            _tokenHandler = tokenHandler;
        }

        public class InputModel
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Ensure GET renders the page
        public void OnGet() { }

        // Server-side POST handler (non-AJAX). Ensures cookie is set and server redirects.
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Username) || string.IsNullOrWhiteSpace(Input.Password))
            {
                ModelState.AddModelError(string.Empty, "Username and password are required.");
                return Page();
            }

            try
            {
                var login = await _apiClient.LoginAsync(Input.Username, Input.Password);

                // Store API token for outbound requests (persistent cookie handled there)
                await _tokenHandler.StoreTokenAsync(HttpContext, login.token, rememberMe: false);

                // Authenticate the Razor Pages user via cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Input.Username),
                    // add role/other claims from 'login' if available
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false, // set true if you have "Remember me"
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                // Signal the layout to show a one-time success message after the server redirect.
                TempData["showSuccess"] = "true";
                TempData["username"] = Input.Username;

                var redirect = string.IsNullOrWhiteSpace(login.redirect) ? "/" : login.redirect;
                return Redirect(redirect);
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Unexpected error during login.");
                return Page();
            }
        }

        // Existing AJAX login handler unchanged except TempData removed
        public async Task<IActionResult> OnPostAjaxAsync([FromBody] InputModel input)
        {
            if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Password))
                return new JsonResult(new { success = false, error = "Username and password are required." }) { StatusCode = 400 };

            try
            {
                var login = await _apiClient.LoginAsync(input.Username, input.Password);

                // Store API token for outbound requests
                await _tokenHandler.StoreTokenAsync(HttpContext, login.token, rememberMe: false);

                // Authenticate the Razor Pages user via cookie
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, input.Username),
                    // add role/other claims from 'login' if available
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false, // set true if you have "Remember me"
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                var redirect = string.IsNullOrWhiteSpace(login.redirect) ? "/" : login.redirect;
                return new JsonResult(new { success = true, redirect });
            }
            catch (HttpRequestException ex)
            {
                return new JsonResult(new { success = false, error = ex.Message }) { StatusCode = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.BadRequest) };
            }
            catch
            {
                return new JsonResult(new { success = false, error = "Unexpected error during login." }) { StatusCode = 500 };
            }
        }
    }
}