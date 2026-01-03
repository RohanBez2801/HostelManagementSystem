using Microsoft.AspNetCore.Mvc;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System;
using System.IO;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class RoomController : ControllerBase
    {
        [HttpGet("all")]
        public IActionResult GetAllRooms()
        {
            var rooms = new List<object>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string sql = @"SELECT r.RoomID, r.RoomNumber, r.BlockID, b.BlockName, b.BlockGender, r.Capacity, 
                               (SELECT COUNT(*) FROM tbl_Learners l WHERE l.RoomID = r.RoomID) as RealOccupancy
                        FROM tbl_Rooms r LEFT JOIN tbl_Blocks b ON r.BlockID = b.BlockID ORDER BY r.RoomNumber ASC";
                    using (var cmd = new OleDbCommand(sql, conn)) using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rooms.Add(new
                            {
                                Id = reader["RoomID"],
                                Number = reader["RoomNumber"]?.ToString(),
                                Block = reader["BlockName"]?.ToString(),
                                BlockGender = reader["BlockGender"]?.ToString() ?? "Mixed",
                                Capacity = reader["Capacity"],
                                Occupied = reader["RealOccupancy"],
                                Available = Convert.ToInt32(reader["Capacity"]) - Convert.ToInt32(reader["RealOccupancy"])
                            });
                        }
                    }
                }
                return Ok(rooms);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpGet("available")]
        public IActionResult GetAvailableRooms()
        {
            var rooms = new List<object>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string sql = @"SELECT r.RoomID, r.RoomNumber, b.BlockName, b.BlockGender, r.Capacity, 
                               (SELECT COUNT(*) FROM tbl_Learners l WHERE l.RoomID = r.RoomID) as RealOccupancy
                        FROM tbl_Rooms r LEFT JOIN tbl_Blocks b ON r.BlockID = b.BlockID ORDER BY r.RoomNumber ASC";
                    using (var cmd = new OleDbCommand(sql, conn)) using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int cap = Convert.ToInt32(reader["Capacity"]);
                            int occ = Convert.ToInt32(reader["RealOccupancy"]);
                            if (occ < cap)
                            {
                                rooms.Add(new
                                {
                                    Id = reader["RoomID"],
                                    Number = reader["RoomNumber"]?.ToString(),
                                    Block = reader["BlockName"]?.ToString(),
                                    BlockGender = reader["BlockGender"]?.ToString() ?? "Mixed",
                                    Available = cap - occ
                                });
                            }
                        }
                    }
                }
                return Ok(rooms);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpGet("blocks")]
        public IActionResult GetAllBlocks()
        {
            var blocks = new List<object>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string sql = "SELECT BlockID, BlockName, BlockGender FROM tbl_Blocks ORDER BY BlockName ASC";
                    using (var cmd = new OleDbCommand(sql, conn)) using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            blocks.Add(new { BlockID = reader["BlockID"], BlockName = reader["BlockName"], BlockGender = reader["BlockGender"] });
                        }
                    }
                }
                return Ok(blocks);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("add")]
        public IActionResult AddRoom([FromBody] RoomModel room)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string sql = "INSERT INTO [tbl_Rooms] ([RoomNumber], [BlockID], [Capacity], [CurrentOccupancy]) VALUES (?, ?, ?, 0)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", room.RoomNumber);
                        cmd.Parameters.AddWithValue("?", room.BlockID);
                        cmd.Parameters.AddWithValue("?", room.Capacity);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Room Added" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("addBlock")]
        public IActionResult AddBlock([FromBody] BlockModel block)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string sql = "INSERT INTO [tbl_Blocks] ([BlockName], [BlockGender]) VALUES (?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", block.BlockName);
                        cmd.Parameters.AddWithValue("?", block.BlockGender ?? "Mixed");
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Block Added" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // 6. GET ROOM DETAILS (Fixed)
        [HttpGet("details/{roomId}")]
        public IActionResult GetRoomDetails(int roomId)
        {
            var occupants = new List<object>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    // Fixed SQL: Select separate name columns
                    string sql = "SELECT LearnerID, Surname, Names, Grade FROM tbl_Learners WHERE RoomID = ?";

                    OleDbCommand cmd = new OleDbCommand(sql, conn);
                    cmd.Parameters.AddWithValue("?", roomId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string sName = reader["Surname"]?.ToString() ?? "";
                            string fName = reader["Names"]?.ToString() ?? "";

                            occupants.Add(new
                            {
                                LearnerID = reader["LearnerID"],
                                Name = fName,
                                Surname = sName,
                                Grade = reader["Grade"]?.ToString() ?? "-"
                            });
                        }
                    }
                }
                return Ok(occupants);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error: " + ex.Message });
            }
        }
    }

    public class RoomModel { 
        public string RoomNumber { get; set; } 
        public int BlockID { get; set; } 
        public string BlockName { get; set; } 
        public int Capacity { get; set; } 
    }
    public class BlockModel { 
        public string BlockName { get; set; } 
        public string BlockGender { get; set; } 
    }
}