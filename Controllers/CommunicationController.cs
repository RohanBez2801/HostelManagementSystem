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
    public class CommunicationController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet("log")]
        public IActionResult GetLog()
        {
            var logs = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureTable(conn);
                    string sql = "SELECT * FROM tbl_CommunicationLog ORDER BY DateSent DESC";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new
                            {
                                Id = reader["ID"],
                                Type = reader["MsgType"].ToString(),
                                Recipient = reader["Recipient"].ToString(),
                                Subject = reader["Subject"].ToString(),
                                Body = reader["Body"].ToString(),
                                Status = reader["Status"].ToString(),
                                Date = Convert.ToDateTime(reader["DateSent"]).ToString("yyyy-MM-dd HH:mm")
                            });
                        }
                    }
                }
                return Ok(logs);
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("email/send")]
        public IActionResult SendEmail([FromBody] EmailModel model)
        {
            // In a real app, this would use SMTP.
            // For this phase, we mock success and log it.
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureTable(conn);

                    // Determine recipients count (mock logic)
                    string recipientsDesc = model.RecipientType == "All" ? "All Parents" :
                                            model.RecipientType == "Grade" ? $"Grade {model.RecipientValue} Parents" :
                                            "Single Parent";

                    string sql = "INSERT INTO tbl_CommunicationLog (MsgType, Recipient, Subject, Body, Status, DateSent) VALUES ('Email', ?, ?, ?, 'Sent', ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", recipientsDesc);
                        cmd.Parameters.AddWithValue("?", model.Subject);
                        cmd.Parameters.AddWithValue("?", model.Body);
                        cmd.Parameters.AddWithValue("?", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Email Sent Successfully" });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("sms/send")]
        public IActionResult SendSMS([FromBody] object model)
        {
            return StatusCode(501, new { Message = "SMS Service Not Configured (Phase 2)" });
        }

        private void EnsureTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_CommunicationLog", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string sql = @"CREATE TABLE tbl_CommunicationLog (
                    ID AUTOINCREMENT PRIMARY KEY,
                    MsgType TEXT(20),
                    Recipient TEXT(100),
                    Subject TEXT(150),
                    Body MEMO,
                    Status TEXT(20),
                    DateSent DATETIME
                )";
                using (var cmd = new OleDbCommand(sql, conn)) cmd.ExecuteNonQuery();
            }
        }
    }

    public class EmailModel
    {
        public string RecipientType { get; set; } // All, Grade, Single
        public string RecipientValue { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
