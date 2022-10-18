using Azure.Data.Tables;
using Azure.Identity;
using EnterpriseChatUI.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace EnterpriseChatUI.Server.Controllers
{
    public class HomeController : Controller
    {
        private IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string companyId)
        {
            var serviceClient = new TableServiceClient(
                new Uri(_configuration.GetValue<string>("storageUrl")),
                new DefaultAzureCredential()
                );

            var tableClient = serviceClient.GetTableClient("Clients");

            var entity = tableClient.Query<ClientEntity>(x => x.RowKey == companyId && x.PartitionKey == "0");

            if (entity.Count() == 0)
            {
                return NotFound();
            }

            return View();
        }
    }
}
