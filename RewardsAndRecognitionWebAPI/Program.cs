
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RewardsAndRecognitionRepository.Data;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionRepository.Repos;

//namespace RewardsAndRecognitionWebAPI
//{
//    public class Program
//    {
//        public static async Task Main(string[] args)
//        {
//            var builder = WebApplication.CreateBuilder(args);

//            // Add services to the container.

//            builder.Services.AddControllers();
//            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//            builder.Services.AddEndpointsApiExplorer();
//            builder.Services.AddSwaggerGen();


//            // Add DbContext
//            builder.Services.AddDbContext<ApplicationDbContext>(options =>
//                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//            // Add Identity
//            builder.Services.AddIdentity<User, IdentityRole>()
//                .AddEntityFrameworkStores<ApplicationDbContext>()
//                .AddDefaultTokenProviders();

//            builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
//                .AddCookie(IdentityConstants.ApplicationScheme);

//            builder.Services.AddAuthorization();

//            var app = builder.Build();

//            using (var scope = app.Services.CreateScope())
//                {
//                   var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//                  var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
//                  await DbInitializer.SeedRolesAndUsersAsync(roleManager, userManager);
//                  await DbInitializer.SeedAdminAsync(userManager, roleManager);
//               }

//                // Configure the HTTP request pipeline.
//                if (app.Environment.IsDevelopment())
//            {
//                app.UseSwagger();
//                app.UseSwaggerUI();
//            }

//            app.UseAuthorization();


//            app.MapControllers();

//            app.Run();
//        }
//    }
//}


namespace RewardsAndRecognitionWebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Ensure console logging is enabled and verbose for diagnostics
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);

            // Controllers & Swagger
            builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                {
                    // Prevent System.Text.Json exception on cycles in EF Core navigation properties
                    opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                    // Serialize enums as strings so the Blazor client (which uses string enums) can deserialize
                    opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                    // Use camelCase for property names
                    opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Identity
            builder.Services
                .AddIdentity<User, IdentityRole>(options =>
                {
                    // Optional: configure password/lockout here
                    options.Password.RequireDigit = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Authentication (cookie) - configure Identity's application cookie
            builder.Services.ConfigureApplicationCookie(options =>
            {
                // For SPA/API use, avoid redirecting to login page on API calls
                options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
                {
                    OnRedirectToLogin = ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                };

                // Cookies must be available for cross-site requests from your Blazor dev server.
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.SlidingExpiration = true;
            });

            builder.Services.AddAuthorization();

            // CORS - allow your Blazor app to call this API
            // Adjust origins to match your Blazor URL (use https)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("BlazorClient", policy =>
                {
                    policy.WithOrigins("http://localhost:5222", "https://localhost:5222")
                          .SetIsOriginAllowedToAllowWildcardSubdomains()
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddScoped<IUserRepo, UserRepo>();
            builder.Services.AddScoped<ITeamRepo, TeamRepo>();
            builder.Services.AddScoped<IYearQuarterRepo, YearQuarterRepo>();
            builder.Services.AddScoped<ICategoryRepo, CategoryRepo>();
            builder.Services.AddScoped<INominationRepo, NominationRepo>();
            builder.Services.AddScoped<IApprovalRepo, ApprovalRepo>();

            // Configure EmailSettings from appsettings.json
            builder.Services.Configure<RewardsAndRecognitionRepository.Models.EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));
            
            // Register EmailService
            builder.Services.AddScoped<RewardsAndRecognitionRepository.Service.IEmailService, EmailService>();


            var app = builder.Build();

            // Seed roles & users (log any exceptions)
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                    // Only run seeding when there are no roles or no users yet
                    var hasAnyRoles = await roleManager.Roles.AnyAsync();
                    var hasAnyUsers = await userManager.Users.AnyAsync();

                    if (!hasAnyRoles || !hasAnyUsers)
                    {
                        await DbInitializer.SeedRolesAndUsersAsync(roleManager, userManager);
                        await DbInitializer.SeedAdminAsync(userManager, roleManager);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception during seeding: " + ex.ToString());
            }

            // Middleware pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // CORS must be applied after UseRouting and before auth/authorization
            app.UseCors("BlazorClient");

            // Request logging middleware (development helper)
            app.Use(async (context, next) =>
            {
                try
                {
                    var loggerFactory = context.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILoggerFactory)) as Microsoft.Extensions.Logging.ILoggerFactory;
                    var logger = loggerFactory?.CreateLogger("RequestLogger");
                    var method = context.Request.Method;
                    var path = context.Request.Path + context.Request.QueryString;
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    var ua = context.Request.Headers["User-Agent"].ToString();
                    logger?.LogInformation("Incoming request: {Method} {Path} from {IP} UA:{UA}", method, path, ip, ua);
                }
                catch { }
                await next();
            });

            //  MUST be before UseAuthorization()
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Log unhandled exceptions that may occur on background threads
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                try
                {
                    Console.Error.WriteLine("Unhandled exception (AppDomain): " + eventArgs.ExceptionObject?.ToString());
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
            {
                try
                {
                    Console.Error.WriteLine("Unobserved task exception: " + eventArgs.Exception?.ToString());
                    eventArgs.SetObserved();
                }
                catch { }
            };

            // Wire host lifetime events to capture shutdown reason
            var lifetime = app.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() => Console.WriteLine("Host lifetime: ApplicationStarted"));
            lifetime.ApplicationStopping.Register(() => Console.WriteLine("Host lifetime: ApplicationStopping"));
            lifetime.ApplicationStopped.Register(() => Console.WriteLine("Host lifetime: ApplicationStopped"));

            try
            {
                app.Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Host terminated unexpectedly: " + ex.ToString());
                throw;
            }
        }
    }
}

