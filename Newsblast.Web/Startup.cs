using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Rest;
using Newsblast.Shared.Data;
using Newsblast.Web.Services;

namespace Newsblast.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new RequireHttpsAttribute());
            });

            var connectionString = Configuration.GetConnectionString("SqlServerConnectionString");
            if (connectionString == null)
            {
                connectionString = Configuration["SqlServerConnectionString"];
            }

            services.AddDbContextPool<NewsblastContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "Discord";
            })
            .AddCookie()
            .AddOAuth("Discord", options =>
            {
                options.ClientId = Configuration["DiscordClientId"];
                options.ClientSecret = Configuration["DiscordClientSecret"];
                options.CallbackPath = new PathString("/signin-discord");

                options.AuthorizationEndpoint = "https://discordapp.com/api/oauth2/authorize";
                options.TokenEndpoint = "https://discordapp.com/api/oauth2/token";

                options.Scope.Add("identify");
                options.Scope.Add("guilds");

                options.SaveTokens = true;

                options.Events = new OAuthEvents
                {
                    OnRemoteFailure = context =>
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        context.HandleResponse();

                        return Task.CompletedTask;
                    },
                    OnCreatingTicket = async context =>
                    {
                        using (var discord = new DiscordRestClient())
                        {
                            try
                            {
                                await discord.LoginAsync(TokenType.Bearer, context.AccessToken);

                                context.Identity.AddClaim(new Claim(ClaimTypes.Sid, discord.CurrentUser.Id.ToString()));
                            }
                            catch (Exception ex)
                            {
                                context.Fail(ex);
                            }
                        }
                    }
                };
            });

            services.AddSingleton<DiscordBotClient>();
            services.AddScoped<DiscordUserClient>();

            services.AddMvc();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var options = new RewriteOptions()
                .AddRedirectToHttpsPermanent();

            app.UseRewriter(options);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStatusCodePagesWithReExecute("/error/{0}");

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
