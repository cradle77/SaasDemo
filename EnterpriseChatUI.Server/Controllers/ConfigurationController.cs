using Azure.Data.Tables;
using Azure.Identity;
using EnterpriseChatUI.Server.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseChatUI.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private IConfiguration _configuration;

        public ConfigurationController(IConfiguration configuration, IServer server)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string companyId)
        {
            var baseUrl = _configuration.GetValue<string>("apiBaseUrl") ??
                $"{this.HttpContext.Request.Scheme}://{this.HttpContext.Request.Host}";

            // read additional settings from table storage
            var serviceClient = new TableServiceClient(
                new Uri(_configuration.GetValue<string>("storageUrl")),
                new DefaultAzureCredential()
                );

            var tableClient = serviceClient.GetTableClient("Clients");

            var entity = await tableClient.GetEntityAsync<ClientEntity>("0", companyId);

            var result = new
            {
                CompanyId = companyId,
                EnterpriseChatApiUrl = $"{baseUrl}/api/",
                AzureAdB2C = new
                {
                    Authority = entity.Value.Authority,
                    ClientId = entity.Value.ClientId,
                    ValidateAuthority = false
                }
            };

            return Ok(result);
        }

        [HttpGet("headers")]
        public IActionResult Headers()
        {
            return this.Ok(this.Request.Headers);
        }
    }
}
