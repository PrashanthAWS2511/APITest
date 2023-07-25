using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OrderAPI.CommonFunctions;
using OrderAPI.Microservices;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace OrderAPI
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
            services.AddControllers();
            //services.AddScoped<IDBLayer, DBLayer>();
            services.AddScoped<IOrderMs, OrderMs>();
            //DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            ProviderName = Convert.ToString(Configuration["TimeOuts:ProviderName"]);
            StatementTimeOut = Convert.ToInt32(Configuration["TimeOuts:StatementTimeOut"]);
            EnableReadUnCommit = Convert.ToBoolean(Configuration["TimeOuts:EnableReadUnCommit"]);
            ConnectionString = Convert.ToString(Configuration["PostGreConnectionStrings:ConnectionStrings"]);

        }

        public static string ProviderName { get; private set; }
        public static int StatementTimeOut { get; private set; }
        public static bool EnableReadUnCommit { get; private set; }
        public static string ConnectionString { get; private set; }
    }
}