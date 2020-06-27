using System;
using Hangfire;
using Hangfire.SqlServer;
using MassTransit;
using MassTransit.HangfireIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Scheduler.Service
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddHangfire(configuration => configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(
                        Configuration.GetConnectionString("scheduler"),
                        new SqlServerStorageOptions
                        {
                            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                            QueuePollInterval = TimeSpan.Zero,
                            UseRecommendedIsolationLevel = true,
                            DisableGlobalLocks = true,
                            PrepareSchemaIfNecessary = true
                        }));

            services.AddSingleton<IHangfireComponentResolver, ServiceProviderHangfireComponentResolver>();
            
            services.AddHangfireServer();
            
            services.AddMassTransit(configurator =>
            {
                configurator.AddBus(context => Bus.Factory.CreateUsingRabbitMq(rabbit =>
                {
                    rabbit.Host(Configuration.GetValue<string>("RabbitMqHost"));
                    rabbit.UseHangfireScheduler(
                        context.Container.GetRequiredService<IHangfireComponentResolver>(),
                        "scheduler");
                }));
            });
            services.AddHostedService<BusService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHangfireDashboard();
            
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                
            });
        }
    }
}