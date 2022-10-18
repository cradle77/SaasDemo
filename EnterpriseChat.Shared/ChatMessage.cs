namespace EnterpriseChat.Shared
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public DateTime TimeStamp { get; set; }

        public string Username { get; set; }

        public string Content { get; set; }

        public int RoomId { get; set; }

        public ChatRoom Room { get; set; }
    }
}
