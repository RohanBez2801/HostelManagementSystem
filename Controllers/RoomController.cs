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
                        // Optimization: Cache ordinals to avoid string-based lookups in loop
                        int ordRoomID = reader.GetOrdinal("RoomID");
                        int ordRoomNumber = reader.GetOrdinal("RoomNumber");
                        int ordBlockName = reader.GetOrdinal("BlockName");
                        int ordBlockGender = reader.GetOrdinal("BlockGender");
                        int ordCapacity = reader.GetOrdinal("Capacity");
                        int ordRealOccupancy = reader.GetOrdinal("RealOccupancy");

                        while (reader.Read())
                        {
                            // Use Convert.ToInt32(GetValue) for robust casting (e.g. Int16/Byte -> Int32)
                            int capacity = Convert.ToInt32(reader.GetValue(ordCapacity));
                            int occupied = Convert.ToInt32(reader.GetValue(ordRealOccupancy));

                            rooms.Add(new
                            {
                                Id = reader.GetValue(ordRoomID),
                                // Use GetValue().ToString() to handle both string and numeric types safely
                                Number = reader.IsDBNull(ordRoomNumber) ? "" : reader.GetValue(ordRoomNumber).ToString(),
                                Block = reader.IsDBNull(ordBlockName) ? "" : reader.GetValue(ordBlockName).ToString(),
                                BlockGender = reader.IsDBNull(ordBlockGender) ? "Mixed" : reader.GetValue(ordBlockGender).ToString(),
                                Capacity = capacity,
                                Occupied = occupied,
                                Available = capacity - occupied
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
                        // Optimization: Cache ordinals to avoid string-based lookups in loop
                        int ordRoomID = reader.GetOrdinal("RoomID");
                        int ordRoomNumber = reader.GetOrdinal("RoomNumber");
                        int ordBlockName = reader.GetOrdinal("BlockName");
                        int ordBlockGender = reader.GetOrdinal("BlockGender");
                        int ordCapacity = reader.GetOrdinal("Capacity");
                        int ordRealOccupancy = reader.GetOrdinal("RealOccupancy");

                        while (reader.Read())
                        {
                            int cap = Convert.ToInt32(reader.GetValue(ordCapacity));
                            int occ = Convert.ToInt32(reader.GetValue(ordRealOccupancy));

                            if (occ < cap)
                            {
                                rooms.Add(new
                                {
                                    Id = reader.GetValue(ordRoomID),
                                    Number = reader.IsDBNull(ordRoomNumber) ? "" : reader.GetValue(ordRoomNumber).ToString(),
                                    Block = reader.IsDBNull(ordBlockName) ? "" : reader.GetValue(ordBlockName).ToString(),
                                    BlockGender = reader.IsDBNull(ordBlockGender) ? "Mixed" : reader.GetValue(ordBlockGender).ToString(),
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
                        // Optimization: Cache ordinals to avoid string-based lookups in loop
                        int ordBlockID = reader.GetOrdinal("BlockID");
                        int ordBlockName = reader.GetOrdinal("BlockName");
                        int ordBlockGender = reader.GetOrdinal("BlockGender");

                        while (reader.Read())
                        {
                            blocks.Add(new {
                                BlockID = reader.GetValue(ordBlockID),
                                BlockName = reader.IsDBNull(ordBlockName) ? "" : reader.GetValue(ordBlockName).ToString(),
                                BlockGender = reader.IsDBNull(ordBlockGender) ? "Mixed" : reader.GetValue(ordBlockGender).ToString()
                            });
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
                        // Optimization: Cache ordinals to avoid string-based lookups in loop
                        int ordLearnerID = reader.GetOrdinal("LearnerID");
                        int ordSurname = reader.GetOrdinal("Surname");
                        int ordNames = reader.GetOrdinal("Names");
                        int ordGrade = reader.GetOrdinal("Grade");

                        while (reader.Read())
                        {
                            string sName = reader.IsDBNull(ordSurname) ? "" : reader.GetValue(ordSurname).ToString();
                            string fName = reader.IsDBNull(ordNames) ? "" : reader.GetValue(ordNames).ToString();

                            occupants.Add(new
                            {
                                LearnerID = reader.GetValue(ordLearnerID),
                                Name = fName,
                                Surname = sName,
                                Grade = reader.IsDBNull(ordGrade) ? "-" : reader.GetValue(ordGrade).ToString()
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