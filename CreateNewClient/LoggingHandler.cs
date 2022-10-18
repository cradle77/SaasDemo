using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateNewClient
{
    public class LoggingHandler : DelegatingHandler
    {
        private bool _logRequests;

        public LoggingHandler(HttpMessageHandler innerHandler, bool logRequests)
            : base(innerHandler)
        {
            _logRequests = logRequests;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_logRequests)
            {
                Console.WriteLine("Request:");
                Console.WriteLine(request.ToString());
                if (request.Content != null)
                {
                    Console.WriteLine(await request.Content.ReadAsStringAsync());
                }
                Console.WriteLine();
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (_logRequests)
            {
                Console.WriteLine("Response:");
                Console.WriteLine(response.ToString());
                if (response.Content != null)
                {
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
                Console.WriteLine();
            }

            return response;
        }
    }
}
