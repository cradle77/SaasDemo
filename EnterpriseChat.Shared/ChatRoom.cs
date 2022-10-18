using System.ComponentModel.DataAnnotations;

namespace EnterpriseChat.Shared
{
    public class ChatRoom
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [MaxLength(10)]
        public string CompanyId { get; set; }
    }
}
