using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace RewardsAndRecognitionBlazorApp.Services
{
    public class SessionStorageAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;

        public SessionStorageAuthStateProvider(IJSRuntime js)
        {
            _js = js;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var isLoggedIn = await _js.InvokeAsync<string>("browserStorage.getItem", "isLoggedIn");
                var userId = await _js.InvokeAsync<string>("browserStorage.getItem", "userId");
                var role = await _js.InvokeAsync<string>("browserStorage.getItem", "role");

                if (!string.IsNullOrWhiteSpace(isLoggedIn) && isLoggedIn == "1" && !string.IsNullOrWhiteSpace(userId))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Name, userId),
                    };

                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    var identity = new ClaimsIdentity(claims, "SessionStorageAuth");
                    var user = new ClaimsPrincipal(identity);
                    return new AuthenticationState(user);
                }

                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return new AuthenticationState(anonymous);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auth provider error: {ex.Message}");
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return new AuthenticationState(anonymous);
            }
        }

        public async Task MarkUserAsAuthenticatedAsync(string userId, string? role)
        {
            try
            {
                await _js.InvokeVoidAsync("browserStorage.setItem", "userId", userId);
                await _js.InvokeVoidAsync("browserStorage.setItem", "role", role ?? "");
                await _js.InvokeVoidAsync("browserStorage.setItem", "isLoggedIn", "1");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userId),
                };

                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, "SessionStorageAuth");
                var user = new ClaimsPrincipal(identity);

                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mark auth error: {ex.Message}");
            }
        }

        public async Task MarkUserAsLoggedOutAsync()
        {
            try
            {
                await _js.InvokeVoidAsync("browserStorage.removeItem", "userId");
                await _js.InvokeVoidAsync("browserStorage.removeItem", "role");
                await _js.InvokeVoidAsync("browserStorage.removeItem", "isLoggedIn");

                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mark logout error: {ex.Message}");
            }
        }
    }
}
