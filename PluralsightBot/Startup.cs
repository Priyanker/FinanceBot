// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.3.0

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using FinanceBot.Bots;
using FinanceBot.Dialogs;
using FinanceBot.Services;

namespace FinanceBot
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
            services.AddMemoryCache();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

            // Create the Bot Framework Adapter.
            services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>();

            // Configure Services
            services.AddSingleton<BotServices>();

            //Configure HTTP finance service.
            services.AddSingleton<IFinancialServices,FinancialServices>();
            // Configure State
            ConfigureState(services);

            ConfigureDialogs(services);

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogBot<MainDialog>>();
        }

        public void ConfigureState(IServiceCollection services)
        {
            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.) 
            services.AddSingleton<IStorage, MemoryStorage>();

            //var storageAccount = "";
            //var storageContainer = "";

            //services.AddSingleton<IStorage>(new AzureBlobStorage(storageAccount, storageContainer));


            // Create the User state. 
            services.AddSingleton<UserState>();

            // Create the Conversation state. 
            services.AddSingleton<ConversationState>();

            // Create an instanc of the state service 
            services.AddSingleton<BotStateService>();
        }

        public void ConfigureDialogs(IServiceCollection services)
        {
            services.AddSingleton<MainDialog>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
