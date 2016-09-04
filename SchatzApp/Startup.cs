using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SchatzApp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            // Static file options: inject caching info for all static files.
            StaticFileOptions sfo = new StaticFileOptions
            {
                OnPrepareResponse = (context) =>
                {
                    context.Context.Response.Headers["Cache-Control"] = "private, max-age=31536000";
                    context.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddYears(1).ToString("R");
                }
            };
            app.UseStaticFiles(sfo);

            // Serve our (single) .cshtml file, and serve API requests.
            app.UseMvc(routes =>
            {
                routes.MapRoute("api-getgammaquiz", "api/getgammaquiz/{*paras}", new { controller = "Api", action = "GetGammaQuiz", paras = "" });
                routes.MapRoute("api-evalquiz", "api/evalquiz/{*paras}", new { controller = "Api", action = "EvalQuiz", paras = "" });
                routes.MapRoute("default", "{*paras}", new { controller = "App", action = "Index", paras = "" });
            });
        }
    }
}
