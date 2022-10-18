using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateNewClient
{
    public class NewClientParameters
    {
        public string Name { get; set; }

        public string ClientId { get; set; }
        
        public string ClientSecret { get; set; }

        public string MetadataUrl { get; set; }
    }
}
