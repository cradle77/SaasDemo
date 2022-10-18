using Azure.Identity;
using EnterpriseChatApi.Data;
using EnterpriseChatApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;

namespace EnterpriseChatApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddAzureKeyVault(
                new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
                new DefaultAzureCredential()
            );

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
            //builder.Services.AddSingleton<ICompanyContextAccessor, RoutingCompanyContextAccessor>();
            builder.Services.AddSingleton<ICompanyContextAccessor, ClaimsCompanyContextAccessor>();
            builder.Services.AddSingleton<RoutingCompanyContextAccessor, RoutingCompanyContextAccessor>();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(options =>
                {
                    builder.Configuration.Bind("AzureAdB2C", options);

                    options.TokenValidationParameters.NameClaimType = "name";
                },
                options => { builder.Configuration.Bind("AzureAdB2C", options); });

            builder.Services.AddOptions<AuthorizationOptions>()
                .Configure<IServiceProvider>((options, sp) => 
                {
                    options.AddPolicy("CompanyMustMatch",
                    policy =>
                    {
                        policy
                            .RequireAuthenticatedUser()
                            .RequireAssertion(context =>
                            {
                                var routingAccessor = sp.GetRequiredService<RoutingCompanyContextAccessor>();
                                var currentCompanyAccessor = sp.GetRequiredService<ICompanyContextAccessor>();
                                
                                var routingCompany = routingAccessor.CompanyContext.CompanyId;
                                var currentCompany = currentCompanyAccessor.CompanyContext.CompanyId;
                                return string.Equals(routingCompany, currentCompany, StringComparison.InvariantCultureIgnoreCase);
                            });
                    });
                });
            
            builder.Services.AddAuthorization();

            builder.Services.AddDbContext<ChatContext>(options =>
                options.UseSqlServer(builder.Configuration.GetValue<string>("chat-sqlconnectionstring")));

            builder.Services.AddCors();

            builder.Services.AddHealthChecks();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin();
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
            });

            app.UseHealthChecks("/health");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}