using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.OleDb;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Use relative pathing to ensure the app works on other machines
        // Use your specific repo path as seen in the error message
        private readonly string _connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\rouxn\source\repos\HostelManagementSystem\Data\HostelDb.accdb;";
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            using (OleDbConnection conn = new OleDbConnection(_connectionString))
            {
                // 1. Username first (?), Password second (?)
                string sql = "SELECT UserID, UserRole, FullName FROM tbl_Users WHERE Username = ? AND [Password] = ?";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    // 2. MUST ADD IN THIS EXACT ORDER
                    cmd.Parameters.AddWithValue("?", request.Username); // Matches 1st ?
                    cmd.Parameters.AddWithValue("?", request.Password); // Matches 2nd ?

                    conn.Open();
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
        public string Username { get; set; }
        public string Password { get; set; }
    }
}