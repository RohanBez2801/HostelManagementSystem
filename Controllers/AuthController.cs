using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.OleDb;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            using (var conn = Helpers.DbHelper.GetConnection())
            {
                // 1. Username first (?), Password second (?)
                string sql = "SELECT [UserID], [UserRole], [FullName] FROM [tbl_Users] WHERE [Username] = ? AND [Password] = ?";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    // 2. MUST ADD IN THIS EXACT ORDER
                    cmd.Parameters.AddWithValue("?", request.Username); // Matches 1st ?
                    cmd.Parameters.AddWithValue("?", request.Password); // Matches 2nd ?

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new
                            {
                                UserId = reader["UserID"],
                                Role = reader["UserRole"].ToString(),
                                UserName = reader["FullName"].ToString()
                            });
                        }
                    }
                }
            }
            return Unauthorized(new { Message = "Invalid credentials" });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}