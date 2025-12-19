using Microsoft.AspNetCore.Mvc;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class LearnerController : ControllerBase
    {
        private readonly string _connString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\rouxn\source\repos\HostelManagementSystem\Data\HostelDb.accdb;";

        [HttpPost("register")]
        public IActionResult RegisterLearner([FromBody] LearnerRequest request)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();

                    // 1. Insert the Learner
                    string insertSql = "INSERT INTO tbl_Learners (AdmissionNo, FullName, Grade, RoomID) VALUES (?, ?, ?, ?)";
                    using (OleDbCommand cmd = new OleDbCommand(insertSql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", request.AdmissionNo);
                        cmd.Parameters.AddWithValue("?", request.FullName);
                        cmd.Parameters.AddWithValue("?", request.Grade);
                        cmd.Parameters.AddWithValue("?", request.RoomId);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Update the Room Occupancy (Increment by 1)
                    string updateRoomSql = "UPDATE tbl_Rooms SET CurrentOccupancy = CurrentOccupancy + 1 WHERE RoomID = ?";
                    using (OleDbCommand cmdUpdate = new OleDbCommand(updateRoomSql, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("?", request.RoomId);
                        cmdUpdate.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Learner registered and room allocated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error: " + ex.Message });
            }
        }

        // 2. GET LEARNERS BY ROOM ID
        [HttpGet("by-room/{roomId}")]
        public IActionResult GetLearnersByRoom(int roomId)
        {
            var learners = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    // We join tbl_Learners with the room ID to get specific student details
                    string sql = "SELECT FullName, AdmissionNo, Grade FROM tbl_Learners WHERE RoomID = ?";
                    OleDbCommand cmd = new OleDbCommand(sql, conn);
                    cmd.Parameters.AddWithValue("?", roomId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            learners.Add(new
                            {
                                Name = reader["FullName"],
                                AdNo = reader["AdmissionNo"],
                                Grade = reader["Grade"]
                            });
                        }
                    }
                }
                return Ok(learners);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        // 3. GET ALL LEARNERS
        [HttpGet("list-all")]
        public IActionResult GetAllLearners()
        {
            var list = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    // JOIN is used here to show the Room Number instead of just the Room ID
                    string sql = @"SELECT L.LearnerID, L.AdmissionNo, L.FullName, L.Grade, R.RoomNumber 
                           FROM tbl_Learners L 
                           LEFT JOIN tbl_Rooms R ON L.RoomID = R.RoomID 
                           ORDER BY L.FullName ASC";

                    OleDbCommand cmd = new OleDbCommand(sql, conn);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                Id = reader["LearnerID"],
                                AdNo = reader["AdmissionNo"],
                                Name = reader["FullName"],
                                Grade = reader["Grade"],
                                Room = reader["RoomNumber"]?.ToString() ?? "Unassigned"
                            });
                        }
                    }
                }
                return Ok(list);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // 4. DELETE LEARNER
        [HttpDelete("delete/{id}")]
        public IActionResult DeleteLearner(int id)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();

                    // 1. Get the RoomID first so we know which room to decrease the count for
                    int roomId = -1;
                    string findSql = "SELECT RoomID FROM tbl_Learners WHERE LearnerID = ?";
                    using (OleDbCommand cmdFind = new OleDbCommand(findSql, conn))
                    {
                        cmdFind.Parameters.AddWithValue("?", id);
                        object result = cmdFind.ExecuteScalar();
                        if (result == null) return NotFound("Learner not found.");
                        roomId = Convert.ToInt32(result);
                    }

                    // 2. Delete the Learner
                    string deleteSql = "DELETE FROM tbl_Learners WHERE LearnerID = ?";
                    using (OleDbCommand cmdDel = new OleDbCommand(deleteSql, conn))
                    {
                        cmdDel.Parameters.AddWithValue("?", id);
                        cmdDel.ExecuteNonQuery();
                    }

                    // 3. Decrement the Room Occupancy (CurrentOccupancy - 1)
                    if (roomId != -1)
                    {
                        string updateRoomSql = "UPDATE tbl_Rooms SET CurrentOccupancy = CurrentOccupancy - 1 WHERE RoomID = ? AND CurrentOccupancy > 0";
                        using (OleDbCommand cmdUpdate = new OleDbCommand(updateRoomSql, conn))
                        {
                            cmdUpdate.Parameters.AddWithValue("?", roomId);
                            cmdUpdate.ExecuteNonQuery();
                        }
                    }
                }
                return Ok(new { Message = "Learner removed and room capacity updated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error: " + ex.Message });
            }
        }
    }

    public class LearnerRequest
    {
        public string AdmissionNo { get; set; }
        public string FullName { get; set; }
        public string Grade { get; set; }
        public int RoomId { get; set; }
    }
}