using EnterpriseChat.Shared;
using EnterpriseChat.Shared.ViewModels;
using EnterpriseChatApi.Data;
using EnterpriseChatApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseChatApi.Controllers
{
    [Route("api/{companyId}/[controller]")]
    [ApiController]
    [Authorize("CompanyMustMatch")]
    public class RoomsController : ControllerBase
    {
        private ICompanyContextAccessor _companyContextAccessor;
        private ChatContext _chatContext;

        public RoomsController(ICompanyContextAccessor companyContextAccessor, ChatContext chatContext)
        {
            _companyContextAccessor = companyContextAccessor;
            _chatContext = chatContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var rooms = await _chatContext.Rooms
                //.Where(r => r.CompanyId == _companyContextAccessor.CompanyContext.CompanyId)
                .ToListAsync();
            return Ok(rooms);
        }

        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetById(int roomId)
        {
            var room = await _chatContext.Rooms
                .SingleOrDefaultAsync(x => x.Id == roomId);

            if (room == null)
            {
                return NotFound();
            }
            
            return Ok(room);
        }
        

        [HttpGet("messages")]
        public async Task<IActionResult> GetAllMessagesAsync()
        {
            var messages = await _chatContext.Messages
                //.Where(m => m.Room.CompanyId == _companyContextAccessor.CompanyContext.CompanyId)
                .ToListAsync();
            return Ok(messages);
        }

        [HttpGet("{roomId:int}/messages")]
        public async Task<IActionResult> GetMessagesAsync(int roomId)
        {
            // check if room exists
            if (!await _chatContext.Rooms.AnyAsync(r => r.Id == roomId))
            {
                return NotFound();
            }

            var messages = await _chatContext.Messages
                //.Where(m => m.Room.CompanyId == _companyContextAccessor.CompanyContext.CompanyId)
                .Where(m => m.RoomId == roomId)
                .OrderBy(m => m.TimeStamp)
                .ToListAsync();
            return Ok(messages);
        }

        [HttpPost("{roomId:int}/messages")]
        public async Task<IActionResult> PostMessageAsync(int roomId, [FromBody] ChatMessageViewModel message)
        {
            // check if room exists
            if (!await _chatContext.Rooms.AnyAsync(r => r.Id == roomId))
            {
                return NotFound();
            }

            var newMessage = new ChatMessage
            {
                RoomId = roomId,
                TimeStamp = DateTime.Now,
                //Username = "test", // for now
                Username = User.Identity.Name,
                Content = message.Content
            };

            _chatContext.Messages.Add(newMessage);
            await _chatContext.SaveChangesAsync();
            return Ok(message);
        }
    }
}
