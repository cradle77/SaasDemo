using EnterpriseChat.Shared;
using EnterpriseChatApi.Services;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseChatApi.Data
{
    public class ChatContext : DbContext
    {
        private ICompanyContextAccessor _companyContextAccessor;

        public ChatContext(DbContextOptions<ChatContext> options, ICompanyContextAccessor companyContextAccessor) : base(options)
        {
            _companyContextAccessor = companyContextAccessor;
        }

        public DbSet<ChatRoom> Rooms { get; set; }
        public DbSet<ChatMessage> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.AddInterceptors(new SessionCommandInterceptor(_companyContextAccessor));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<ChatRoom>().HasQueryFilter(r => r.CompanyId == _companyContextAccessor.CompanyContext.CompanyId);
            modelBuilder.Entity<ChatMessage>().HasOne(x => x.Room).WithMany().HasForeignKey(m => m.RoomId);

            //modelBuilder.Entity<ChatMessage>().HasQueryFilter(m => m.Room.CompanyId == _companyContextAccessor.CompanyContext.CompanyId);
        }
    }
}
