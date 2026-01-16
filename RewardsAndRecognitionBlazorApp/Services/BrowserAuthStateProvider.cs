using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using RewardsAndRecognitionBlazorApp.ViewModels;

namespace RewardsAndRecognitionBlazorApp.Services
{
    public class BrowserAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;
        private readonly UserSession _session;

        public BrowserAuthStateProvider(IJSRuntime js, UserSession session)
        {
            _js = js;
            _session = session;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Ensure UserSession is initialized from localStorage
                await _session.InitializeAsync(_js);

                if (string.IsNullOrWhiteSpace(_session.UserId))
                {
                    var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                    return new AuthenticationState(anonymous);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, _session.UserId),
                    new Claim(ClaimTypes.Name, _session.UserId),
                };

                if (!string.IsNullOrWhiteSpace(_session.Role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, _session.Role));
                }

                var identity = new ClaimsIdentity(claims, "Local");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auth error: {ex.Message}");
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return new AuthenticationState(anonymous);
            }
        }

        // Call this after successful login
        public async Task MarkUserAsAuthenticatedAsync(string userId, string? role)
        {
            try
            {
                await _js.InvokeVoidAsync("browserStorage.setItem", "userId", userId);
                await _js.InvokeVoidAsync("browserStorage.setItem", "role", role ?? "");
                await _js.InvokeVoidAsync("browserStorage.setItem", "isLoggedIn", "1");

                // Update in-memory session (force refresh to pick up new storage values)
                await _session.InitializeAsync(_js, forceRefresh: true);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userId),
                };

                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, "Local");
                var user = new ClaimsPrincipal(identity);

                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mark authenticated error: {ex.Message}");
            }
        }

        // Call this on logout
        public async Task MarkUserAsLoggedOutAsync()
        {
            try
            {
                await _js.InvokeVoidAsync("browserStorage.removeItem", "userId");
                await _js.InvokeVoidAsync("browserStorage.removeItem", "role");
                await _js.InvokeVoidAsync("browserStorage.removeItem", "isLoggedIn");

                // Refresh in-memory session to clear values
                await _session.InitializeAsync(_js, forceRefresh: true);

                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mark logged out error: {ex.Message}");
            }
        }
    }
}
