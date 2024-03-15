using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Repositories;
using Service.DInspect.Services;
using Service.DInspect.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Service.DInspect
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private const string serviceName = "The DInspect Service APIs";
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var tempFolder = Path.GetTempPath();
            Trace.WriteLine($"Temp folder: {tempFolder}");

            services.AddOptions();
            services.AddMemoryCache();

            services.AddMvc().AddNewtonsoftJson();
            services.AddCors();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.Configure<MySetting>(Configuration.GetSection("dinspect"));

            services.TryAddSingleton<IConnectionFactory, ConnectionFactory>();
            //services.AddScoped<IServiceWrapper, ServiceWrapper>();
            services.AddTransient<IServiceWrapper, ServiceWrapper>();

            //IConfigurationSection sec = Configuration.GetSection("dinspect");
            //services.Configure<AzureConfiguration>(options => sec.Bind(options));

            //services.AddTransient<IConnectionFactory, ConnectionFactory>();
            //services.AddScoped<IServiceWrapper, ServiceWrapper>();

            //            if (Configuration.GetValue<string>("dinspect:HostName").Contains("localhost"))
            //            {
            //#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
            //                ConfigurationOptions = services.BuildServiceProvider().GetService<IOptions<AzureConfiguration>>();
            //#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'

            //                //var data = new ConfigurationService(ConfigurationOptions);
            //                var connectionString = this.Configuration.GetValue<string>("dinspect:ConnectionStrings:CosmosConnection");

            //                services.AddAuthentication(options =>
            //                {
            //                    options.DefaultScheme = AzureADDefaults.AuthenticationScheme;
            //                }).AddJwtBearer("AzureAD", options =>
            //                {
            //                    options.Audience = Configuration.GetValue<string>("dinspect:AzureAd:Audience");
            //                    options.Authority = Configuration.GetValue<string>("dinspect:AzureAd:Instance") + Configuration.GetValue<string>("dinspect:AzureAd:TenantId");
            //                    options.TokenValidationParameters = new TokenValidationParameters()
            //                    {
            //                        ValidIssuer = Configuration.GetValue<string>("dinspect:AzureAd:Issuer"),
            //                        ValidAudience = Configuration.GetValue<string>("dinspect:AzureAd:Audience")
            //                    };
            //                });

            //                SwaggerConfig.AddSwaggerGenWithOAuth(services,
            //                    Configuration.GetValue<string>("dinspect:Version"),
            //                    Configuration.GetValue<string>("dinspect:HostName"),
            //                    string.Empty,
            //                    Configuration.GetValue<string>("dinspect:AzureAd:TenantId"),
            //                    serviceName);

            //                services.AddStandardLogging(
            //                    Configuration.GetValue<string>("dinspect:ApplicationInsights:InstrumentationKey"),
            //                    Configuration.GetValue<string>("Logging:LogLevel:Default"),
            //                    tempFolder);
            //            }
            //            else
            //            {
            //var connectionString = this.Configuration.GetValue<string>("dinspect:ConnectionStrings:CosmosConnection");

            //services.AddAuthentication(options =>
            //{
            //    options.DefaultScheme = AzureADDefaults.AuthenticationScheme;
            //}).AddJwtBearer("AzureAD", options =>
            //{
            //    options.Authority = Configuration.GetValue<string>("dinspect:AzureAd:Instance") + Configuration.GetValue<string>("dinspect:AzureAd:TenantId");
            //    options.TokenValidationParameters = new TokenValidationParameters()
            //    {
            //        ValidateIssuer = true,
            //        ValidAudiences = new List<string> {
            //            Configuration.GetValue<string>("dinspect:AzureAd:ClientId")
            //        }
            //    };
            //});

            //SwaggerConfig.AddSwaggerGenWithOAuth(services,
            //   Configuration.GetValue<string>("dinspect:Version"),
            //   Configuration.GetValue<string>("dinspect:HostName"),
            //   Configuration.GetValue<string>("dinspect:SwaggerBasePath"),
            //   Configuration.GetValue<string>("dinspect:AzureAd:TenantId"),
            //   serviceName);


            //services.AddStandardLogging(
            //    Configuration.GetValueFromHierarchy<string>("dinspect:ApplicationInsights:InstrumentationKey"),
            //    Configuration.GetValue<string>("Logging:LogLevel:Default"),
            //    tempFolder);
            //}


            //services.AddScoped<IFormFileValidator>(h => new FormFileValidator(Configuration.GetValue<int>("AzureStorage:MaxFileSizeInMb")));
            //services.AddScoped<IBlobStorageRepository>(sp => new BlobStorageRepository(
            //    Configuration.GetValue<string>("AzureStorage:ConnectionStrings"),
            //    Configuration.GetValue<string>("AzureStorage:ContainerDInspect"),
            //    sp.GetService<IMemoryCache>(),
            //    sp.GetService<ILogger<BlobStorageRepository>>()));

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Service.DInspect", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Service.DInspect v1"));
            }

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