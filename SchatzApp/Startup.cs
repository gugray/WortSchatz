using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Serilog;

using SchatzApp.Logic;
using Countries;

namespace SchatzApp
{
    public class Startup
    {
        private readonly IHostEnvironment env;
        private readonly ILoggerFactory loggerFactory;
        private readonly IConfigurationRoot config;

        private ResultRepo resultRepo = null;

        public Startup(IHostEnvironment env, ILoggerFactory loggerFactory)
        {
            this.env = env;
            this.loggerFactory = loggerFactory;

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
                var seriConf = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(config["logFileName"]);
                if (config["logLevel"] == "Trace") seriConf.MinimumLevel.Verbose();
                else if (config["logLevel"] == "Debug") seriConf.MinimumLevel.Debug();
                else if (config["logLevel"] == "Information") seriConf.MinimumLevel.Information();
                else if (config["logLevel"] == "Warning") seriConf.MinimumLevel.Warning();
                else if (config["logLevel"] == "Error") seriConf.MinimumLevel.Error();
                else seriConf.MinimumLevel.Fatal();
                Log.Logger = seriConf.CreateLogger();
            }
            loggerFactory.AddSerilog();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (env.IsDevelopment())
            {
                services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            }

            // Application-specific singletons.
            services.AddSingleton(new CountryResolver());
            services.AddSingleton(new PageProvider(loggerFactory, env.IsDevelopment()));
            services.AddSingleton(new Sampler(loggerFactory, config["sampleFileName"]));
            resultRepo = new ResultRepo(loggerFactory, config["dbFileName"]);
            services.AddSingleton(resultRepo);
            // MVC for serving pages and REST
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            }).AddRazorRuntimeCompilation();
            // Configuration singleton
            services.AddSingleton<IConfiguration>(sp => { return config; });
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLife)
        {
            // Sign up to application shutdown so we can do proper cleanup
            appLife.ApplicationStopping.Register(onApplicationStopping);
            // Static file options: inject caching info for all static files.
            StaticFileOptions sfo = new StaticFileOptions
            {
                OnPrepareResponse = (context) =>
                {
                    // For all stuff coming from "/static/**", add directly to cache indefinitely.
                    if (context.Context.Request.Path.Value.StartsWith("/static/"))
                    {
                        context.Context.Response.Headers["Cache-Control"] = "private, max-age=31536000";
                        context.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddYears(1).ToString("R");
                    }
                    // For everything coming from "/files/**", disable caching
                    else if (context.Context.Request.Path.Value.StartsWith("/files/"))
                    {
                        context.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                        context.Context.Response.Headers["Pragma"] = "no-cache";
                        context.Context.Response.Headers["Expires"] = "0";
                    }
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
                routes.MapRoute("api-getscore", "api/getscore/{*paras}", new { controller = "Api", action = "GetScore", paras = "" });
                routes.MapRoute("api-export", "api/export/{*paras}", new { controller = "Api", action = "Export", paras = "" });
                routes.MapRoute("default", "{*paras}", new { controller = "Index", action = "Index", paras = "" });
            });
        }

        private void onApplicationStopping()
        {
            if (resultRepo != null) resultRepo.Dispose();
        }
    }
}
