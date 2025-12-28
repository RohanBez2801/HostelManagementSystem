using Microsoft.AspNetCore.Mvc;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class StaffController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet("list")]
        public IActionResult GetAllStaff()
        {
            var staffList = new List<object>();
            using (OleDbConnection conn = new OleDbConnection(_connString))
            {
                string sql = "SELECT [StaffID], [FullName], [JobTitle], [Shift], [ContactNo] FROM [tbl_Staff] ORDER BY [FullName] ASC";
                OleDbCommand cmd = new OleDbCommand(sql, conn);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        staffList.Add(new
                        {
                            Id = reader["StaffID"],
                            Name = reader["FullName"],
                            Title = reader["JobTitle"],
                            Shift = reader["Shift"],
                            Phone = reader["ContactNo"]
                        });
                    }
                }
            }
            return Ok(staffList);
        }

        [HttpPost("add")]
        public IActionResult AddStaff([FromBody] StaffModel staff)
        {
            using (OleDbConnection conn = new OleDbConnection(_connString))
            {
                string sql = "INSERT INTO [tbl_Staff] ([FullName], [IDNumber], [JobTitle], [Shift], [ContactNo]) VALUES (?, ?, ?, ?, ?)";
                OleDbCommand cmd = new OleDbCommand(sql, conn);
                cmd.Parameters.AddWithValue("?", staff.FullName);
                cmd.Parameters.AddWithValue("?", staff.IDNumber);
                cmd.Parameters.AddWithValue("?", staff.JobTitle);
                cmd.Parameters.AddWithValue("?", staff.Shift);
                cmd.Parameters.AddWithValue("?", staff.ContactNo);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(new { Message = "Staff member added successfully" });
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteStaff(int id)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    string sql = "DELETE FROM [tbl_Staff] WHERE [StaffID] = ?";
                    OleDbCommand cmd = new OleDbCommand(sql, conn);
                    cmd.Parameters.AddWithValue("?", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class StaffModel
    {
        public string FullName { get; set; } = string.Empty;
        public string IDNumber { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Shift { get; set; } = string.Empty;
        public string ContactNo { get; set; } = string.Empty;
    }
}