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
    public class InventoryController : ControllerBase
    {
        [HttpGet("all")]
        public IActionResult GetAllInventory()
        {
            var items = new List<object>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    EnsureInventoryTable(conn);
                    // Left Join with Rooms to get Room Number if assigned to a room
                    string sql = @"SELECT i.InventoryID, i.ItemName, i.Category, i.Quantity, i.Condition, r.RoomNumber
                                   FROM tbl_Inventory i
                                   LEFT JOIN tbl_Rooms r ON i.RoomID = r.RoomID
                                   ORDER BY i.ItemName ASC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        // ⚡ Bolt: Cache ordinals to avoid repeated string lookups (Performance Optimization)
                        int ordId = reader.GetOrdinal("InventoryID");
                        int ordName = reader.GetOrdinal("ItemName");
                        int ordCategory = reader.GetOrdinal("Category");
                        int ordQuantity = reader.GetOrdinal("Quantity");
                        int ordCondition = reader.GetOrdinal("Condition");
                        int ordRoom = reader.GetOrdinal("RoomNumber");

                        while (reader.Read())
                        {
                            items.Add(new
                            {
                                Id = Convert.ToInt32(reader.GetValue(ordId)),
                                Name = reader.IsDBNull(ordName) ? "" : reader.GetValue(ordName).ToString(),
                                Category = reader.IsDBNull(ordCategory) ? "" : reader.GetValue(ordCategory).ToString(),
                                Quantity = Convert.ToInt32(reader.GetValue(ordQuantity)),
                                Condition = reader.IsDBNull(ordCondition) ? "" : reader.GetValue(ordCondition).ToString(),
                                Room = reader.IsDBNull(ordRoom) ? "General Store" : reader.GetValue(ordRoom).ToString()
                            });
                        }
                    }
                }
                return Ok(items);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("add")]
        public IActionResult AddInventory([FromBody] InventoryItem item)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    EnsureInventoryTable(conn);
                    string sql = "INSERT INTO tbl_Inventory (ItemName, Category, Quantity, Condition, RoomID) VALUES (?, ?, ?, ?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", item.Name);
                        cmd.Parameters.AddWithValue("?", item.Category ?? "General");
                        cmd.Parameters.AddWithValue("?", item.Quantity);
                        cmd.Parameters.AddWithValue("?", item.Condition ?? "Good");
                        // If RoomID is 0 or null, we treat it as null in DB (General Store)
                        if (item.RoomId > 0)
                            cmd.Parameters.AddWithValue("?", item.RoomId);
                        else
                            cmd.Parameters.AddWithValue("?", DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Inventory item added" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteInventory(int id)
        {
             try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    string sql = "DELETE FROM tbl_Inventory WHERE InventoryID = ?";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Item deleted" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        private void EnsureInventoryTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Inventory", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string createSql = @"CREATE TABLE tbl_Inventory (
                    InventoryID AUTOINCREMENT PRIMARY KEY,
                    ItemName TEXT(100),
                    Category TEXT(50),
                    Quantity INT,
                    Condition TEXT(50),
                    RoomID INT
                )";
                using (var cmd = new OleDbCommand(createSql, conn)) cmd.ExecuteNonQuery();
            }
        }
    }

    public class InventoryItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public string Condition { get; set; }
        public int RoomId { get; set; }
    }
}
