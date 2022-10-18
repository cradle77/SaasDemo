using EnterpriseChatApi.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data;
using System.Data.Common;

namespace EnterpriseChatApi.Data
{
    public class SessionCommandInterceptor : DbCommandInterceptor
    {
        private ICompanyContextAccessor _companyContextAccessor;

        public SessionCommandInterceptor(ICompanyContextAccessor companyContextAccessor)
        {
            _companyContextAccessor = companyContextAccessor;
        }

        private void AddSessionContext(DbCommand command)
        {
            command.CommandText = "EXEC sp_set_session_context @key=N'CompanyId', @value=@CompanyId; " + command.CommandText;

            Console.WriteLine("result is: " + command.CommandText);

            DbParameter param = command.CreateParameter();
            param.ParameterName = "@CompanyId";
            param.Value = _companyContextAccessor.CompanyContext.CompanyId;
            command.Parameters.Add(param);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            this.AddSessionContext(command);
            
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            this.AddSessionContext(command);
            
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            this.AddSessionContext(command);

            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    public class SessionContextInterceptor : DbConnectionInterceptor
    {
        private ICompanyContextAccessor _companyContextAccessor;
        private bool _contextSet;

        public SessionContextInterceptor(ICompanyContextAccessor companyContextAccessor)
        {
            _companyContextAccessor = companyContextAccessor;
        }
        
        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
            
            if (string.IsNullOrEmpty(_companyContextAccessor.CompanyContext.CompanyId) || _contextSet)
            {
                return;
            }

            Console.WriteLine("Set context");

            using (DbCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = "EXEC sp_set_session_context @key=N'CompanyId', @value=@CompanyId";
                DbParameter param = cmd.CreateParameter();
                param.ParameterName = "@CompanyId";
                param.Value = _companyContextAccessor.CompanyContext.CompanyId;
                cmd.Parameters.Add(param);
                await cmd.ExecuteNonQueryAsync();
            }

            _contextSet = true;
        }

        public override Task ConnectionClosedAsync(DbConnection connection, ConnectionEndEventData eventData)
        {
            return base.ConnectionClosedAsync(connection, eventData);

            Console.WriteLine("Connection closed");

            _contextSet = false;
        }
    }
}
