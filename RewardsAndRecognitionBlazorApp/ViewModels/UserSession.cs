using Microsoft.JSInterop;

namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    public class UserSession
    {
        private IJSRuntime? _jsRuntime;
        private bool _initialized = false;
        private string? _userId;
        private string? _role;

        public string? UserId
        {
            get => _userId;
            set => _userId = value;
        }

        public string? Role
        {
            get => _role;
            set => _role = value;
        }

        public async Task InitializeAsync(IJSRuntime jsRuntime, bool forceRefresh = false)
        {
            // If already initialized and not forcing a refresh, do nothing
            if (_initialized && !forceRefresh)
                return;

            _jsRuntime ??= jsRuntime;

            if (_jsRuntime == null)
            {
                System.Diagnostics.Debug.WriteLine("UserSession.InitializeAsync called without an IJSRuntime available.");
                return;
            }

            try
            {
                // Read latest values from browser sessionStorage via the JS helper
                _userId = await _jsRuntime.InvokeAsync<string>("browserStorage.getItem", "userId");
                _role = await _jsRuntime.InvokeAsync<string>("browserStorage.getItem", "role");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize UserSession: {ex.Message}");
            }

            _initialized = true;
        }

        public async Task ClearSessionAsync(IJSRuntime jsRuntime)
        {
            _jsRuntime ??= jsRuntime;
            
            if (_jsRuntime != null)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("browserStorage.removeItem", "userId");
                    await _jsRuntime.InvokeVoidAsync("browserStorage.removeItem", "role");
                    await _jsRuntime.InvokeVoidAsync("browserStorage.removeItem", "authToken");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to clear session: {ex.Message}");
                }
            }

            _userId = null;
            _role = null;
            _initialized = false;
        }

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(_userId);
    }
}