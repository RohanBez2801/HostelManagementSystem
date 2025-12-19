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
        private readonly string _connString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\rouxn\source\repos\HostelManagementSystem\Data\HostelDb.accdb;";

        [HttpGet("list")]
        public IActionResult GetAllStaff()
        {
            var staffList = new List<object>();
            using (OleDbConnection conn = new OleDbConnection(_connString))
            {
                string sql = "SELECT * FROM tbl_Staff ORDER BY FullName ASC";
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
                string sql = "INSERT INTO tbl_Staff (FullName, IDNumber, JobTitle, Shift, ContactNo) VALUES (?, ?, ?, ?, ?)";
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
    }

    public class StaffModel
    {
        public string FullName { get; set; }
        public string IDNumber { get; set; }
        public string JobTitle { get; set; }
        public string Shift { get; set; }
        public string ContactNo { get; set; }
    }
}