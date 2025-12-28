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
    public class ParentController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet("all")]
        public IActionResult GetAllParents()
        {
            var parents = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureParentTable(conn);
                    string sql = @"SELECT * FROM tbl_Parents ORDER BY ParentName ASC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            parents.Add(new
                            {
                                Id = reader["ParentID"],
                                Name = reader["ParentName"]?.ToString(),
                                Phone = reader["Phone"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                Address = reader["Address"]?.ToString()
                            });
                        }
                    }
                }
                return Ok(parents);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpGet("children/{parentId}")]
        public IActionResult GetChildren(int parentId)
        {
            var children = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    // We need to check if ParentID column exists in tbl_Learners
                    EnsureLearnerParentColumn(conn);

                    string sql = "SELECT LearnerID, Surname, Names, Grade, RoomID FROM tbl_Learners WHERE ParentID = ?";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", parentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                children.Add(new {
                                    Id = reader["LearnerID"],
                                    Name = $"{reader["Surname"]} {reader["Names"]}",
                                    Grade = reader["Grade"]?.ToString(),
                                    RoomId = reader["RoomID"]
                                });
                            }
                        }
                    }
                }
                return Ok(children);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("add")]
        public IActionResult AddParent([FromBody] ParentModel parent)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureParentTable(conn);
                    string sql = "INSERT INTO tbl_Parents (ParentName, Phone, Email, Address) VALUES (?, ?, ?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", parent.Name);
                        cmd.Parameters.AddWithValue("?", parent.Phone ?? "");
                        cmd.Parameters.AddWithValue("?", parent.Email ?? "");
                        cmd.Parameters.AddWithValue("?", parent.Address ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Parent Added" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("link")]
        public IActionResult LinkChild([FromBody] LinkChildModel link)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureLearnerParentColumn(conn);
                    string sql = "UPDATE tbl_Learners SET ParentID = ? WHERE LearnerID = ?";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", link.ParentId);
                        cmd.Parameters.AddWithValue("?", link.LearnerId);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Child Linked" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        private void EnsureParentTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Parents", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string createSql = @"CREATE TABLE tbl_Parents (
                    ParentID AUTOINCREMENT PRIMARY KEY,
                    ParentName TEXT(150),
                    Phone TEXT(50),
                    Email TEXT(100),
                    Address TEXT(255)
                )";
                using (var cmd = new OleDbCommand(createSql, conn)) cmd.ExecuteNonQuery();
            }
        }

        private void EnsureLearnerParentColumn(OleDbConnection conn)
        {
            // Check if ParentID exists in tbl_Learners
            var schema = conn.GetSchema("Columns", new string[] { null, null, "tbl_Learners", "ParentID" });
            if (schema.Rows.Count == 0)
            {
                try
                {
                    using (var cmd = new OleDbCommand("ALTER TABLE tbl_Learners ADD COLUMN ParentID INT", conn))
                        cmd.ExecuteNonQuery();
                }
                catch { /* Ignore if race condition or exists */ }
            }
        }
    }

    public class ParentModel
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }

    public class LinkChildModel
    {
        public int ParentId { get; set; }
        public int LearnerId { get; set; }
    }
}
