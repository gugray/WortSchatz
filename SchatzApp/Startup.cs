using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

using SchatzApp.Logic;

namespace SchatzApp
{
    public class Startup
    {
        private readonly IHostingEnvironment env;
        private readonly IConfigurationRoot config;

        public Startup(IHostingEnvironment env)
        {
            this.env = env;
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.devenv.json", optional: true)
                .AddEnvironmentVariables();
            if (Directory.Exists("/etc/wschatz")) builder.AddJsonFile("/etc/wschatz/appsettings.json", optional: true);
            config = builder.Build();
            // If running in production or staging, will log to file. Initialize Serilog here.
            if (!env.IsDevelopment())
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(config["logFileName"])
                    .CreateLogger();
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Application-specific singletons.
            services.AddSingleton(new PageProvider(env.IsDevelopment()));
            services.AddSingleton(new Sampler(config["sampleFileName"]));
            // MVC for serving pages and REST
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            // Log to console (debug) or file (otherwise).
            if (env.IsDevelopment()) loggerFactory.AddConsole();
            else loggerFactory.AddSerilog();
            // Static file options: inject caching info for all static files.
            StaticFileOptions sfo = new StaticFileOptions
            {
                OnPrepareResponse = (context) =>
                {
                    context.Context.Response.Headers["Cache-Control"] = "private, max-age=31536000";
                    context.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddYears(1).ToString("R");
                }
            };
            // Static files (JS, CSS etc.) served directly.
            app.UseStaticFiles(sfo);
            // Serve our (single) .cshtml file, and serve API requests.
            app.UseMvc(routes =>
            {
                routes.MapRoute("api-getpage", "api/getpage/{*paras}", new { controller = "Api", action = "GetPage", paras = "" });
                routes.MapRoute("api-getquiz", "api/getquiz/{*paras}", new { controller = "Api", action = "GetQuiz", paras = "" });
                routes.MapRoute("api-evalquiz", "api/evalquiz/{*paras}", new { controller = "Api", action = "EvalQuiz", paras = "" });
                routes.MapRoute("default", "{*paras}", new { controller = "Index", action = "Index", paras = "" });
            });
        }
    }
}
