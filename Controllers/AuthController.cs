using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.OleDb;
using HostelManagementSystem.Helpers;
using System.Runtime.Versioning;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Invalid request.");

            try
            {
                // FIX 1: Use the centralized connection from DbHelper
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();

                    // FIX 2: Added brackets [] around reserved words [Password] and table name [tbl_Users]
                    // This prevents the "Syntax Error" crash in Access.
                    string sql = "SELECT [UserID], [UserRole], [FullName] FROM [tbl_Users] WHERE [Username] = ? AND [Password] = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", request.Username);
                        cmd.Parameters.AddWithValue("?", request.Password); // Note: In production, hash this!

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Login Success
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
                return Unauthorized(new { Message = "Invalid username or password" });
            }
            catch (Exception ex)
            {
                // Log the actual error to help debugging
                return StatusCode(500, new { Message = "Database Error: " + ex.Message });
            }
        }
    }

    // This model ensures the JSON body { "username": "...", "password": "..." } is read correctly
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}