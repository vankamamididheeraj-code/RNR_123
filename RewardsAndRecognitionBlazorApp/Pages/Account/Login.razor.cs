//using Microsoft.AspNetCore.Components;
//using Microsoft.Extensions.Logging;
//using System.ComponentModel.DataAnnotations;
//using static RewardsAndRecognitionBlazorApp.Pages.Login;
//using System;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Components.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.Logging;
////using RewardsAndRecognitionRepository.Models;

//namespace RewardsAndRecognitionBlazorApp.Pages
//{
//    public partial class Login : ComponentBase
//    {


//        private LoginModel loginModel = new();
//    private bool isSubmitting = false;
//    private string errorMessage;
//    private bool showPassword = false;

//    private string passwordInputType => showPassword ? "text" : "password";
//    private string passwordToggleIcon => showPassword ? "fa-eye-slash" : "fa-eye";

//    [Inject] private SignInManager<User> SignInManager { get; set; }
//    [Inject] private NavigationManager Navigation { get; set; }
//    [Inject] private ILogger<Login> Logger { get; set; }

//    // To check if user is already logged in
//    [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; }

//    protected override async Task OnInitializedAsync()
//    {
//        // Equivalent to your OnGetAsync check
//        var authState = await AuthenticationStateTask;
//        var user = authState.User;

//        if (user?.Identity != null && user.Identity.IsAuthenticated)
//        {
//            // Redirect to home if user is already logged in
//            Navigation.NavigateTo("/", true);
//            return;
//        }

//        // (If you use external providers and need to clear external cookies,
//        // that would be handled server-side similar to HttpContext.SignOutAsync)
//    }

//    private async Task HandleLogin()
//    {
//        isSubmitting = true;
//        errorMessage = null;

//        try
//        {
//            // Same as _signInManager.PasswordSignInAsync in your cshtml.cs
//            var result = await SignInManager.PasswordSignInAsync(
//                loginModel.Email,
//                loginModel.Password,
//                loginModel.RememberMe,
//                lockoutOnFailure: false);

//            if (result.Succeeded)
//            {
//                Logger.LogInformation("User logged in.");
//                // You can support returnUrl via query string if you want.
//                Navigation.NavigateTo("/", true);
//                return;
//            }

//            if (result.RequiresTwoFactor)
//            {
//                // Match your MVC redirect to LoginWith2fa page
//                // Adjust route as per your app
//                Navigation.NavigateTo($"/loginwith2fa?rememberMe={loginModel.RememberMe}", true);
//                return;
//            }

//            if (result.IsLockedOut)
//            {
//                Logger.LogWarning("User account locked out.");
//                Navigation.NavigateTo("/lockout", true);
//                return;
//            }

//            // Invalid login
//            errorMessage = "Invalid login attempt.";
//        }
//        catch (Exception ex)
//        {
//            Logger.LogError(ex, "An error occurred during login.");
//            errorMessage = "An error occurred during login.";
//        }
//        finally
//        {
//            isSubmitting = false;
//        }
//    }

//    private void TogglePasswordVisibility()
//    {
//        showPassword = !showPassword;
//    }

//    public class LoginModel
//    {
//        [Required]
//        [EmailAddress]
//        public string Email { get; set; }

//        [Required]
//        [DataType(DataType.Password)]
//        public string Password { get; set; }

//        [Display(Name = "Remember me?")]
//        public bool RememberMe { get; set; }
//    }

//}
//}
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Text.Json;
using RewardsAndRecognitionBlazorApp.ViewModels;


namespace RewardsAndRecognitionBlazorApp.Pages.Account
{
    public partial class Login : ComponentBase
    {
        private LoginModel loginModel = new();
        private bool isSubmitting;
        private string errorMessage;
        private bool showPassword;

        private string passwordInputType => showPassword ? "text" : "password";
        private string passwordToggleIcon => showPassword ? "fa-eye-slash" : "fa-eye";

        [Inject] private HttpClient Http { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private ILogger<Login> Logger { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private UserSession Session { get; set; }
        [Inject] private System.Text.Json.JsonSerializerOptions JsonOptions { get; set; }
        [Inject] private RewardsAndRecognitionBlazorApp.Services.BrowserAuthStateProvider AuthProvider { get; set; }
        [Inject] private Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider { get; set; }

        protected override async Task OnInitializedAsync()
        {
            // Load token if exists
            var token = await JS.InvokeAsync<string>("browserStorage.getItem", "authToken");
            if (!string.IsNullOrEmpty(token))
            {
                Http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            // Optional: if you have an auth state provider that can tell you if already logged in,
            // you can redirect to "/" here.
            await Task.CompletedTask;
        }

        private async Task HandleLogin()
        {
            isSubmitting = true;
            errorMessage = null;

            try
            {
                // This calls your RewardsAndRecognitionWebAPI
                // Make sure Http.BaseAddress is set to your API URL in Program.cs
                // e.g. builder.Services.AddHttpClient(...)

                // Use fetch with credentials so cookies are set in the browser
                var jsResp = await JS.InvokeAsync<JsonElement>("fetchWithCredentials", "POST", "api/account/login", loginModel);
                LoginResultDto loginResult = null;
                try
                {
                    var ok = jsResp.GetProperty("ok").GetBoolean();
                    if (ok)
                    {
                        // parse JSON body for userId/role
                        var text = jsResp.GetProperty("text").GetString() ?? string.Empty;
                        try
                        {
                            loginResult = System.Text.Json.JsonSerializer.Deserialize<LoginResultDto>(text, JsonOptions);
                            if (loginResult != null)
                            {
                                // Let the auth provider persist session and notify state changes
                                if (AuthStateProvider is RewardsAndRecognitionBlazorApp.Services.SessionStorageAuthStateProvider sessionProvider)
                                {
                                    await sessionProvider.MarkUserAsAuthenticatedAsync(loginResult.UserId, loginResult.Role);
                                }
                                else if (AuthProvider != null)
                                {
                                    await AuthProvider.MarkUserAsAuthenticatedAsync(loginResult.UserId, loginResult.Role);
                                }
                                else
                                {
                                    // Fallback: set session directly
                                    Session.UserId = loginResult.UserId;
                                    Session.Role = loginResult.Role;
                                    try
                                    {
                                        await JS.InvokeVoidAsync("browserStorage.setItem", "userId", loginResult.UserId);
                                        await JS.InvokeVoidAsync("browserStorage.setItem", "role", loginResult.Role ?? string.Empty);
                                        await JS.InvokeVoidAsync("browserStorage.setItem", "isLoggedIn", "1");
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.LogWarning(ex, "Failed to persist session to browser storage");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Failed to parse login result JSON: {Text}", text);
                        }

                        Logger.LogInformation("User logged in via API.");
                        // Navigate based on user role
                        if (loginResult?.Role == "Admin")
                        {
                            Navigation.NavigateTo("/dashboard/admin", forceLoad: true);
                        }
                        else if (loginResult?.Role == "Manager")
                        {
                            Navigation.NavigateTo("/dashboard/manager", forceLoad: true);
                        }
                        else if (loginResult?.Role == "Director")
                        {
                            Navigation.NavigateTo("/dashboard/director", forceLoad: true);
                        }
                        else if (loginResult?.Role == "TeamLead" || loginResult?.Role == "Team Lead")
                        {
                            Navigation.NavigateTo("/dashboard/teamlead", forceLoad: true);
                        }
                        else if (loginResult?.Role == "Employee")
                        {
                            Navigation.NavigateTo("/dashboard/employee", forceLoad: true);
                        }
                        else
                        {
                            Navigation.NavigateTo("/", forceLoad: true);
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "fetchWithCredentials login error");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during login call to API.");
                errorMessage = "An error occurred during login.";
            }
            finally
            {
                isSubmitting = false;
            }
        }

        private void TogglePasswordVisibility()
        {
            showPassword = !showPassword;
        }

        public class LoginModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        private sealed class LoginResultDto
{
    public string UserId { get; set; }
    public string Role { get; set; }
}
    }
}
