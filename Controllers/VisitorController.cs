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
    public class VisitorController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet("all")]
        public IActionResult GetAllVisitors()
        {
            var visitors = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureVisitorTable(conn);
                    string sql = @"SELECT v.VisitorID, v.VisitorName, v.Phone, v.VisitDate, v.TimeIn, v.TimeOut, l.Surname, l.Names
                                   FROM tbl_Visitors v
                                   LEFT JOIN tbl_Learners l ON v.LearnerID = l.LearnerID
                                   ORDER BY v.VisitDate DESC, v.TimeIn DESC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            visitors.Add(new
                            {
                                Id = reader["VisitorID"],
                                Name = reader["VisitorName"]?.ToString(),
                                Phone = reader["Phone"]?.ToString(),
                                Student = $"{reader["Surname"]} {reader["Names"]}",
                                Date = Convert.ToDateTime(reader["VisitDate"]).ToString("yyyy-MM-dd"),
                                TimeIn = Convert.ToDateTime(reader["TimeIn"]).ToString("HH:mm"),
                                TimeOut = reader["TimeOut"] != DBNull.Value ? Convert.ToDateTime(reader["TimeOut"]).ToString("HH:mm") : "-"
                            });
                        }
                    }
                }
                return Ok(visitors);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("checkin")]
        public IActionResult CheckIn([FromBody] VisitorModel visitor)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureVisitorTable(conn);
                    string sql = "INSERT INTO tbl_Visitors (VisitorName, Phone, LearnerID, VisitDate, TimeIn) VALUES (?, ?, ?, ?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", visitor.VisitorName);
                        cmd.Parameters.AddWithValue("?", visitor.Phone);
                        cmd.Parameters.AddWithValue("?", visitor.LearnerID);
                        cmd.Parameters.AddWithValue("?", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("?", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Visitor Checked In" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPut("checkout/{id}")]
        public IActionResult CheckOut(int id)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    string sql = "UPDATE tbl_Visitors SET TimeOut = ? WHERE VisitorID = ?";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", DateTime.Now);
                        cmd.Parameters.AddWithValue("?", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Visitor Checked Out" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        private void EnsureVisitorTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Visitors", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string createSql = @"CREATE TABLE tbl_Visitors (
                    VisitorID AUTOINCREMENT PRIMARY KEY,
                    VisitorName TEXT(100),
                    Phone TEXT(20),
                    LearnerID INT,
                    VisitDate DATETIME,
                    TimeIn DATETIME,
                    TimeOut DATETIME
                )";
                using (var cmd = new OleDbCommand(createSql, conn)) cmd.ExecuteNonQuery();
            }
        }
    }

    public class VisitorModel
    {
        public string VisitorName { get; set; }
        public string Phone { get; set; }
        public int LearnerID { get; set; }
    }
}
