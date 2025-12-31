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
    public class NoticeController : ControllerBase
    {
        [HttpGet("all")]
        public IActionResult GetNotices()
        {
            var list = new List<object>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    EnsureNoticeTable(conn); // Auto-create table

                    string sql = "SELECT * FROM tbl_Notices ORDER BY DatePosted DESC";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                id = reader["NoticeID"],
                                message = reader["Message"].ToString(),
                                priority = reader["Priority"].ToString(),
                                date = Convert.ToDateTime(reader["DatePosted"]).ToString("dd MMM yyyy HH:mm")
                            });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("add")]
        public IActionResult AddNotice([FromBody] NoticeModel notice)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    EnsureNoticeTable(conn);

                    string sql = "INSERT INTO tbl_Notices (Message, Priority, DatePosted) VALUES (?, ?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", notice.Message);
                        cmd.Parameters.AddWithValue("?", notice.Priority ?? "Normal");
                        cmd.Parameters.AddWithValue("?", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Notice posted" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteNotice(int id)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    using (var cmd = new OleDbCommand("DELETE FROM tbl_Notices WHERE NoticeID = ?", conn))
                    {
                        cmd.Parameters.AddWithValue("?", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Notice deleted" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        private void EnsureNoticeTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Notices", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string sql = @"CREATE TABLE tbl_Notices (
                    NoticeID AUTOINCREMENT PRIMARY KEY,
                    Message MEMO,
                    Priority TEXT(20),
                    DatePosted DATETIME
                )";
                using (var cmd = new OleDbCommand(sql, conn)) cmd.ExecuteNonQuery();
            }
        }
    }

    public class NoticeModel
    {
        public string Message { get; set; }
        public string Priority { get; set; }
    }
}