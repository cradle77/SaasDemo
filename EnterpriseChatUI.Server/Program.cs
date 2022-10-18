using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;

namespace EnterpriseChatUI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            builder.Services.AddHttpClient("EnterpriseChatApi", client => client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("apiBaseUrl")));

            builder.Services.AddHealthChecks();

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto |
                   ForwardedHeaders.XForwardedHost;

                options.ForwardedHostHeaderName = "X-ORIGINAL-HOST";

                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();

            app.UseForwardedHeaders();
            
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();

                app.Use(async (context, next) => 
                {
                    // if companyId is not set, set it to acme
                    if (string.IsNullOrEmpty(context.Request.Query["companyId"]))
                    {
                        context.Request.QueryString = context.Request.QueryString.Add("companyId", "contoso");
                    }

                    await next();
                });
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseHealthChecks("/health");

            app.UseRouting();

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToController("Index", "Home");

            app.Run();
        }
    }
}