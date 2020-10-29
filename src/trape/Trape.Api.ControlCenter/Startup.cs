using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using trape.datalayer;
using Trape.Api.ControlCenter;

namespace Trape.Api.ControlCenter
{
    public class Startup
    {
        public Container Container { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Config.SetUp();

            // Configure Logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Config.Current)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Destructure.ToMaximumDepth(4)
                .Destructure.ToMaximumStringLength(100)
                .Destructure.ToMaximumCollectionCount(10)
                .WriteTo.Console(
#if DEBUG
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
#else
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
#endif
                    outputTemplate: Config.GetValue("Serilog:OutputTemplateConsole")
                )
                .WriteTo.File(
                    path: Config.GetValue("Serilog:LogFileLocation"), retainedFileCountLimit: 7, rollingInterval: RollingInterval.Day, buffered: false,
                    outputTemplate: Config.GetValue("Serilog:OutputTemplateFile")
                )
                .CreateLogger();


            var dbContextOptionsBuilder = new DbContextOptionsBuilder<TrapeContext>();
            dbContextOptionsBuilder.UseNpgsql(Config.GetConnectionString("trape-db"));
            dbContextOptionsBuilder.EnableDetailedErrors(false);
            dbContextOptionsBuilder.EnableSensitiveDataLogging(false);

            Container = new Container();
            Container.Options.DefaultLifestyle = new AsyncScopedLifestyle();
            Container.Options.ResolveUnregisteredConcreteTypes = false;

            services.AddLogging();

            services.AddControllers();

            services.AddLocalization();

            services.AddSimpleInjector(Container, options =>
            {
                options.AddAspNetCore();
                options.AddLogging();
                options.AddLocalization();
            });

            Container.Register<TrapeContext, TrapeContext>();
            Container.RegisterInstance(Log.Logger);
            Container.RegisterInstance(Config.Current);
            Container.RegisterInstance(dbContextOptionsBuilder);
            Container.RegisterInstance(dbContextOptionsBuilder.Options);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // UseSimpleInjector() finalizes the integration process.
            app.UseSimpleInjector(Container);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            Container.Verify();
        }
    }
}
