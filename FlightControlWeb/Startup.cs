using System.Collections.Concurrent;
using FlightControlWeb.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlightControlWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // Adds contorllers and uses dependency injection for manager(model) and it's dictionaries.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<ConcurrentDictionary<string, FlightPlan>>();
            services.AddSingleton<ConcurrentDictionary<string, Server>>();
            services.AddSingleton<ConcurrentDictionary<string, string>>();
            services.AddSingleton<IFlightsManager, FlightsManager>();

        }

        // Configures the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
