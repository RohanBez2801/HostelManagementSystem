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
    public class LeaveController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet("all")]
        public IActionResult GetAllLeave()
        {
            var leaveRecords = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    // *** FIX: This line auto-creates the missing table ***
                    EnsureLeaveTable(conn);

                    string sql = @"SELECT v.[LeaveID], l.[Surname], l.[Names], l.[FatherPhone], l.[MotherPhone], 
                                   v.[LeaveType], v.[DepartureDate], 
                                   v.[ExpectedReturnDate], v.[Status], v.[ContactPerson]
                                   FROM [tbl_Leave] v
                                   INNER JOIN [tbl_Learners] l ON v.[LearnerID] = l.[LearnerID]
                                   ORDER BY v.[DepartureDate] DESC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string pPhone = reader["FatherPhone"]?.ToString();
                            if (string.IsNullOrEmpty(pPhone)) pPhone = reader["MotherPhone"]?.ToString();
                            string fullName = $"{reader["Surname"]} {reader["Names"]}".Trim();

                            leaveRecords.Add(new
                            {
                                id = reader["LeaveID"],
                                learnerName = fullName,
                                parentPhone = pPhone ?? "N/A",
                                type = reader["LeaveType"],
                                departure = Convert.ToDateTime(reader["DepartureDate"]).ToString("yyyy-MM-dd HH:mm"),
                                expectedReturn = Convert.ToDateTime(reader["ExpectedReturnDate"]).ToString("yyyy-MM-dd HH:mm"),
                                status = reader["Status"].ToString(),
                                contact = reader["ContactPerson"].ToString()
                            });
                        }
                    }
                }
                return Ok(leaveRecords);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Error: " + ex.Message }); }
        }

        [HttpPost("add")]
        public IActionResult AddLeave([FromBody] LeaveModel leave)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureLeaveTable(conn);

                    string sql = "INSERT INTO [tbl_Leave] ([LearnerID], [LeaveType], [DepartureDate], [ExpectedReturnDate], [Status], [ContactPerson]) VALUES (?, ?, ?, ?, 'Away', ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", leave.LearnerId);
                        cmd.Parameters.AddWithValue("?", leave.LeaveType);
                        cmd.Parameters.AddWithValue("?", leave.DepartureDate);
                        cmd.Parameters.AddWithValue("?", leave.ExpectedReturnDate);
                        cmd.Parameters.AddWithValue("?", leave.ContactPerson);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Leave record created" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPut("return/{leaveId}")]
        public IActionResult MarkReturned(int leaveId)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureLeaveTable(conn);

                    string sql = "UPDATE [tbl_Leave] SET [Status] = 'Returned' WHERE [LeaveID] = ?";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", leaveId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Learner returned" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // --- HELPER: AUTO-CREATES THE MISSING TABLE ---
        private void EnsureLeaveTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Leave", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string createSql = @"CREATE TABLE [tbl_Leave] (
                    [LeaveID] AUTOINCREMENT PRIMARY KEY,
                    [LearnerID] INT,
                    [LeaveType] TEXT(50),
                    [DepartureDate] DATETIME,
                    [ExpectedReturnDate] DATETIME,
                    [Status] TEXT(50),
                    [ContactPerson] TEXT(100)
                )";
                using (var cmd = new OleDbCommand(createSql, conn)) cmd.ExecuteNonQuery();
            }
        }
    }

    public class LeaveModel
    {
        public int LearnerId { get; set; }
        public string LeaveType { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public string ContactPerson { get; set; }
    }
}