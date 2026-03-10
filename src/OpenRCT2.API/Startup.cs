using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using OpenRCT2.API.Abstractions;
using OpenRCT2.API.AppVeyor;
using OpenRCT2.API.Authentication;
using OpenRCT2.API.Configuration;
using OpenRCT2.API.Implementations;
using OpenRCT2.API.Services;
using OpenRCT2.DB;
using OpenRCT2.DB.Abstractions;

namespace OpenRCT2.API
{
    public class Startup
    {
        private const string MainWebsite = "https://openrct2.io";

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<ApiConfig>(Configuration.GetSection("api"));
            services.Configure<DBOptions>(Configuration.GetSection("database"));
            services.Configure<EmailConfig>(Configuration.GetSection("email"));
            services.Configure<OpenRCT2org.UserApiOptions>(Configuration.GetSection("openrct2.org"));

            services.AddSingleton<Random>();
            services.AddSingleton<HttpClient>();
            services.AddSingleton<IServerRepository, ServerRepository>();
            services.AddSingleton<IAppVeyorService, AppVeyorService>();
            services.AddSingleton<ILocalisationService, LocalisationService>();
            services.AddSingleton<Emailer>();
            services.AddSingleton<GoogleRecaptchaService>();
            services.AddScoped<NeDesignsService>();
            services.AddScoped<UserAccountService>();
            services.AddScoped<UserAuthenticationService>();

            if (!HostingEnvironment.IsTesting())
            {
                services.AddSingleton<OpenRCT2org.IUserApi, OpenRCT2org.UserApi>();
                services.AddOpenRCT2DB();

                // Authentication
                services.AddAuthentication(
                    options =>
                    {
                        options.DefaultAuthenticateScheme = ApiAuthenticationOptions.DefaultScheme;
                        options.DefaultChallengeScheme = ApiAuthenticationOptions.DefaultScheme;
                    })
                    .AddApiAuthentication();
            }

            services.AddControllersWithViews();
            services.AddCors();
        }

        public void Configure(
            IServiceProvider serviceProvider,
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IOptions<DBOptions> dbOptions,
            ILogger<Startup> logger)
        {
            // Use X-Forwarded-For header for client IP address
            app.UseForwardedHeaders(
                new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.All
                });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Setup / connect to the database
            var missingDatabaseKeys = new[]
            {
                (Key: "database.host", Env: "database__host", Value: dbOptions.Value.Host),
                (Key: "database.user", Env: "database__user", Value: dbOptions.Value.User),
                (Key: "database.password", Env: "database__password", Value: dbOptions.Value.Password),
                (Key: "database.name", Env: "database__name", Value: dbOptions.Value.Name)
            }
            .Where(x => string.IsNullOrWhiteSpace(x.Value))
            .ToArray();

            if (missingDatabaseKeys.Length > 0)
            {
                logger.LogWarning(
                    "Database configuration is incomplete. Missing keys: {MissingKeys}. You can set them in the 'database' section of ~/.openrct2/api.config.yml or via env vars: {EnvKeys}.",
                    string.Join(", ", missingDatabaseKeys.Select(x => x.Key)),
                    string.Join(", ", missingDatabaseKeys.Select(x => x.Env)));
            }
            else
            {
                var dbService = serviceProvider.GetService<IDBService>();
                try
                {
#pragma warning disable VSTHRD002
                    dbService.SetupAsync().Wait();
#pragma warning restore VSTHRD002
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occured while setting up the database service");
                }
            }

            // Allow certain domains for AJAX / JSON capability
            app.UseCors(builder => builder.AllowAnyOrigin()
                                          .AllowAnyHeader()
                                          .AllowAnyMethod());

#if _ENABLE_CHAT_
            app.Map("/chat", wsapp => {
                wsapp.UseWebSockets(new WebSocketOptions {
                    ReplaceFeature = true
                });

                wsapp.Use(async (context, next) =>
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        using (var wsSession = new WebSocketSession(serviceProvider, webSocket))
                        {
                            await wsSession.Run();
                        }
                        return;
                    }
                    await next();
                });
            });
#endif

            // Redirect servers.openrct2.website to /servers
            // servers.openrct2.website
            app.Use(async (context, next) =>
            {
                string host = context.Request.Host.Value;
                if (String.Equals(host, "servers.openrct2.io", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(host, "servers.openrct2.website", StringComparison.OrdinalIgnoreCase))
                {
#if REDIRECT_SERVERS_TO_HOME
                    string accept = context.Request.Headers[HeaderNames.Accept];
                    string[] accepts = accept.Split(',');
                    if (accepts.Contains(MimeTypes.ApplicationJson))
                    {
                        context.Request.Path = "/servers";
                    }
                    else
                    {
                        context.Response.Redirect(MainWebsite);
                        return;
                    }
#else
                    context.Request.Path = "/servers";
#endif
                }
                await next();
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Fallback to an empty 404
            app.Run(
                context =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                });
        }
    }
}
