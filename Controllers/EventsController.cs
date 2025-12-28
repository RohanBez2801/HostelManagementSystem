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
    public class EventsController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet("all")]
        public IActionResult GetEvents()
        {
            var list = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureTable(conn);
                    string sql = "SELECT * FROM tbl_Events ORDER BY EventDate ASC";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                Id = reader["EventID"],
                                Title = reader["Title"].ToString(),
                                Description = reader["Description"].ToString(),
                                Date = Convert.ToDateTime(reader["EventDate"]).ToString("yyyy-MM-dd"),
                                Type = reader["EventType"].ToString()
                            });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("add")]
        public IActionResult AddEvent([FromBody] EventModel model)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureTable(conn);
                    string sql = "INSERT INTO tbl_Events (Title, Description, EventDate, EventType) VALUES (?, ?, ?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", model.Title);
                        cmd.Parameters.AddWithValue("?", model.Description ?? "");
                        cmd.Parameters.AddWithValue("?", DateTime.Parse(model.Date));
                        cmd.Parameters.AddWithValue("?", model.Type ?? "General");
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Event Added" });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteEvent(int id)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("DELETE FROM tbl_Events WHERE EventID = ?", conn))
                    {
                        cmd.Parameters.AddWithValue("?", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Event Deleted" });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private void EnsureTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Events", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string sql = @"CREATE TABLE tbl_Events (
                    EventID AUTOINCREMENT PRIMARY KEY,
                    Title TEXT(150),
                    Description MEMO,
                    EventDate DATETIME,
                    EventType TEXT(50)
                )";
                using (var cmd = new OleDbCommand(sql, conn)) cmd.ExecuteNonQuery();
            }
        }
    }

    public class EventModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Date { get; set; }
        public string Type { get; set; }
    }
}
