using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace HostelManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [SupportedOSPlatform("windows")]
    public class MaintenanceController : ControllerBase
    {
        // Connection string for MS Access
        private string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        /// <summary>
        /// Retrieves all maintenance issues, joined with Room data for display.
        /// </summary>
        [HttpGet("all")]
        public IActionResult GetAll()
        {
            var list = new List<object>();
            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                try
                {
                    // INNER JOIN ensures we get the human-readable RoomNumber from tbl_Rooms. Bracketing all identifiers.
                    string sql = @"SELECT m.[MaintenanceID], m.[IssueDescription], m.[Priority], m.[ReportedDate], m.[Status], r.[RoomNumber] 
                                 FROM [tbl_Maintenance] m 
                                 INNER JOIN [tbl_Rooms] r ON m.[RoomID] = r.[RoomID] 
                                 ORDER BY m.[ReportedDate] DESC";

                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                list.Add(new
                                {
                                    Id = reader["MaintenanceID"],
                                    RoomNumber = reader["RoomNumber"]?.ToString() ?? "N/A",
                                    IssueDescription = reader["IssueDescription"]?.ToString() ?? "-",
                                    Priority = reader["Priority"]?.ToString() ?? "Medium",
                                    Status = reader["Status"]?.ToString() ?? "Pending",
                                    ReportedDate = reader["ReportedDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReportedDate"]) : DateTime.MinValue
                                });
                            }
                            catch (Exception rowEx)
                            {
                                Console.WriteLine($"Error reading maintenance row: {rowEx.Message}");
                            }
                        }
                    }
                    return Ok(list);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "Database Error: " + ex.Message });
                }
            }
        }

        /// <summary>
        /// Records a new maintenance defect reported by staff.
        /// </summary>
        [HttpPost("add")]
        public IActionResult AddIssue([FromBody] MaintenanceRequest req)
        {
            if (req == null) return BadRequest("Invalid request data.");

            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                try
                {
                    // Access requires brackets around reserved words like [Status]
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
                catch (Exception ex)
                {
                    return StatusCode(500, $"Database Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Updates the status of an issue (e.g., marking it as Fixed).
        /// </summary>
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
                catch (Exception ex)
                {
                    return StatusCode(500, $"Database Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Deletes a maintenance record from the history.
        /// </summary>
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
                catch (Exception ex)
                {
                    return StatusCode(500, $"Database Error: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// DTO for incoming Maintenance Data.
    /// </summary>
    public class MaintenanceRequest
    {
        public int RoomID { get; set; }
        public string IssueDescription { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public DateTime ReportedDate { get; set; }
        public string Status { get; set; } = "Pending";
    }
}