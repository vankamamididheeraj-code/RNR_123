using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Http;
using Microsoft.JSInterop;
//using RewardsAndRecognitionRepository.Data;
//using RewardsAndRecognitionRepository.Models;

namespace RewardsAndRecognitionBlazorApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // Register HttpClient for WebAPI calls
            builder.Services.AddHttpClient("RNRWebAPI", client =>
            {
                // Use HTTPS to avoid HTTP -> HTTPS redirect which breaks CORS preflight
                client.BaseAddress = new Uri("https://localhost:51332/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30); // Add timeout
            });

            // Default HttpClient for API calls
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("RNRWebAPI"));

            // Register UserSession service
            builder.Services.AddScoped<RewardsAndRecognitionBlazorApp.ViewModels.UserSession>();

            // Register authentication/authorization
            builder.Services.AddOptions();
            builder.Services.AddAuthorizationCore();
            // Use SessionStorage-backed AuthenticationStateProvider so role claims are read from sessionStorage
            builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, RewardsAndRecognitionBlazorApp.Services.SessionStorageAuthStateProvider>();
            // Keep BrowserAuthStateProvider registered as concrete service (some pages call it directly)
            builder.Services.AddScoped<RewardsAndRecognitionBlazorApp.Services.BrowserAuthStateProvider>();

            // Register JsonSerializerOptions with enum converter
            builder.Services.AddSingleton(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

            await builder.Build().RunAsync();
        }
    }
}
