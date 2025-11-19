using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using LIS_Middleware.DataDB;
using LIS_Middleware.Middleware;

namespace LIS_Middleware
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
            // 註冊 Supabase Client
            var supabaseUrl = Configuration["Supabase:Url"];
            var supabaseKey = Configuration["Supabase:ApiKey"];
            
            var options = new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false
            };
            
            var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
            supabaseClient.InitializeAsync().Wait();
            
            services.AddSingleton(supabaseClient);
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // 加入 Request/Response 記錄 Middleware
            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
