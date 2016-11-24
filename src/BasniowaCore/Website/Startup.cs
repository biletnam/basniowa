﻿using System;
using System.IO;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.ResolveAnything;
using AutoMapper;
using Common.Cqrs;
using Common.Startup;
using DataAccess;
using DataAccess.UniqueId;
using Logic.Services;
using Logic.Shows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Website.Infrastructure;

namespace Website
{
    /// <summary>
    /// Startup class.
    /// </summary>
    public class Startup
    {
        private static readonly string TheaterDbConnectionStringKey = "TheaterDb";

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="env">The env.</param>
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>Service provider.</returns>
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddDataProtection();
            services.AddOptions();
            services.AddMvc();
            services.AddSwaggerGen();
            services.ConfigureSwaggerGen(options =>
            {
                var currentAssemblyPath = typeof(Startup).GetTypeInfo().Assembly.Location;
                var xmlCommentsPath = Path.ChangeExtension(currentAssemblyPath, "xml");
                
                options.IncludeXmlComments(xmlCommentsPath);
                options.DescribeAllEnumsAsStrings();

                options.OperationFilter<AuthorizationHeaderParameterOperationFilter>();
            });

            services.AddJwtBearerAuthentication(Configuration);

            services.AddMemoryCache();
            services.AddLogging();
            services.AddSingleton(GetDbOptions);
            services.AddTransient<TheaterDb>();

            var builder = new ContainerBuilder();
            builder.Populate(services);
            
            builder.RegisterType<DbContextFactory>().As<IDbContextFactory>().SingleInstance();
            RegisterUniqueIdProvider(builder);

            builder.RegisterType<ServiceProviderHandlerResolver>().As<IHandlerResolver>().SingleInstance();
            builder.RegisterType<SynchronousMessageBus>()
                .As<ICommandPublisher>()
                .As<IEventPublisher>()
                .SingleInstance();

            builder
                .Register(c => new BufferingUniqueIdService(c.Resolve<IUniqueIdProvider>(), 10))
                .As<IUniqueIdService>()
                .SingleInstance();
            builder.RegisterType<DateTimeService>().As<IDateTimeService>().SingleInstance();
            builder.RegisterType<ShowsReader>().As<IShowsReader>().InstancePerDependency();

            builder.RegisterAssemblyTypes(Assembly.Load(new AssemblyName("Logic")))
                .AsClosedTypesOf(typeof(IHandler<>))
                .PropertiesAutowired();

            builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());

            var container = builder.Build();
            return new AutofacServiceProvider(container);
        }

        private DbContextOptions<TheaterDb> GetDbOptions(IServiceProvider serviceProvider)
        {
            var builder = new DbContextOptionsBuilder<TheaterDb>();
            builder.UseMemoryCache(serviceProvider.GetService<IMemoryCache>())
                .UseLoggerFactory(serviceProvider.GetService<ILoggerFactory>());
            ConfigureDbOptions(builder);
            return builder.Options;
        }

        private void ConfigureMapper(IMapperConfigurationExpression cfg)
        {
            Initializer.Init<MapperStartupAttribute>(cfg);
        }

        /// <summary>
        /// Configures the database options.
        /// </summary>
        /// <param name="options">The options.</param>
        protected virtual void ConfigureDbOptions(DbContextOptionsBuilder<TheaterDb> options)
        {
            var connectionString = Configuration.GetConnectionString(TheaterDbConnectionStringKey);
            options.UseSqlServer(connectionString);
        }

        /// <summary>
        /// Registers the unique identifier provider.
        /// </summary>
        /// <param name="builder">The builder.</param>
        protected virtual void RegisterUniqueIdProvider(ContainerBuilder builder)
        {
            var connectionString = Configuration.GetConnectionString(TheaterDbConnectionStringKey);
            builder
                .Register(c => new SqlUniqueIdProvider(connectionString))
                .As<IUniqueIdProvider>()
                .SingleInstance();
        }

        /// <summary>
        /// Configures the application.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="env">The env.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.ConfigureJwtBearerAuthentication();
            app.UseMvc();
            app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = new[] { "index.htm" } });
            app.UseStaticFiles();
            
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUi();
            }

            Mapper.Initialize(ConfigureMapper);
        }
    }
}
