using EnterpriseChatUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Web;

namespace EnterpriseChatUI.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // get the current Url
            var uriHelper = builder.Services.BuildServiceProvider().GetRequiredService<NavigationManager>();
            var currentUrl = uriHelper.ToAbsoluteUri(uriHelper.Uri);

            // add configuration from api call
            var httpClient = new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

            var apiPath = $"Configuration";

            builder.Configuration.AddJsonStream(await httpClient.GetStreamAsync(apiPath));

            var companyId = builder.Configuration.GetValue<string>("companyId");
            
            // configure api HttpClient
            var enterpriseChatApiUrl = builder.Configuration.GetValue<string>("EnterpriseChatApiUrl");
#if DEBUG
            enterpriseChatApiUrl += $"{companyId}/";
#endif

            builder.Services.AddHttpClient("EnterpriseChatApi", client => client.BaseAddress = new Uri(enterpriseChatApiUrl))
                .AddHttpMessageHandler(sp => 
                {
                    var handler = sp.GetRequiredService<AuthorizationMessageHandler>()
                        .ConfigureHandler(
                            authorizedUrls: new[] { enterpriseChatApiUrl },
                            scopes: new[] { "https://saasdemo22.onmicrosoft.com/saas-demo-api/chat.readwrite" });

                    return handler;
                });
            
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("EnterpriseChatApi"));

            builder.Services.AddMsalAuthentication(options =>
            {
                builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
                options.ProviderOptions.DefaultAccessTokenScopes.Add("https://saasdemo22.onmicrosoft.com/saas-demo-api/chat.readwrite");
            });
            
            builder.Services.AddScoped<ChatService>();

            await builder.Build().RunAsync();
        }
    }
}