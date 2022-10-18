using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using CreateNewClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Directory = System.IO.Directory;

var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

var configuration = builder.Build();

var newClient = new NewClientParameters
{
    Name = "fabrikam",
    ClientId = configuration["auth0:clientId"],
    ClientSecret = configuration["auth0:clientSecret"],
    MetadataUrl = configuration["auth0:metadataUrl"]
};

await Create(newClient);

//await CleanUp(newClient.Name);

async Task Create(NewClientParameters newClient, bool logRequests = false)
{
    Console.WriteLine("Acquiring token...");
    
    var credentials = new ClientSecretCredential(
        configuration["tenantId"],
        configuration["clientId"],
        configuration["clientSecret"]);

    var token = await credentials.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

    var client = new HttpClient(new LoggingHandler(new HttpClientHandler(), logRequests))
    {
        DefaultRequestHeaders =
        {
            Authorization = new AuthenticationHeaderValue("Bearer", token.Token)
        }
    };

    Console.WriteLine("Inspecting IDP's metadata...");
    // let's retrieve the issuer from the metadata url
    var metadata = await new HttpClient().GetStringAsync(newClient.MetadataUrl);

    var issuer = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata)["issuer"].ToString();

    var graphClient = new GraphServiceClient(client);

    Console.WriteLine("Retrieving API Connector");
    var apiConnector = (await graphClient.Identity.ApiConnectors
        .GetAsync(x => x.QueryParameters.Filter = $"displayName eq 'Token Enrich'"))
        .Value
        .Single();

    Console.WriteLine("Creating new Identity Provider in B2C...");
    // create new identity provider
    var newProvider = new OpenIdConnectIdentityProvider
    {
        DisplayName = $"{newClient.Name}Idp",
        ClientId = newClient.ClientId,
        ClientSecret = newClient.ClientSecret,
        MetadataUrl = newClient.MetadataUrl,
        Scope = "openid profile email",
        ResponseType = OpenIdConnectResponseTypes.Code,
        ResponseMode = OpenIdConnectResponseMode.Form_post,
        ClaimsMapping = new ClaimsMapping()
        {
            UserId = "sub",
            DisplayName = "name",
            Email = "email"
        }
    };

    var result = await graphClient.Identity.IdentityProviders.PostAsync(newProvider);

    Console.WriteLine("Creating new UserFlow in B2C...");

    // create the new user flow
    var newUserFlow = new B2cIdentityUserFlow
    {
        UserFlowType = UserFlowType.SignUpOrSignIn,
        IdentityProviders = new List<IdentityProvider>()
    {
        new IdentityProvider()
        {
            Id = result.Id,
            Type = "OpenIdConnect",
            ClientId = newProvider.ClientId,
            ClientSecret = newProvider.ClientSecret,
            Name = newProvider.DisplayName,
            OdataType = "#microsoft.graph.identityProvider",
        },
    },
        ApiConnectorConfiguration = new UserFlowApiConnectorConfiguration()
        {
            PreTokenIssuance = new IdentityApiConnector()
            {
                Id = apiConnector.Id
            }
        },
        UserFlowTypeVersion = 3,
        Id = newClient.Name
    };

    var result2 = await graphClient.Identity.B2cUserFlows.PostAsync(newUserFlow);

    Console.WriteLine("Updating UI Application's redirect URLs...");

    var application = (await graphClient.Applications.GetAsync(x => x.QueryParameters.Filter = $"displayName eq 'Saas-demo'"))
        .Value
        .Single();

    application.Spa.RedirectUris.Add($"https://{newClient.Name}.enterprisechat.co.uk/authentication/login-callback");

    var response = await client.PatchAsync("https://graph.microsoft.com/beta/applications/" + application.Id,
        new StringContent(JsonSerializer.Serialize(new
        {
            spa = new
            {
                redirectUris = application.Spa.RedirectUris
            }
        }), Encoding.UTF8, "application/json"));
    response.EnsureSuccessStatusCode();

    Console.WriteLine("Creating client's entry in table storage...");

    var serviceClient = new TableServiceClient(
        new Uri(configuration["storageUrl"]),
        new DefaultAzureCredential());

    var tableClient = serviceClient.GetTableClient("Clients");

    var entity = new ClientEntity()
    {
        PartitionKey = "0",
        RowKey = newClient.Name,
        Authority = $"https://saasdemo22.b2clogin.com/saasdemo22.onmicrosoft.com/B2C_1_{newClient.Name}",
        Issuer = issuer,
        ClientId = application.AppId
    };

    tableClient.AddEntity(entity);

    Console.WriteLine("Job done! Enjoy at ");
    Console.WriteLine($"https://{newClient.Name}.enterprisechat.co.uk");

    Console.WriteLine("*** don't forget to add the CompanyId claim in the portal ***");
    Console.WriteLine("https://portal.azure.com/0206e18a-a81b-4311-98c4-c19feef0e9f5#blade/Microsoft_AAD_B2CAdmin/TenantManagementMenuBlade/overview");
    Console.WriteLine($"https://portal.azure.com/#view/Microsoft_AAD_B2CAdmin/ManageUserJourneyMenuBlade/~/overview-item/tenantId/saasdemo22.onmicrosoft.com/userJourneyType/B2CSignUpOrSignInWithPassword_V3/userJourneyId/B2C_1_{newClient.Name}/enableApiConnectors~/true/isB2CTenant~/true/isCiamTenant~/false");
}

async Task CleanUp(string companyId, bool logRequests = false)
{
    Console.WriteLine("Acquiring token...");

    var credentials = new ClientSecretCredential(
        configuration["tenantId"],
        configuration["clientId"],
        configuration["clientSecret"]);

    var token = await credentials.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

    var client = new HttpClient(new LoggingHandler(new HttpClientHandler(), logRequests))
    {
        DefaultRequestHeaders =
        {
            Authorization = new AuthenticationHeaderValue("Bearer", token.Token)
        }
    };

    var graphClient = new GraphServiceClient(client);

    Console.WriteLine("Removing UserFlow in B2C...");

    await graphClient.Identity.B2cUserFlows[$"B2C_1_{companyId}"].DeleteAsync();

    Console.WriteLine("Removing Identity Provider in B2C...");
    
    var identityProvider = (await graphClient.Identity.IdentityProviders
        .GetAsync(x => x.QueryParameters.Filter = $"displayName eq '{companyId}Idp'"))
        .Value
        .Single();

    await graphClient.Identity.IdentityProviders[identityProvider.Id].DeleteAsync();

    Console.WriteLine("Removing Redirect URL from Application...");

    var application = (await graphClient.Applications.GetAsync(x => x.QueryParameters.Filter = $"displayName eq 'Saas-demo'"))
        .Value
        .Single();

    application.Spa.RedirectUris.Remove($"https://{companyId}.enterprisechat.co.uk/authentication/login-callback");

    var response = await client.PatchAsync("https://graph.microsoft.com/beta/applications/" + application.Id,
        new StringContent(JsonSerializer.Serialize(new
        {
            spa = new
            {
                redirectUris = application.Spa.RedirectUris
            }
        }), Encoding.UTF8, "application/json"));
    response.EnsureSuccessStatusCode();

    Console.WriteLine("Removing Client entry from Table Storage...");
    
    var serviceClient = new TableServiceClient(
        new Uri(configuration["storageUrl"]),
        new DefaultAzureCredential());

    var tableClient = serviceClient.GetTableClient("Clients");

    tableClient.DeleteEntity("0", companyId);

    Console.WriteLine($"Job done, client {companyId} removed!");
}