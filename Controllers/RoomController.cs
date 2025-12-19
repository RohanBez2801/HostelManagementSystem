using Microsoft.AspNetCore.Mvc;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class RoomController : ControllerBase
    {
        // Your specific database path
        private readonly string _connString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\rouxn\source\repos\HostelManagementSystem\Data\HostelDb.accdb;";

        // 1. GET ALL ROOMS (For the Visual Mapping Panel)
        [HttpGet("all")]
        public IActionResult GetAllRooms()
        {
            var rooms = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    string sql = "SELECT * FROM tbl_Rooms ORDER BY RoomNumber ASC";
                    OleDbCommand cmd = new OleDbCommand(sql, conn);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rooms.Add(new
                            {
                                Id = reader["RoomID"],
                                Number = reader["RoomNumber"],
                                Block = reader["BlockName"],
                                Capacity = Convert.ToInt32(reader["Capacity"]),
                                Occupied = Convert.ToInt32(reader["CurrentOccupancy"]),
                                // Calculation for the frontend progress bar
                                Available = Convert.ToInt32(reader["Capacity"]) - Convert.ToInt32(reader["CurrentOccupancy"])
                            });
                        }
                    }
                }
                return Ok(rooms);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Database Error: " + ex.Message });
            }
        }

        // 2. GET AVAILABLE ROOMS ONLY (For the Registration Dropdown)
        [HttpGet("available")]
        public IActionResult GetAvailableRooms()
        {
            var availableRooms = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    // Logic: Only rooms where there is still space
                    string sql = "SELECT * FROM tbl_Rooms WHERE CurrentOccupancy < Capacity ORDER BY RoomNumber ASC";
                    OleDbCommand cmd = new OleDbCommand(sql, conn);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            availableRooms.Add(new
                            {
                                Id = reader["RoomID"],
                                Number = reader["RoomNumber"],
                                Block = reader["BlockName"],
                                Available = Convert.ToInt32(reader["Capacity"]) - Convert.ToInt32(reader["CurrentOccupancy"])
                            });
                        }
                    }
                }
                return Ok(availableRooms);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Database Error: " + ex.Message });
            }
        }

        // 3. POST: ADD NEW ROOM (For the 'Add Room' Button)
        [HttpPost("add")]
        public IActionResult AddRoom([FromBody] RoomModel room)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    string sql = "INSERT INTO tbl_Rooms (RoomNumber, BlockName, Capacity, CurrentOccupancy) VALUES (?, ?, ?, 0)";
                    OleDbCommand cmd = new OleDbCommand(sql, conn);
                    cmd.Parameters.AddWithValue("?", room.RoomNumber);
                    cmd.Parameters.AddWithValue("?", room.BlockName);
                    cmd.Parameters.AddWithValue("?", room.Capacity);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return Ok(new { Message = "New Room added to the hostel successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error saving room: " + ex.Message });
            }
        }
    }

    // Data Transfer Object (DTO) for adding rooms
    public class RoomModel
    {
        public string RoomNumber { get; set; }
        public string BlockName { get; set; }
        public int Capacity { get; set; }
    }
}