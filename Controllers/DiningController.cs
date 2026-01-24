using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Runtime.Versioning;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class DiningController : ControllerBase
    {
        [HttpGet("log")]
        public IActionResult GetDiningLog()
        {
            var logs = new List<object>();
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    EnsureTable(conn);
                    string sql = "SELECT * FROM tbl_DiningLog ORDER BY DateLogged DESC";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        // ⚡ Bolt: Cache ordinals to avoid repeated string lookups inside the loop
                        int ordId = reader.GetOrdinal("ID");
                        int ordSupplier = reader.GetOrdinal("Supplier");
                        int ordItem = reader.GetOrdinal("Item");
                        int ordQuantity = reader.GetOrdinal("Quantity");
                        int ordReceivedBy = reader.GetOrdinal("ReceivedBy");
                        int ordDate = reader.GetOrdinal("DateLogged");

                        while (reader.Read())
                        {
                            logs.Add(new
                            {
                                Id = reader.GetValue(ordId),
                                Supplier = reader.IsDBNull(ordSupplier) ? string.Empty : reader.GetValue(ordSupplier).ToString(),
                                Item = reader.IsDBNull(ordItem) ? string.Empty : reader.GetValue(ordItem).ToString(),
                                Quantity = reader.IsDBNull(ordQuantity) ? string.Empty : reader.GetValue(ordQuantity).ToString(),
                                ReceivedBy = reader.IsDBNull(ordReceivedBy) ? string.Empty : reader.GetValue(ordReceivedBy).ToString(),
                                Date = reader.IsDBNull(ordDate) ? string.Empty : reader.GetDateTime(ordDate).ToString("yyyy-MM-dd HH:mm")
                            });
                        }
                    }
                }
                return Ok(logs);
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost("add")]
        public IActionResult AddLog([FromBody] DiningItem item)
        {
            try
            {
                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    EnsureTable(conn);
                    string sql = "INSERT INTO tbl_DiningLog (Supplier, Item, Quantity, ReceivedBy, DateLogged) VALUES (?, ?, ?, ?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", item.Supplier);
                        cmd.Parameters.AddWithValue("?", item.Item);
                        cmd.Parameters.AddWithValue("?", item.Quantity);
                        cmd.Parameters.AddWithValue("?", item.ReceivedBy);
                        cmd.Parameters.AddWithValue("?", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Logged" });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        private void EnsureTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_DiningLog", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                using (var cmd = new OleDbCommand("CREATE TABLE tbl_DiningLog (ID AUTOINCREMENT PRIMARY KEY, Supplier TEXT(100), Item TEXT(100), Quantity TEXT(50), ReceivedBy TEXT(100), DateLogged DATETIME)", conn))
                    cmd.ExecuteNonQuery();
            }
        }
    }

    public class DiningItem
    {
        public string Supplier { get; set; }
        public string Item { get; set; }
        public string Quantity { get; set; }
        public string ReceivedBy { get; set; }
    }
}