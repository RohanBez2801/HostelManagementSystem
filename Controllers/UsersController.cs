using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.IO;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class UsersController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet("all")]
        public IActionResult GetAllUsers()
        {
            var list = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureUsersTable(conn);
                    string sql = "SELECT [UserID], [FullName], [Username], [UserRole], [Status] FROM [tbl_Users] ORDER BY [FullName]";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                id = reader["UserID"],
                                name = reader["FullName"],
                                username = reader["Username"],
                                role = reader["UserRole"],
                                status = reader["Status"] == DBNull.Value ? "Active" : reader["Status"]
                            });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("add")]
        public IActionResult AddUser([FromBody] UserModel user)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureUsersTable(conn);
                    string sql = "INSERT INTO [tbl_Users] ([FullName], [Username], [Password], [UserRole], [Status]) VALUES (?, ?, ?, ?, 'Active')";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", user.FullName);
                        cmd.Parameters.AddWithValue("?", user.Username);
                        cmd.Parameters.AddWithValue("?", user.Password);
                        cmd.Parameters.AddWithValue("?", user.Role);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "User created successfully" });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] UserModel user)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    string sql = "UPDATE [tbl_Users] SET [Password] = ? WHERE [UserID] = ?";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", user.Password);
                        cmd.Parameters.AddWithValue("?", user.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Password updated" });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    string sql = "DELETE FROM [tbl_Users] WHERE [UserID] = ?";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "User deleted" });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private void EnsureUsersTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Users", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string sql = @"CREATE TABLE [tbl_Users] (
                    [UserID] AUTOINCREMENT PRIMARY KEY,
                    [FullName] TEXT(100),
                    [Username] TEXT(50) UNIQUE,
                    [Password] TEXT(100),
                    [UserRole] TEXT(50),
                    [Status] TEXT(20)
                )";
                using (var cmd = new OleDbCommand(sql, conn)) cmd.ExecuteNonQuery();
            }
        }
    }

    public class UserModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}