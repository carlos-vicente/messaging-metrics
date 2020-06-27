using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using WebApi.Messaging;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            services.AddMassTransit(configurator =>
            {
                configurator.AddConsumer<NotificationConsumer>();
                
                configurator.AddBus(context => Bus.Factory.CreateUsingRabbitMq(rabbit =>
                {
                    var host = rabbit.Host(Configuration.GetValue<string>("RabbitMqHost"));
                    rabbit.UseMessageScheduler(new Uri(host.Address, "scheduler"));
                    
                    rabbit.ReceiveEndpoint("web_notifications", endpointConfigurator =>
                    {
                        endpointConfigurator.Durable = false;
                        endpointConfigurator.QueueExpiration = TimeSpan.FromSeconds(10);
                        
                        endpointConfigurator.UseScheduledRedelivery(retryConfigurator =>
                        {
                            retryConfigurator.Handle<CanNotCurrentlyProcessException>();
                            retryConfigurator.Interval(2, TimeSpan.FromSeconds(30));
                        });
                        
                        endpointConfigurator.ConfigureConsumer<NotificationConsumer>(context);

                        EndpointConvention.Map<NotifySomethingHappened>(endpointConfigurator.InputAddress);
                    });
                }));
            });

            services.AddHostedService<BusService>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Masstransit testing framework",
                    Version = "v1"
                });
                //c.DocInclusionPredicate((_, api) => !string.IsNullOrWhiteSpace(api.GroupName));
                //c.TagActionsBy(api => new[]{api.GroupName});

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            
            app.UseSwagger();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}