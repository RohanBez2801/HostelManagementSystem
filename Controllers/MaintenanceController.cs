using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.IO;

namespace HostelManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [SupportedOSPlatform("windows")]
    public class MaintenanceController : ControllerBase
    {
        private readonly string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet("all")]
        public IActionResult GetAll()
        {
            var list = new List<object>();
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                try
                {
                    // FIX: Changed INNER JOIN to LEFT JOIN so repairs show up even if room is deleted.
                    string sql = @"SELECT m.[MaintenanceID], m.[IssueDescription], m.[Priority], m.[ReportedDate], m.[Status], r.[RoomNumber] 
                                 FROM [tbl_Maintenance] m 
                                 LEFT JOIN [tbl_Rooms] r ON m.[RoomID] = r.[RoomID] 
                                 ORDER BY m.[ReportedDate] DESC";

                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        // Optimization: Cache ordinals to avoid string-based lookups in loop
                        int ordMaintenanceID = reader.GetOrdinal("MaintenanceID");
                        int ordRoomNumber = reader.GetOrdinal("RoomNumber");
                        int ordIssue = reader.GetOrdinal("IssueDescription");
                        int ordPriority = reader.GetOrdinal("Priority");
                        int ordStatus = reader.GetOrdinal("Status");
                        int ordReportedDate = reader.GetOrdinal("ReportedDate");

                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                Id = reader.GetValue(ordMaintenanceID),
                                RoomNumber = !reader.IsDBNull(ordRoomNumber) ? reader.GetValue(ordRoomNumber).ToString() : "Deleted Room",
                                IssueDescription = reader.IsDBNull(ordIssue) ? "-" : reader.GetValue(ordIssue).ToString(),
                                Priority = reader.IsDBNull(ordPriority) ? "Medium" : reader.GetValue(ordPriority).ToString(),
                                Status = reader.IsDBNull(ordStatus) ? "Pending" : reader.GetValue(ordStatus).ToString(),
                                ReportedDate = !reader.IsDBNull(ordReportedDate) ? Convert.ToDateTime(reader.GetValue(ordReportedDate)) : DateTime.MinValue
                            });
                        }
                    }
                    return Ok(list);
                }
                catch (Exception ex) { return StatusCode(500, new { Message = "Database Error: " + ex.Message }); }
            }
        }

        [HttpPost("add")]
        public IActionResult AddIssue([FromBody] MaintenanceRequest req)
        {
            if (req == null) return BadRequest("Invalid request data.");
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                try
                {
                    string sql = @"INSERT INTO [tbl_Maintenance] 
                                 ([RoomID], [IssueDescription], [Priority], [ReportedDate], [Status]) 
                                 VALUES (?, ?, ?, ?, ?)";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", req.RoomID);
                        cmd.Parameters.AddWithValue("?", req.IssueDescription);
                        cmd.Parameters.AddWithValue("?", req.Priority);
                        cmd.Parameters.AddWithValue("?", req.ReportedDate);
                        cmd.Parameters.AddWithValue("?", req.Status);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    return Ok(new { message = "Issue recorded successfully" });
                }
                catch (Exception ex) { return StatusCode(500, $"Database Error: {ex.Message}"); }
            }
        }

        [HttpPut("update-status/{id}")]
        public IActionResult UpdateStatus(int id, [FromBody] string newStatus)
        {
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                try
                {
                    string sql = "UPDATE [tbl_Maintenance] SET [Status] = ? WHERE [MaintenanceID] = ?";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", newStatus);
                        cmd.Parameters.AddWithValue("?", id);
                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0) return Ok();
                        return NotFound();
                    }
                }
                catch (Exception ex) { return StatusCode(500, $"Database Error: {ex.Message}"); }
            }
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteIssue(int id)
        {
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                try
                {
                    string sql = "DELETE FROM [tbl_Maintenance] WHERE [MaintenanceID] = ?";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", id);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    return Ok();
                }
                catch (Exception ex) { return StatusCode(500, $"Database Error: {ex.Message}"); }
            }
        }
    }

    public class MaintenanceRequest
    {
        public int RoomID { get; set; }
        public string IssueDescription { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public DateTime ReportedDate { get; set; }
        public string Status { get; set; } = "Pending";
    }
}