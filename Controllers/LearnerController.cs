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
    public class LearnerController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        // --- 1. THE ROBUST DATABASE FIXER ---
        [HttpGet("fix-db")]
        public IActionResult FixDatabase()
        {
            var log = new List<string>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();

                    // A. Ensure Columns Exist
                    var columns = new Dictionary<string, string> {
                        { "Surname", "TEXT(100)" }, { "Names", "TEXT(100)" }, { "FullName", "TEXT(255)" },
                        { "Gender", "TEXT(20)" }, { "PreferredName", "TEXT(50)" }, { "DOB", "DATETIME" },
                        { "HomeLanguage", "TEXT(50)" }, { "PlaceOfBirth", "TEXT(100)" }, { "Citizenship", "TEXT(50)" },
                        { "StudyPermitNo", "TEXT(50)" }, { "Religion", "TEXT(50)" }, { "LearnerCell", "TEXT(50)" },
                        { "HomeAddress", "MEMO" }, { "FatherName", "TEXT(100)" }, { "FatherPhone", "TEXT(50)" },
                        { "FatherEmail", "TEXT(100)" }, { "FatherID", "TEXT(50)" }, { "FatherEmployer", "TEXT(100)" },
                        { "MotherName", "TEXT(100)" }, { "MotherPhone", "TEXT(50)" }, { "MotherEmail", "TEXT(100)" },
                        { "MotherID", "TEXT(50)" }, { "MotherEmployer", "TEXT(100)" },
                        { "MedicalAidName", "TEXT(100)" }, { "MedicalAidNo", "TEXT(50)" }, { "DoctorName", "TEXT(100)" },
                        { "MedicalConditions", "MEMO" }, { "EmergencyContact", "MEMO" },
                        { "PrevSchool", "TEXT(100)" }, { "PrevHostel", "TEXT(100)" },
                        { "RefTeacher", "TEXT(100)" }, { "RefTeacherCell", "TEXT(50)" }, { "GradesRepeated", "TEXT(50)" }
                    };

                    var existingColumns = new List<string>();
                    DataTable schema = conn.GetSchema("Columns", new string[] { null, null, "tbl_Learners", null });
                    foreach (DataRow row in schema.Rows) existingColumns.Add(row["COLUMN_NAME"].ToString().ToLower());

                    foreach (var col in columns)
                    {
                        if (!existingColumns.Contains(col.Key.ToLower()))
                        {
                            try
                            {
                                using (var cmd = new OleDbCommand($"ALTER TABLE [tbl_Learners] ADD COLUMN [{col.Key}] {col.Value}", conn))
                                {
                                    cmd.ExecuteNonQuery();
                                    log.Add($"Created column '{col.Key}'");
                                }
                            }
                            catch { }
                        }
                    }

                    // B. SAFE SYNC: Use '&' instead of '+' to prevent Nulls from erasing names
                    // 1. Fill Surname/Names if missing but FullName exists
                    try
                    {
                        string splitSql = "UPDATE [tbl_Learners] SET [Surname] = Left([FullName], InStr([FullName], ' ') - 1), [Names] = Mid([FullName], InStr([FullName], ' ') + 1) WHERE ([Surname] IS NULL OR [Surname] = '') AND [FullName] LIKE '% %'";
                        using (var cmd = new OleDbCommand(splitSql, conn)) cmd.ExecuteNonQuery();
                    }
                    catch { }

                    // 2. Fill FullName if missing but Surname/Names exist (Using & for safety)
                    try
                    {
                        string concatSql = "UPDATE [tbl_Learners] SET [FullName] = [Surname] & ' ' & [Names] WHERE ([FullName] IS NULL OR [FullName] = '')";
                        using (var cmd = new OleDbCommand(concatSql, conn))
                        {
                            int rows = cmd.ExecuteNonQuery();
                            log.Add($"Resynced FullName for {rows} records.");
                        }
                    }
                    catch (Exception ex) { log.Add("Concat Error: " + ex.Message); }
                }
                return Ok(new { Message = "Database Repair Complete", Log = log });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Fix Failed: " + ex.Message, Log = log }); }
        }

        // --- 2. GET ALL LEARNERS (UPDATED: Sorts by Block -> Room -> Surname) ---
        [HttpGet("list-all")]
        public IActionResult GetAllLearners()
        {
            var list = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    // UPDATED SQL: Joins Blocks and sorts hierarchically
                    string sql = @"
                        SELECT L.[LearnerID], L.[AdmissionNo], L.[Surname], L.[Names], L.[FullName], 
                               L.[Grade], L.[Gender], L.[RoomID], 
                               R.[RoomNumber], B.[BlockName]
                        FROM ([tbl_Learners] L 
                        LEFT JOIN [tbl_Rooms] R ON L.[RoomID] = R.[RoomID])
                        LEFT JOIN [tbl_Blocks] B ON R.[BlockID] = B.[BlockID]
                        ORDER BY B.[BlockName], R.[RoomNumber], L.[Surname], L.[Names] ASC";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Name Construction
                            string sName = reader["Surname"]?.ToString() ?? "";
                            string fName = reader["Names"]?.ToString() ?? "";
                            string dbFull = reader["FullName"]?.ToString();
                            string displayName = !string.IsNullOrWhiteSpace(dbFull) ? dbFull : $"{sName} {fName}".Trim();
                            if (string.IsNullOrWhiteSpace(displayName)) displayName = "Unknown";

                            // Room Construction (Block - Room)
                            string rNum = reader["RoomNumber"]?.ToString();
                            string bName = reader["BlockName"]?.ToString();
                            string displayRoom = "Unassigned";

                            if (!string.IsNullOrEmpty(rNum))
                            {
                                // If we have a block name, combine them. E.g., "Block A - 101"
                                displayRoom = !string.IsNullOrEmpty(bName) ? $"{bName} - {rNum}" : rNum;
                            }

                            list.Add(new
                            {
                                id = reader["LearnerID"],
                                adNo = reader["AdmissionNo"]?.ToString() ?? "N/A",
                                name = displayName,
                                surname = sName,
                                names = fName,
                                grade = reader["Grade"]?.ToString() ?? "-",
                                gender = reader["Gender"]?.ToString() ?? "-",
                                roomId = reader["RoomID"] != DBNull.Value ? reader["RoomID"] : 0,
                                room = displayRoom // Sends "Block A - 101" to frontend
                            });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Server Error: " + ex.Message }); }
        }

        // --- 3. REGISTER LEARNER (Syncs Data) ---
        [HttpPost("register")]
        public IActionResult RegisterLearner([FromBody] LearnerRegistrationModel req)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();

                    string surname = req.Surname ?? "";
                    string names = req.Names ?? "";
                    string fullName = $"{surname} {names}".Trim();

                    string sql = @"INSERT INTO [tbl_Learners] 
                    (
                        [AdmissionNo], [Surname], [Names], [FullName], [Gender], [RoomID], [DOB], [Grade], [HomeLanguage],
                        [PreferredName], [PlaceOfBirth], [Citizenship], [StudyPermitNo], [Religion], [LearnerCell], [HomeAddress],
                        [PrevSchool], [PrevHostel], [RefTeacher], [RefTeacherCell], [GradesRepeated],
                        [FatherName], [FatherID], [FatherEmployer], [FatherPhone], [FatherEmail],
                        [MotherName], [MotherID], [MotherEmployer], [MotherPhone], [MotherEmail],
                        [MedicalAidName], [MedicalAidNo], [DoctorName], [MedicalConditions], [EmergencyContact]
                    ) 
                    VALUES 
                    (
                        ?, ?, ?, ?, ?, ?, ?, ?, ?, 
                        ?, ?, ?, ?, ?, ?, ?, 
                        ?, ?, ?, ?, ?, 
                        ?, ?, ?, ?, ?, 
                        ?, ?, ?, ?, ?, 
                        ?, ?, ?, ?, ?
                    )";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", req.AdmissionNo ?? "");
                        cmd.Parameters.AddWithValue("?", surname);
                        cmd.Parameters.AddWithValue("?", names);
                        cmd.Parameters.AddWithValue("?", fullName);
                        cmd.Parameters.AddWithValue("?", req.Gender ?? "Male");
                        cmd.Parameters.AddWithValue("?", req.RoomId);
                        cmd.Parameters.AddWithValue("?", req.DOB);
                        cmd.Parameters.AddWithValue("?", req.Grade);
                        cmd.Parameters.AddWithValue("?", req.HomeLanguage ?? "");
                        cmd.Parameters.AddWithValue("?", req.PreferredName ?? "");
                        cmd.Parameters.AddWithValue("?", req.PlaceOfBirth ?? "");
                        cmd.Parameters.AddWithValue("?", req.Citizenship ?? "Namibian");
                        cmd.Parameters.AddWithValue("?", req.StudyPermitNo ?? "");
                        cmd.Parameters.AddWithValue("?", req.Religion ?? "");
                        cmd.Parameters.AddWithValue("?", req.LearnerCell ?? "");
                        cmd.Parameters.AddWithValue("?", req.HomeAddress ?? "");
                        cmd.Parameters.AddWithValue("?", req.PrevSchool ?? "");
                        cmd.Parameters.AddWithValue("?", req.PrevHostel ?? "");
                        cmd.Parameters.AddWithValue("?", req.RefTeacher ?? "");
                        cmd.Parameters.AddWithValue("?", req.RefTeacherCell ?? "");
                        cmd.Parameters.AddWithValue("?", req.GradesRepeated ?? "");
                        cmd.Parameters.AddWithValue("?", req.FatherName ?? "");
                        cmd.Parameters.AddWithValue("?", req.FatherID ?? "");
                        cmd.Parameters.AddWithValue("?", req.FatherEmployer ?? "");
                        cmd.Parameters.AddWithValue("?", req.FatherPhone ?? "");
                        cmd.Parameters.AddWithValue("?", req.FatherEmail ?? "");
                        cmd.Parameters.AddWithValue("?", req.MotherName ?? "");
                        cmd.Parameters.AddWithValue("?", req.MotherID ?? "");
                        cmd.Parameters.AddWithValue("?", req.MotherEmployer ?? "");
                        cmd.Parameters.AddWithValue("?", req.MotherPhone ?? "");
                        cmd.Parameters.AddWithValue("?", req.MotherEmail ?? "");
                        cmd.Parameters.AddWithValue("?", req.MedicalAidName ?? "");
                        cmd.Parameters.AddWithValue("?", req.MedicalAidNo ?? "");
                        cmd.Parameters.AddWithValue("?", req.DoctorName ?? "");
                        cmd.Parameters.AddWithValue("?", req.MedicalConditions ?? "");
                        cmd.Parameters.AddWithValue("?", req.EmergencyContact ?? "");

                        cmd.ExecuteNonQuery();
                    }

                    // Update Room
                    string updateRoomSql = "UPDATE [tbl_Rooms] SET [CurrentOccupancy] = [CurrentOccupancy] + 1 WHERE [RoomID] = ? AND [CurrentOccupancy] < [Capacity]";
                    using (OleDbCommand cmdUpdate = new OleDbCommand(updateRoomSql, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("?", req.RoomId);
                        cmdUpdate.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Learner registered successfully!" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Error: " + ex.Message }); }
        }

        // --- 4. DELETE LEARNER ---
        [HttpDelete("delete/{id}")]
        public IActionResult DeleteLearner(int id)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    int roomId = -1;
                    using (OleDbCommand cmdFind = new OleDbCommand("SELECT [RoomID] FROM [tbl_Learners] WHERE [LearnerID] = ?", conn))
                    {
                        cmdFind.Parameters.AddWithValue("?", id);
                        var res = cmdFind.ExecuteScalar();
                        if (res != null && res != DBNull.Value) roomId = Convert.ToInt32(res);
                    }

                    using (OleDbCommand cmdDel = new OleDbCommand("DELETE FROM [tbl_Learners] WHERE [LearnerID] = ?", conn))
                    {
                        cmdDel.Parameters.AddWithValue("?", id);
                        cmdDel.ExecuteNonQuery();
                    }

                    if (roomId > 0)
                    {
                        using (OleDbCommand cmdUpd = new OleDbCommand("UPDATE [tbl_Rooms] SET [CurrentOccupancy] = [CurrentOccupancy] - 1 WHERE [RoomID] = ?", conn))
                        {
                            cmdUpd.Parameters.AddWithValue("?", roomId);
                            cmdUpd.ExecuteNonQuery();
                        }
                    }
                }
                return Ok(new { Message = "Learner deleted." });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // --- 5. UPDATE LEARNER ---
        [HttpPut("update/{id}")]
        public IActionResult UpdateLearner(int id, [FromBody] LearnerRegistrationModel request)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();

                    string surname = request.Surname ?? "";
                    string names = request.Names ?? "";
                    string fullName = $"{surname} {names}".Trim();

                    string updateSql = "UPDATE [tbl_Learners] SET [AdmissionNo] = ?, [Surname] = ?, [Names] = ?, [FullName] = ?, [Grade] = ?, [RoomID] = ? WHERE [LearnerID] = ?";
                    using (OleDbCommand cmd = new OleDbCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", request.AdmissionNo);
                        cmd.Parameters.AddWithValue("?", surname);
                        cmd.Parameters.AddWithValue("?", names);
                        cmd.Parameters.AddWithValue("?", fullName);
                        cmd.Parameters.AddWithValue("?", request.Grade);
                        cmd.Parameters.AddWithValue("?", request.RoomId);
                        cmd.Parameters.AddWithValue("?", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Learner updated successfully!" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Error: " + ex.Message }); }
        }
    }

    public class LearnerRegistrationModel
    {
        public string Surname { get; set; }
        public string Names { get; set; }
        public string Gender { get; set; }
        public int RoomId { get; set; }
        public string AdmissionNo { get; set; }
        public string PreferredName { get; set; }
        public DateTime DOB { get; set; }
        public int Grade { get; set; }
        public string HomeLanguage { get; set; }
        public string PlaceOfBirth { get; set; }
        public string Citizenship { get; set; }
        public string StudyPermitNo { get; set; }
        public string Religion { get; set; }
        public string LearnerCell { get; set; }
        public string HomeAddress { get; set; }
        public string PrevSchool { get; set; }
        public string PrevHostel { get; set; }
        public string RefTeacher { get; set; }
        public string RefTeacherCell { get; set; }
        public string GradesRepeated { get; set; }
        public string FatherName { get; set; }
        public string FatherID { get; set; }
        public string FatherEmployer { get; set; }
        public string FatherPhone { get; set; }
        public string FatherEmail { get; set; }
        public string MotherName { get; set; }
        public string MotherID { get; set; }
        public string MotherEmployer { get; set; }
        public string MotherPhone { get; set; }
        public string MotherEmail { get; set; }
        public string MedicalAidName { get; set; }
        public string MedicalAidNo { get; set; }
        public string DoctorName { get; set; }
        public string MedicalConditions { get; set; }
        public string EmergencyContact { get; set; }
    }
}