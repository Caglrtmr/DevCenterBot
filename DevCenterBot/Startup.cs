// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.22.0

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace DevCenterBot
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
            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, Bots.EchoBot>();

            services.Configure<BotSettings>(botSettings =>
            {
                //botSettings.AccessCacheExpiryInDays = Convert.ToInt32(this.Configuration["AccessCacheExpiryInDays"]);
                //botSettings.AppBaseUri = this.Configuration["AppBaseUri"];
                //botSettings.ExpertAppId = this.Configuration["ExpertAppId"];
                //botSettings.ExpertAppPassword = this.Configuration["ExpertAppPassword"];
                //botSettings.UserAppId = this.Configuration["UserAppId"];
                //botSettings.UserAppPassword = this.Configuration["UserAppPassword"];
                //botSettings.TenantId = this.Configuration["TenantId"];

                botSettings.AOAI_ENDPOINT = (this.Configuration["AOAI_ENDPOINT"]);
                botSettings.AOAI_KEY = this.Configuration["AOAI_KEY"];
                botSettings.AOAI_DEPLOYMENTID = this.Configuration["AOAI_DEPLOYMENTID"];
                botSettings.SEARCH_INDEX_NAME = this.Configuration["SEARCH_INDEX_NAME"];
                botSettings.SEARCH_SERVICE_NAME = this.Configuration["SEARCH_SERVICE_NAME"];
                botSettings.SEARCH_QUERY_KEY = this.Configuration["SEARCH_QUERY_KEY"];


                botSettings.SettingForPrompt = this.Configuration["SettingForPrompt"];
                botSettings.SettingForTemperature = this.Configuration["SettingForTemperature"];
                botSettings.SettingForMaxToken = this.Configuration["SettingForMaxToken"];
                botSettings.SettingForTopK = this.Configuration["SettingForTopK"];

            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
