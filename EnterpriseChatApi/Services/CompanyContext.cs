namespace EnterpriseChatApi.Services
{
    public class CompanyContext
    {
        public string CompanyId { get; set; }
    }

    public interface ICompanyContextAccessor
    {
        CompanyContext CompanyContext { get; }
    }

    internal class RoutingCompanyContextAccessor : ICompanyContextAccessor
    {
        private IHttpContextAccessor _httpContextAccessor;

        public RoutingCompanyContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public CompanyContext CompanyContext
        {
            get
            {
                var context = new CompanyContext();
                var routeData = _httpContextAccessor.HttpContext.GetRouteData();
                if (routeData.Values.ContainsKey("companyId"))
                {
                    context.CompanyId = routeData.Values["companyId"].ToString();
                }
                return context;
            }
        }
    }

    internal class ClaimsCompanyContextAccessor : ICompanyContextAccessor
    {
        private IHttpContextAccessor _httpContextAccessor;

        public ClaimsCompanyContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public CompanyContext CompanyContext
        {
            get
            {
                var context = new CompanyContext();
                var claims = _httpContextAccessor.HttpContext.User.Claims;
                if (claims.Any(c => c.Type == "extension_CompanyId"))
                {
                    context.CompanyId = claims.First(c => c.Type == "extension_CompanyId").Value;
                }
                return context;
            }
        }
    }
}
