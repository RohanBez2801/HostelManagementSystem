using Microsoft.AspNetCore.Mvc;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System;
using System.IO;
using System.Data;
using System.Linq;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class AttendanceController : ControllerBase
    {
        // GET: api/attendance/load?date=2025-12-23
        [HttpGet("load")]
        public IActionResult LoadAttendance([FromQuery] DateTime date)
        {
            try
            {
                var learners = new List<dynamic>();
                var attendanceRecords = new Dictionary<int, dynamic>();

                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    EnsureAttendanceTable(conn);

                    // STEP 1: Get All Learners
                    // FIX: Microsoft Access STRICTLY requires parentheses () around JOIN clauses
                    string sqlLearners = @"
                        SELECT L.[LearnerID], L.[AdmissionNo], L.[Surname], L.[Names], L.[Grade], R.[RoomNumber] 
                        FROM ([tbl_Learners] L
                        LEFT JOIN [tbl_Rooms] R ON L.[RoomID] = R.[RoomID])
                        ORDER BY R.[RoomNumber], L.[Surname]";

                    using (var cmd = new OleDbCommand(sqlLearners, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            learners.Add(new
                            {
                                Id = Convert.ToInt32(reader["LearnerID"]),
                                AdNo = reader["AdmissionNo"]?.ToString(),
                                Name = $"{reader["Surname"]} {reader["Names"]}",
                                Grade = reader["Grade"]?.ToString(),
                                Room = reader["RoomNumber"]?.ToString() ?? "Unassigned"
                            });
                        }
                    }

                    // STEP 2: Get Attendance for THIS Date only
                    string sqlAttendance = "SELECT [LearnerID], [Status], [Remarks] FROM [tbl_Attendance] WHERE [AttendanceDate] = ?";

                    using (var cmd = new OleDbCommand(sqlAttendance, conn))
                    {
                        cmd.Parameters.AddWithValue("?", date.Date);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int lId = Convert.ToInt32(reader["LearnerID"]);
                                if (!attendanceRecords.ContainsKey(lId))
                                {
                                    attendanceRecords.Add(lId, new
                                    {
                                        Status = reader["Status"]?.ToString(),
                                        Remarks = reader["Remarks"]?.ToString()
                                    });
                                }
                            }
                        }
                    }
                }

                // STEP 3: Merge in C# (Safe and Fast)
                var finalResult = learners.Select(l =>
                {
                    string status = "Present";
                    string remarks = "";

                    if (attendanceRecords.ContainsKey(l.Id))
                    {
                        var att = attendanceRecords[l.Id];
                        status = att.Status;
                        remarks = att.Remarks;
                    }

                    return new
                    {
                        learnerId = l.Id,
                        adNo = l.AdNo,
                        name = l.Name,
                        grade = l.Grade,
                        room = l.Room,
                        status = status,
                        remarks = remarks
                    };
                }).ToList();

                return Ok(finalResult);
            }
            catch (Exception ex)
            {
                // This puts the actual error details in the response so you can see it in the browser console
                return StatusCode(500, new { Message = "Server Error: " + ex.Message });
            }
        }

        // POST: api/attendance/save
        [HttpPost("save")]
        public IActionResult SaveAttendance([FromBody] AttendanceSaveRequest req)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    EnsureAttendanceTable(conn);

                    foreach (var item in req.Items)
                    {
                        // Check if record exists
                        string checkSql = "SELECT COUNT(*) FROM [tbl_Attendance] WHERE [LearnerID] = ? AND [AttendanceDate] = ?";
                        bool exists = false;
                        using (var cmdCheck = new OleDbCommand(checkSql, conn))
                        {
                            cmdCheck.Parameters.AddWithValue("?", item.LearnerId);
                            cmdCheck.Parameters.AddWithValue("?", req.Date.Date);
                            exists = Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0;
                        }

                        if (exists)
                        {
                            // Update
                            string updateSql = "UPDATE [tbl_Attendance] SET [Status] = ?, [Remarks] = ? WHERE [LearnerID] = ? AND [AttendanceDate] = ?";
                            using (var cmdUpd = new OleDbCommand(updateSql, conn))
                            {
                                cmdUpd.Parameters.AddWithValue("?", item.Status);
                                cmdUpd.Parameters.AddWithValue("?", item.Remarks ?? "");
                                cmdUpd.Parameters.AddWithValue("?", item.LearnerId);
                                cmdUpd.Parameters.AddWithValue("?", req.Date.Date);
                                cmdUpd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Insert
                            string insertSql = "INSERT INTO [tbl_Attendance] ([LearnerID], [AttendanceDate], [Status], [Remarks]) VALUES (?, ?, ?, ?)";
                            using (var cmdIns = new OleDbCommand(insertSql, conn))
                            {
                                cmdIns.Parameters.AddWithValue("?", item.LearnerId);
                                cmdIns.Parameters.AddWithValue("?", req.Date.Date);
                                cmdIns.Parameters.AddWithValue("?", item.Status);
                                cmdIns.Parameters.AddWithValue("?", item.Remarks ?? "");
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                    }
                }
                return Ok(new { Message = "Attendance saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error saving: " + ex.Message });
            }
        }

        private void EnsureAttendanceTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Attendance", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string createSql = @"CREATE TABLE [tbl_Attendance] (
                    [ID] AUTOINCREMENT PRIMARY KEY,
                    [LearnerID] INT,
                    [AttendanceDate] DATETIME,
                    [Status] TEXT(50),
                    [Remarks] MEMO
                )";
                using (var cmd = new OleDbCommand(createSql, conn)) cmd.ExecuteNonQuery();
            }
        }
    }

    public class AttendanceSaveRequest
    {
        public DateTime Date { get; set; }
        public List<AttendanceItem> Items { get; set; }
    }

    public class AttendanceItem
    {
        public int LearnerId { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
    }
}