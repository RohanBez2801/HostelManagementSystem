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
        [HttpGet("list")]
        public IActionResult GetAllStaff()
        {
            var staffList = new List<object>();
            using (var conn = Helpers.DbHelper.GetConnection())
            {
                string sql = "SELECT [StaffID], [FullName], [JobTitle], [Shift], [ContactNo] FROM [tbl_Staff] ORDER BY [FullName] ASC";
                OleDbCommand cmd = new OleDbCommand(sql, conn);
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
            using (var conn = Helpers.DbHelper.GetConnection())
            {
                string sql = "INSERT INTO [tbl_Staff] ([FullName], [IDNumber], [JobTitle], [Shift], [ContactNo]) VALUES (?, ?, ?, ?, ?)";
                OleDbCommand cmd = new OleDbCommand(sql, conn);
                cmd.Parameters.AddWithValue("?", staff.FullName);
                cmd.Parameters.AddWithValue("?", staff.IDNumber);
                cmd.Parameters.AddWithValue("?", staff.JobTitle);
                cmd.Parameters.AddWithValue("?", staff.Shift);
                cmd.Parameters.AddWithValue("?", staff.ContactNo);
                cmd.ExecuteNonQuery();
            }
            return Ok(new { Message = "Staff member added successfully" });
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteStaff(int id)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string sql = "DELETE FROM [tbl_Staff] WHERE [StaffID] = ?";
                    OleDbCommand cmd = new OleDbCommand(sql, conn);
                    cmd.Parameters.AddWithValue("?", id);
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