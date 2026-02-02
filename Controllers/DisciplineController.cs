using Microsoft.AspNetCore.Mvc;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System;
using System.IO;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class DisciplineController : ControllerBase
    {
        [HttpPost("log")]
        public IActionResult LogIncident([FromBody] DisciplineModel report)
        {
            using (var conn = Helpers.DbHelper.GetConnection())
            {
                string sql = "INSERT INTO [tbl_Discipline] ([LearnerID], [IncidentDate], [Description], [Severity], [ReportedBy]) VALUES (?, ?, ?, ?, ?)";
                OleDbCommand cmd = new OleDbCommand(sql, conn);
                cmd.Parameters.AddWithValue("?", report.LearnerId);
                cmd.Parameters.AddWithValue("?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("?", report.Description);
                cmd.Parameters.AddWithValue("?", report.Severity);
                cmd.Parameters.AddWithValue("?", report.ReportedBy);

                cmd.ExecuteNonQuery();
            }
            return Ok(new { Message = "Incident logged successfully" });
        }

        [HttpGet("history/{learnerId}")]
        public IActionResult GetHistory(int learnerId)
        {
            var history = new List<object>();
            using (var conn = Helpers.DbHelper.GetConnection())
            {
                // JOIN added to get Parent Contact Info for the report
                string sql = @"
                    SELECT d.IncidentDate, d.Description, d.Severity, d.ReportedBy,
                           l.FatherEmail, l.MotherEmail, l.FatherPhone 
                    FROM [tbl_Discipline] d
                    INNER JOIN [tbl_Learners] l ON d.LearnerID = l.LearnerID
                    WHERE d.[LearnerID] = ? 
                    ORDER BY d.[IncidentDate] DESC";

                OleDbCommand cmd = new OleDbCommand(sql, conn);
                cmd.Parameters.AddWithValue("?", learnerId);
                using (var reader = cmd.ExecuteReader())
                {
                    // Optimization: Cache ordinals to avoid string-based lookups in loop
                    int ordIncidentDate = reader.GetOrdinal("IncidentDate");
                    int ordDescription = reader.GetOrdinal("Description");
                    int ordSeverity = reader.GetOrdinal("Severity");
                    int ordReportedBy = reader.GetOrdinal("ReportedBy");
                    int ordFatherEmail = reader.GetOrdinal("FatherEmail");
                    int ordMotherEmail = reader.GetOrdinal("MotherEmail");
                    int ordFatherPhone = reader.GetOrdinal("FatherPhone");

                    while (reader.Read())
                    {
                        // Use GetValue with ToString() to safely handle DBNull (returns empty string)
                        string email = reader.GetValue(ordFatherEmail).ToString();
                        if (string.IsNullOrEmpty(email)) email = reader.GetValue(ordMotherEmail).ToString();

                        history.Add(new
                        {
                            Date = reader.GetValue(ordIncidentDate),
                            Text = reader.GetValue(ordDescription),
                            Level = reader.GetValue(ordSeverity),
                            By = reader.GetValue(ordReportedBy),
                            ParentContact = reader.GetValue(ordFatherPhone).ToString(), // Useful for "Call Parent" button
                            ParentEmail = email
                        });
                    }
                }
            }
            return Ok(history);
        }
    }

    public class DisciplineModel
    {
        public int LearnerId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Minor";
        public string ReportedBy { get; set; } = string.Empty;
    }
}