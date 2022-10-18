using Azure;
using Azure.Data.Tables;

namespace EnterpriseChatUI.Server.Models
{
    public class ClientEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        
        public string RowKey { get; set; }

        public string Authority { get; set; }

        public string Issuer { get; set; }

        public string ClientId { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        
        public ETag ETag { get; set; }
    }
}
