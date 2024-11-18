﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RequestWebApi.Models;

namespace RequestWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestController : ControllerBase
    {
        private readonly RequestDbContext _dbContext;
        
        public RequestController(RequestDbContext RequestDbContext)
        {
            _dbContext = RequestDbContext;
        }

        // GET: api/Request/requests
        [HttpGet("requests")]
        public async Task<ActionResult<IEnumerable<Request>>> GetRequests(int? id, int pageNumber = 1, int pageSize = 20)
        {
            var query = _dbContext.Requests.AsQueryable();

            // Nếu có id, chỉ lấy yêu cầu với receiver bằng id đó
            if (id.HasValue)
            {
                query = query.Where(r => r.Receiver == id.Value);
            }

            // Tính toán tổng số yêu cầu
            var totalRequests = await query.CountAsync();

            // Lấy danh sách yêu cầu theo trang
            var requests = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Trả về kết quả với thông tin tổng số
            Response.Headers.Add("X-Total-Count", totalRequests.ToString());

            // Nếu không có yêu cầu nào, trả về 204 No Content
            if (!requests.Any())
            {
                return NoContent();
            }

            return Ok(requests);
        }


        // GET: api/Request/1
        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<Request>>> GetRequestsByUserId(int id)
        {
            Console.WriteLine($"API Gateway Error: {id}");
            // Lọc các Request mà SenderId hoặc ReceiverId bằng id
            var requests = await _dbContext.Requests
                .Where(r => r.Sender == id || r.Receiver == id)
                .ToListAsync();

            // Nếu không có dữ liệu, trả về mảng rỗng
            return Ok(requests);
        }
        
        // POST: http://localhost:8001/api/request
        [HttpPost]
        public async Task<ActionResult> Create(Request request)
        {
            
            // Kiểm tra nếu yêu cầu đã tồn tại (trong trường hợp sender và receiver đổi vị trí)
            var existingRequest = await _dbContext.Requests
                .FirstOrDefaultAsync(r => 
                    (r.Sender == request.Sender && r.Receiver == request.Receiver) || 
                    (r.Sender == request.Receiver && r.Receiver == request.Sender));

            if (existingRequest != null)
            {
                // Trả về trạng thái tồn tại
                return Conflict(new { status = "exists", message = "Request already exists" });
            }
            // Thêm đối tượng Request vào cơ sở dữ liệu
            await _dbContext.Requests.AddAsync(request);
            await _dbContext.SaveChangesAsync();

            // Lấy ID của Request vừa tạo
            var createdRequestId = request.Id;

            // Trả về ID của Request vừa tạo
            return Ok(new { id = createdRequestId });
        }
        
        // DELETE: api/Request/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _dbContext.Requests.FindAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            _dbContext.Requests.Remove(request);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Request/1/1
        [HttpGet("{curUserId}/{userId}")]
        public async Task<ActionResult<IEnumerable<Request>>> GetRequestsByCurrentUserIdAndUser(int curUserId, int userId)
        {
            // Lọc các Request mà SenderId hoặc ReceiverId bằng id
            var requests = await _dbContext.Requests
                .Where(r => r.Sender == userId && r.Receiver == curUserId)
                .ToListAsync();

            // Nếu không có dữ liệu, trả về mảng rỗng
            return Ok(requests);
        }
        // DELETE: api/Request/delete?senderId=1&receiverId=2
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteRequestBySenderAndReceiver(int senderId, int receiverId)
        {
            var request = await _dbContext.Requests
                .FirstOrDefaultAsync(r => r.Sender == senderId && r.Receiver == receiverId);

            if (request == null)
            {
                return NotFound(new { message = "Request not found" });
            }

            _dbContext.Requests.Remove(request);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}