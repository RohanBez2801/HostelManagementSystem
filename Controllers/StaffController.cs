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
                    // Optimization: Cache ordinals to avoid string-based lookups in loop (O(1) vs O(N))
                    int ordStaffID = reader.GetOrdinal("StaffID");
                    int ordFullName = reader.GetOrdinal("FullName");
                    int ordJobTitle = reader.GetOrdinal("JobTitle");
                    int ordShift = reader.GetOrdinal("Shift");
                    int ordContactNo = reader.GetOrdinal("ContactNo");

                    while (reader.Read())
                    {
                        staffList.Add(new
                        {
                            Id = reader.GetValue(ordStaffID),
                            Name = reader.GetValue(ordFullName),
                            Title = reader.GetValue(ordJobTitle),
                            Shift = reader.GetValue(ordShift),
                            Phone = reader.GetValue(ordContactNo)
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