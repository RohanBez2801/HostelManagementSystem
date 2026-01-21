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
        // --- 1. THE ROBUST DATABASE FIXER ---
        [HttpGet("fix-db")]
        public IActionResult FixDatabase()
        {
            var log = new List<string>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    // A. Ensure Columns Exist (Extended List)
                    var columns = new Dictionary<string, string> {
                        // Basic
                        { "Surname", "TEXT(100)" }, { "Names", "TEXT(100)" }, { "FullName", "TEXT(255)" },
                        { "Gender", "TEXT(20)" }, { "PreferredName", "TEXT(50)" }, { "DOB", "DATETIME" },
                        { "HomeLanguage", "TEXT(50)" }, { "PlaceOfBirth", "TEXT(100)" }, { "Citizenship", "TEXT(50)" },
                        { "StudyPermitNo", "TEXT(50)" }, { "Religion", "TEXT(50)" }, { "LearnerCell", "TEXT(50)" },
                        { "HomeAddress", "MEMO" }, 
                        
                        // Academic / Previous
                        { "PrevSchool", "TEXT(100)" }, { "PrevHostel", "TEXT(100)" },
                        { "RefTeacher", "TEXT(100)" }, { "RefTeacherCell", "TEXT(50)" }, { "GradesRepeated", "TEXT(50)" },

                        // Siblings (1-4)
                        { "Sib1Name", "TEXT(100)" }, { "Sib1Grade", "TEXT(20)" },
                        { "Sib2Name", "TEXT(100)" }, { "Sib2Grade", "TEXT(20)" },
                        { "Sib3Name", "TEXT(100)" }, { "Sib3Grade", "TEXT(20)" },
                        { "Sib4Name", "TEXT(100)" }, { "Sib4Grade", "TEXT(20)" },

                        // Father
                        { "FatherName", "TEXT(100)" }, { "FatherID", "TEXT(50)" }, 
                        { "FatherOccupation", "TEXT(100)" }, { "FatherEmployer", "TEXT(100)" },
                        { "FatherHomePhone", "TEXT(50)" }, { "FatherWorkPhone", "TEXT(50)" }, { "FatherCell", "TEXT(50)" }, 
                        { "FatherFax", "TEXT(50)" }, { "FatherEmail", "TEXT(100)" },
                        { "FatherResAddress", "MEMO" }, { "FatherPostalAddress", "MEMO" },

                        // Mother
                        { "MotherName", "TEXT(100)" }, { "MotherID", "TEXT(50)" }, 
                        { "MotherOccupation", "TEXT(100)" }, { "MotherEmployer", "TEXT(100)" },
                        { "MotherHomePhone", "TEXT(50)" }, { "MotherWorkPhone", "TEXT(50)" }, { "MotherCell", "TEXT(50)" }, 
                        { "MotherFax", "TEXT(50)" }, { "MotherEmail", "TEXT(100)" },
                        { "MotherResAddress", "MEMO" }, { "MotherPostalAddress", "MEMO" },

                        // Relatives/Sign-Out (1-6)
                        { "Rel1Name", "TEXT(100)" }, { "Rel1ID", "TEXT(50)" }, { "Rel1Tel", "TEXT(50)" },
                        { "Rel2Name", "TEXT(100)" }, { "Rel2ID", "TEXT(50)" }, { "Rel2Tel", "TEXT(50)" },
                        { "Rel3Name", "TEXT(100)" }, { "Rel3ID", "TEXT(50)" }, { "Rel3Tel", "TEXT(50)" },
                        { "Rel4Name", "TEXT(100)" }, { "Rel4ID", "TEXT(50)" }, { "Rel4Tel", "TEXT(50)" },
                        { "Rel5Name", "TEXT(100)" }, { "Rel5ID", "TEXT(50)" }, { "Rel5Tel", "TEXT(50)" },
                        { "Rel6Name", "TEXT(100)" }, { "Rel6ID", "TEXT(50)" }, { "Rel6Tel", "TEXT(50)" },

                        // Medical
                        { "MedicalAidName", "TEXT(100)" }, { "MedicalAidNo", "TEXT(50)" }, { "MedicalMainMember", "TEXT(100)" },
                        { "AmbChoice1", "TEXT(100)" }, { "AmbChoice2", "TEXT(100)" },
                        { "HospChoice1", "TEXT(100)" }, { "HospChoice2", "TEXT(100)" },
                        { "BloodGroup", "TEXT(10)" }, { "ChronicMedication", "MEMO" },
                        { "MedicalHistory", "MEMO" }, // Allergies, Epilepsy, etc. combined or CSV
                        { "DoctorName", "TEXT(100)" }, { "DoctorTel", "TEXT(50)" },
                        { "DoctorDeclaredFit", "YESNO" }, { "DoctorDate", "DATETIME" },

                        // Documents (Checklist - Yes/No)
                        { "DocPassportPhoto", "YESNO" }, { "DocBirthCert", "YESNO" }, { "DocReportJune", "YESNO" },
                        { "DocReportDec", "YESNO" }, { "DocParentsID", "YESNO" }, { "DocMunicipal", "YESNO" },
                        { "DocEmployer", "YESNO" }, { "DocDoctorDecl", "YESNO" }, { "DocProofAccept", "YESNO" },
                        { "DocStudyPermit", "YESNO" }
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
                            catch (Exception e) { log.Add($"Error creating '{col.Key}': {e.Message}"); }
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

        // --- 2. GET LEARNER DETAILS (NEW) ---
        [HttpGet("details/{id}")]
        public IActionResult GetLearnerDetails(int id)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string sql = "SELECT * FROM [tbl_Learners] WHERE [LearnerID] = ?";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Helper to safe read
                                string S(string col) => reader[col]?.ToString() ?? "";
                                bool B(string col) => reader[col] != DBNull.Value && Convert.ToBoolean(reader[col]);
                                DateTime? D(string col) => reader[col] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader[col]) : null;

                                var data = new LearnerRegistrationModel
                                {
                                    // Basic
                                    LearnerID = Convert.ToInt32(reader["LearnerID"]),
                                    AdmissionNo = S("AdmissionNo"),
                                    Surname = S("Surname"),
                                    Names = S("Names"),
                                    Gender = S("Gender"),
                                    RoomId = reader["RoomID"] != DBNull.Value ? Convert.ToInt32(reader["RoomID"]) : 0,
                                    DOB = D("DOB") ?? DateTime.MinValue,
                                    Grade = reader["Grade"] != DBNull.Value ? Convert.ToInt32(reader["Grade"]) : 0,
                                    HomeLanguage = S("HomeLanguage"),
                                    PreferredName = S("PreferredName"),
                                    PlaceOfBirth = S("PlaceOfBirth"),
                                    Citizenship = S("Citizenship"),
                                    StudyPermitNo = S("StudyPermitNo"),
                                    Religion = S("Religion"),
                                    LearnerCell = S("LearnerCell"),
                                    HomeAddress = S("HomeAddress"),
                                    
                                    // Academic
                                    PrevSchool = S("PrevSchool"),
                                    PrevHostel = S("PrevHostel"),
                                    RefTeacher = S("RefTeacher"),
                                    RefTeacherCell = S("RefTeacherCell"),
                                    GradesRepeated = S("GradesRepeated"),

                                    // Siblings
                                    Sib1Name = S("Sib1Name"), Sib1Grade = S("Sib1Grade"),
                                    Sib2Name = S("Sib2Name"), Sib2Grade = S("Sib2Grade"),
                                    Sib3Name = S("Sib3Name"), Sib3Grade = S("Sib3Grade"),
                                    Sib4Name = S("Sib4Name"), Sib4Grade = S("Sib4Grade"),

                                    // Father
                                    FatherName = S("FatherName"), FatherID = S("FatherID"),
                                    FatherOccupation = S("FatherOccupation"), FatherEmployer = S("FatherEmployer"),
                                    FatherHomePhone = S("FatherHomePhone"), FatherWorkPhone = S("FatherWorkPhone"),
                                    FatherCell = S("FatherCell"), FatherFax = S("FatherFax"),
                                    FatherEmail = S("FatherEmail"), FatherResAddress = S("FatherResAddress"),
                                    FatherPostalAddress = S("FatherPostalAddress"),

                                    // Mother
                                    MotherName = S("MotherName"), MotherID = S("MotherID"),
                                    MotherOccupation = S("MotherOccupation"), MotherEmployer = S("MotherEmployer"),
                                    MotherHomePhone = S("MotherHomePhone"), MotherWorkPhone = S("MotherWorkPhone"),
                                    MotherCell = S("MotherCell"), MotherFax = S("MotherFax"),
                                    MotherEmail = S("MotherEmail"), MotherResAddress = S("MotherResAddress"),
                                    MotherPostalAddress = S("MotherPostalAddress"),

                                    // Relatives
                                    Rel1Name = S("Rel1Name"), Rel1ID = S("Rel1ID"), Rel1Tel = S("Rel1Tel"),
                                    Rel2Name = S("Rel2Name"), Rel2ID = S("Rel2ID"), Rel2Tel = S("Rel2Tel"),
                                    Rel3Name = S("Rel3Name"), Rel3ID = S("Rel3ID"), Rel3Tel = S("Rel3Tel"),
                                    Rel4Name = S("Rel4Name"), Rel4ID = S("Rel4ID"), Rel4Tel = S("Rel4Tel"),
                                    Rel5Name = S("Rel5Name"), Rel5ID = S("Rel5ID"), Rel5Tel = S("Rel5Tel"),
                                    Rel6Name = S("Rel6Name"), Rel6ID = S("Rel6ID"), Rel6Tel = S("Rel6Tel"),

                                    // Medical
                                    MedicalAidName = S("MedicalAidName"), MedicalAidNo = S("MedicalAidNo"),
                                    MedicalMainMember = S("MedicalMainMember"),
                                    AmbChoice1 = S("AmbChoice1"), AmbChoice2 = S("AmbChoice2"),
                                    HospChoice1 = S("HospChoice1"), HospChoice2 = S("HospChoice2"),
                                    BloodGroup = S("BloodGroup"), ChronicMedication = S("ChronicMedication"),
                                    MedicalHistory = S("MedicalHistory"),
                                    DoctorName = S("DoctorName"), DoctorTel = S("DoctorTel"),
                                    DoctorDeclaredFit = B("DoctorDeclaredFit"), DoctorDate = D("DoctorDate"),

                                    // Docs
                                    DocPassportPhoto = B("DocPassportPhoto"), DocBirthCert = B("DocBirthCert"),
                                    DocReportJune = B("DocReportJune"), DocReportDec = B("DocReportDec"),
                                    DocParentsID = B("DocParentsID"), DocMunicipal = B("DocMunicipal"),
                                    DocEmployer = B("DocEmployer"), DocDoctorDecl = B("DocDoctorDecl"),
                                    DocProofAccept = B("DocProofAccept"), DocStudyPermit = B("DocStudyPermit")
                                };
                                return Ok(data);
                            }
                        }
                    }
                }
                return NotFound(new { Message = "Learner not found" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Error: " + ex.Message }); }
        }


        // --- 3. GET ALL LEARNERS (UPDATED: Sorts by Block -> Room -> Surname) ---
        [HttpGet("list-all")]
        public IActionResult GetAllLearners()
        {
            var list = new List<object>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
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
                        // Optimization: Cache ordinals to avoid repeated dictionary lookups
                        int ordSurname = reader.GetOrdinal("Surname");
                        int ordNames = reader.GetOrdinal("Names");
                        int ordFullName = reader.GetOrdinal("FullName");
                        int ordRoomNumber = reader.GetOrdinal("RoomNumber");
                        int ordBlockName = reader.GetOrdinal("BlockName");
                        int ordLearnerID = reader.GetOrdinal("LearnerID");
                        int ordAdmissionNo = reader.GetOrdinal("AdmissionNo");
                        int ordGrade = reader.GetOrdinal("Grade");
                        int ordGender = reader.GetOrdinal("Gender");
                        int ordRoomID = reader.GetOrdinal("RoomID");

                        while (reader.Read())
                        {
                            // Name Construction
                            string sName = reader[ordSurname]?.ToString() ?? "";
                            string fName = reader[ordNames]?.ToString() ?? "";
                            string dbFull = reader[ordFullName]?.ToString();
                            string displayName = !string.IsNullOrWhiteSpace(dbFull) ? dbFull : $"{sName} {fName}".Trim();
                            if (string.IsNullOrWhiteSpace(displayName)) displayName = "Unknown";

                            // Room Construction (Block - Room)
                            string rNum = reader[ordRoomNumber]?.ToString();
                            string bName = reader[ordBlockName]?.ToString();
                            string displayRoom = "Unassigned";

                            if (!string.IsNullOrEmpty(rNum))
                            {
                                // If we have a block name, combine them. E.g., "Block A - 101"
                                displayRoom = !string.IsNullOrEmpty(bName) ? $"{bName} - {rNum}" : rNum;
                            }

                            list.Add(new
                            {
                                id = reader[ordLearnerID],
                                adNo = reader[ordAdmissionNo]?.ToString() ?? "N/A",
                                name = displayName,
                                surname = sName,
                                names = fName,
                                grade = reader[ordGrade]?.ToString() ?? "-",
                                gender = reader[ordGender]?.ToString() ?? "-",
                                roomId = reader[ordRoomID] != DBNull.Value ? reader[ordRoomID] : 0,
                                room = displayRoom // Sends "Block A - 101" to frontend
                            });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Server Error: " + ex.Message }); }
        }

        // --- 4. REGISTER LEARNER (Simplified for now - Full save is in UPDATE) ---
        [HttpPost("register")]
        public IActionResult RegisterLearner([FromBody] LearnerRegistrationModel req)
        {
            // For now, we keep the simple register logic because the full form is huge.
            // The user typically registers with basics then edits details.
            // But we will add the basics + room assignment.
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string surname = req.Surname ?? "";
                    string names = req.Names ?? "";
                    string fullName = $"{surname} {names}".Trim();

                    string sql = @"INSERT INTO [tbl_Learners] 
                    ([AdmissionNo], [Surname], [Names], [FullName], [Gender], [RoomID], [Grade], [LearnerCell], [DOB]) 
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", req.AdmissionNo ?? "");
                        cmd.Parameters.AddWithValue("?", surname);
                        cmd.Parameters.AddWithValue("?", names);
                        cmd.Parameters.AddWithValue("?", fullName);
                        cmd.Parameters.AddWithValue("?", req.Gender ?? "Male");
                        cmd.Parameters.AddWithValue("?", req.RoomId);
                        cmd.Parameters.AddWithValue("?", req.Grade);
                        cmd.Parameters.AddWithValue("?", req.LearnerCell ?? "");
                        cmd.Parameters.AddWithValue("?", req.DOB);

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
                return Ok(new { Message = "Learner registered successfully! Open details to add full info." });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Error: " + ex.Message }); }
        }

        // --- 5. DELETE LEARNER ---
        [HttpDelete("delete/{id}")]
        public IActionResult DeleteLearner(int id)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
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

        // --- 6. UPDATE LEARNER (THE BIG ONE) ---
        [HttpPut("update/{id}")]
        public IActionResult UpdateLearner(int id, [FromBody] LearnerRegistrationModel r)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string fullName = $"{r.Surname} {r.Names}".Trim();

                    string sql = @"UPDATE [tbl_Learners] SET 
                        [AdmissionNo]=?, [Surname]=?, [Names]=?, [FullName]=?, [Gender]=?, [RoomID]=?, [DOB]=?, [Grade]=?,
                        [HomeLanguage]=?, [PreferredName]=?, [PlaceOfBirth]=?, [Citizenship]=?, [StudyPermitNo]=?, [Religion]=?, [LearnerCell]=?, [HomeAddress]=?,
                        [PrevSchool]=?, [PrevHostel]=?, [RefTeacher]=?, [RefTeacherCell]=?, [GradesRepeated]=?,
                        [Sib1Name]=?, [Sib1Grade]=?, [Sib2Name]=?, [Sib2Grade]=?, [Sib3Name]=?, [Sib3Grade]=?, [Sib4Name]=?, [Sib4Grade]=?,
                        [FatherName]=?, [FatherID]=?, [FatherOccupation]=?, [FatherEmployer]=?, [FatherHomePhone]=?, [FatherWorkPhone]=?, [FatherCell]=?, [FatherFax]=?, [FatherEmail]=?, [FatherResAddress]=?, [FatherPostalAddress]=?,
                        [MotherName]=?, [MotherID]=?, [MotherOccupation]=?, [MotherEmployer]=?, [MotherHomePhone]=?, [MotherWorkPhone]=?, [MotherCell]=?, [MotherFax]=?, [MotherEmail]=?, [MotherResAddress]=?, [MotherPostalAddress]=?,
                        [Rel1Name]=?, [Rel1ID]=?, [Rel1Tel]=?,
                        [Rel2Name]=?, [Rel2ID]=?, [Rel2Tel]=?,
                        [Rel3Name]=?, [Rel3ID]=?, [Rel3Tel]=?,
                        [Rel4Name]=?, [Rel4ID]=?, [Rel4Tel]=?,
                        [Rel5Name]=?, [Rel5ID]=?, [Rel5Tel]=?,
                        [Rel6Name]=?, [Rel6ID]=?, [Rel6Tel]=?,
                        [MedicalAidName]=?, [MedicalAidNo]=?, [MedicalMainMember]=?,
                        [AmbChoice1]=?, [AmbChoice2]=?, [HospChoice1]=?, [HospChoice2]=?,
                        [BloodGroup]=?, [ChronicMedication]=?, [MedicalHistory]=?,
                        [DoctorName]=?, [DoctorTel]=?, [DoctorDeclaredFit]=?, [DoctorDate]=?,
                        [DocPassportPhoto]=?, [DocBirthCert]=?, [DocReportJune]=?, [DocReportDec]=?, [DocParentsID]=?, [DocMunicipal]=?, [DocEmployer]=?, [DocDoctorDecl]=?, [DocProofAccept]=?, [DocStudyPermit]=?
                        WHERE [LearnerID]=?";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        // Safe Add Params Helper
                        void P(object val) => cmd.Parameters.AddWithValue("?", val ?? DBNull.Value);
                        void S(string val) => cmd.Parameters.AddWithValue("?", val ?? "");

                        S(r.AdmissionNo); S(r.Surname); S(r.Names); S(fullName); S(r.Gender); P(r.RoomId); P(r.DOB); P(r.Grade);
                        S(r.HomeLanguage); S(r.PreferredName); S(r.PlaceOfBirth); S(r.Citizenship); S(r.StudyPermitNo); S(r.Religion); S(r.LearnerCell); S(r.HomeAddress);
                        S(r.PrevSchool); S(r.PrevHostel); S(r.RefTeacher); S(r.RefTeacherCell); S(r.GradesRepeated);
                        
                        S(r.Sib1Name); S(r.Sib1Grade); S(r.Sib2Name); S(r.Sib2Grade); S(r.Sib3Name); S(r.Sib3Grade); S(r.Sib4Name); S(r.Sib4Grade);
                        
                        S(r.FatherName); S(r.FatherID); S(r.FatherOccupation); S(r.FatherEmployer); S(r.FatherHomePhone); S(r.FatherWorkPhone); S(r.FatherCell); S(r.FatherFax); S(r.FatherEmail); S(r.FatherResAddress); S(r.FatherPostalAddress);
                        S(r.MotherName); S(r.MotherID); S(r.MotherOccupation); S(r.MotherEmployer); S(r.MotherHomePhone); S(r.MotherWorkPhone); S(r.MotherCell); S(r.MotherFax); S(r.MotherEmail); S(r.MotherResAddress); S(r.MotherPostalAddress);
                        
                        S(r.Rel1Name); S(r.Rel1ID); S(r.Rel1Tel);
                        S(r.Rel2Name); S(r.Rel2ID); S(r.Rel2Tel);
                        S(r.Rel3Name); S(r.Rel3ID); S(r.Rel3Tel);
                        S(r.Rel4Name); S(r.Rel4ID); S(r.Rel4Tel);
                        S(r.Rel5Name); S(r.Rel5ID); S(r.Rel5Tel);
                        S(r.Rel6Name); S(r.Rel6ID); S(r.Rel6Tel);
                        
                        S(r.MedicalAidName); S(r.MedicalAidNo); S(r.MedicalMainMember);
                        S(r.AmbChoice1); S(r.AmbChoice2); S(r.HospChoice1); S(r.HospChoice2);
                        S(r.BloodGroup); S(r.ChronicMedication); S(r.MedicalHistory);
                        S(r.DoctorName); S(r.DoctorTel); P(r.DoctorDeclaredFit); P(r.DoctorDate);
                        
                        P(r.DocPassportPhoto); P(r.DocBirthCert); P(r.DocReportJune); P(r.DocReportDec); P(r.DocParentsID);
                        P(r.DocMunicipal); P(r.DocEmployer); P(r.DocDoctorDecl); P(r.DocProofAccept); P(r.DocStudyPermit);

                        P(id); // Where Clause

                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Learner profile updated successfully!" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Error: " + ex.Message }); }
        }
    }

    public class LearnerRegistrationModel
    {
        public int LearnerID { get; set; }
        // Basic
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
        
        // Academic
        public string PrevSchool { get; set; }
        public string PrevHostel { get; set; }
        public string RefTeacher { get; set; }
        public string RefTeacherCell { get; set; }
        public string GradesRepeated { get; set; }
        
        // Siblings
        public string Sib1Name { get; set; } public string Sib1Grade { get; set; }
        public string Sib2Name { get; set; } public string Sib2Grade { get; set; }
        public string Sib3Name { get; set; } public string Sib3Grade { get; set; }
        public string Sib4Name { get; set; } public string Sib4Grade { get; set; }

        // Father
        public string FatherName { get; set; }
        public string FatherID { get; set; }
        public string FatherOccupation { get; set; }
        public string FatherEmployer { get; set; }
        public string FatherHomePhone { get; set; }
        public string FatherWorkPhone { get; set; }
        public string FatherCell { get; set; }
        public string FatherFax { get; set; }
        public string FatherEmail { get; set; }
        public string FatherResAddress { get; set; }
        public string FatherPostalAddress { get; set; }

        // Mother
        public string MotherName { get; set; }
        public string MotherID { get; set; }
        public string MotherOccupation { get; set; }
        public string MotherEmployer { get; set; }
        public string MotherHomePhone { get; set; }
        public string MotherWorkPhone { get; set; }
        public string MotherCell { get; set; }
        public string MotherFax { get; set; }
        public string MotherEmail { get; set; }
        public string MotherResAddress { get; set; }
        public string MotherPostalAddress { get; set; }

        // Relatives
        public string Rel1Name { get; set; } public string Rel1ID { get; set; } public string Rel1Tel { get; set; }
        public string Rel2Name { get; set; } public string Rel2ID { get; set; } public string Rel2Tel { get; set; }
        public string Rel3Name { get; set; } public string Rel3ID { get; set; } public string Rel3Tel { get; set; }
        public string Rel4Name { get; set; } public string Rel4ID { get; set; } public string Rel4Tel { get; set; }
        public string Rel5Name { get; set; } public string Rel5ID { get; set; } public string Rel5Tel { get; set; }
        public string Rel6Name { get; set; } public string Rel6ID { get; set; } public string Rel6Tel { get; set; }

        // Medical
        public string MedicalAidName { get; set; }
        public string MedicalAidNo { get; set; }
        public string MedicalMainMember { get; set; }
        public string AmbChoice1 { get; set; }
        public string AmbChoice2 { get; set; }
        public string HospChoice1 { get; set; }
        public string HospChoice2 { get; set; }
        public string BloodGroup { get; set; }
        public string ChronicMedication { get; set; }
        public string MedicalHistory { get; set; }
        public string DoctorName { get; set; }
        public string DoctorTel { get; set; }
        public bool DoctorDeclaredFit { get; set; }
        public DateTime? DoctorDate { get; set; }
        public string EmergencyContact { get; set; } // Deprecated in favor of Relatives but kept for safety

        // Docs
        public bool DocPassportPhoto { get; set; }
        public bool DocBirthCert { get; set; }
        public bool DocReportJune { get; set; }
        public bool DocReportDec { get; set; }
        public bool DocParentsID { get; set; }
        public bool DocMunicipal { get; set; }
        public bool DocEmployer { get; set; }
        public bool DocDoctorDecl { get; set; }
        public bool DocProofAccept { get; set; }
        public bool DocStudyPermit { get; set; }
    }
}
