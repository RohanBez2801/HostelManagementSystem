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
        [HttpGet("all")]
        public IActionResult GetAllParents()
        {
            var parents = new List<object>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    EnsureParentTable(conn);
                    string sql = @"SELECT * FROM tbl_Parents ORDER BY ParentName ASC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        // Optimization: Cache ordinals to avoid string-based lookups in loop
                        int ordParentID = reader.GetOrdinal("ParentID");
                        int ordParentName = reader.GetOrdinal("ParentName");
                        int ordPhone = reader.GetOrdinal("Phone");
                        int ordEmail = reader.GetOrdinal("Email");
                        int ordAddress = reader.GetOrdinal("Address");

                        while (reader.Read())
                        {
                            parents.Add(new
                            {
                                Id = reader.GetValue(ordParentID),
                                Name = reader.IsDBNull(ordParentName) ? "" : reader.GetValue(ordParentName).ToString(),
                                Phone = reader.IsDBNull(ordPhone) ? "" : reader.GetValue(ordPhone).ToString(),
                                Email = reader.IsDBNull(ordEmail) ? "" : reader.GetValue(ordEmail).ToString(),
                                Address = reader.IsDBNull(ordAddress) ? "" : reader.GetValue(ordAddress).ToString()
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
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    // We need to check if ParentID column exists in tbl_Learners
                    EnsureLearnerParentColumn(conn);

                    string sql = "SELECT LearnerID, Surname, Names, Grade, RoomID FROM tbl_Learners WHERE ParentID = ?";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", parentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            // Optimization: Cache ordinals to avoid string-based lookups in loop
                            int ordLearnerID = reader.GetOrdinal("LearnerID");
                            int ordSurname = reader.GetOrdinal("Surname");
                            int ordNames = reader.GetOrdinal("Names");
                            int ordGrade = reader.GetOrdinal("Grade");
                            int ordRoomID = reader.GetOrdinal("RoomID");

                            while (reader.Read())
                            {
                                string surname = reader.IsDBNull(ordSurname) ? "" : reader.GetValue(ordSurname).ToString();
                                string names = reader.IsDBNull(ordNames) ? "" : reader.GetValue(ordNames).ToString();

                                children.Add(new {
                                    Id = reader.GetValue(ordLearnerID),
                                    Name = $"{surname} {names}",
                                    Grade = reader.IsDBNull(ordGrade) ? "" : reader.GetValue(ordGrade).ToString(),
                                    RoomId = reader.GetValue(ordRoomID)
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
                using (var conn = Helpers.DbHelper.GetConnection())
                {
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
                using (var conn = Helpers.DbHelper.GetConnection())
                {
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
