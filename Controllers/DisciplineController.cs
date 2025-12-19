using Microsoft.AspNetCore.Mvc;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class DisciplineController : ControllerBase
    {
        private readonly string _connString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\rouxn\source\repos\HostelManagementSystem\Data\HostelDb.accdb;";

        [HttpPost("log")]
        public IActionResult LogIncident([FromBody] DisciplineModel report)
        {
            using (OleDbConnection conn = new OleDbConnection(_connString))
            {
                string sql = "INSERT INTO tbl_Discipline (LearnerID, IncidentDate, Description, Severity, ReportedBy) VALUES (?, ?, ?, ?, ?)";
                OleDbCommand cmd = new OleDbCommand(sql, conn);
                cmd.Parameters.AddWithValue("?", report.LearnerId);
                cmd.Parameters.AddWithValue("?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("?", report.Description);
                cmd.Parameters.AddWithValue("?", report.Severity);
                cmd.Parameters.AddWithValue("?", report.ReportedBy);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(new { Message = "Incident logged successfully" });
        }

        [HttpGet("history/{learnerId}")]
        public IActionResult GetHistory(int learnerId)
        {
            var history = new List<object>();
            using (OleDbConnection conn = new OleDbConnection(_connString))
            {
                string sql = "SELECT * FROM tbl_Discipline WHERE LearnerID = ? ORDER BY IncidentDate DESC";
                OleDbCommand cmd = new OleDbCommand(sql, conn);
                cmd.Parameters.AddWithValue("?", learnerId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        history.Add(new
                        {
                            Date = reader["IncidentDate"],
                            Text = reader["Description"],
                            Level = reader["Severity"],
                            By = reader["ReportedBy"]
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
        public string Description { get; set; }
        public string Severity { get; set; }
        public string ReportedBy { get; set; }
    }
}