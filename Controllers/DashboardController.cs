using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.IO;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class DashboardController : ControllerBase
    {
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            try
            {
                int totalStudents = 0;
                int totalCapacity = 0;
                int pendingMaintenance = 0;
                int lowStockItems = 0;

                using (var conn = Helpers.DbHelper.GetConnection())
                {
                    // 1. Total Students
                    using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM tbl_Learners", conn))
                        totalStudents = (int)cmd.ExecuteScalar();

                    // 2. Total Capacity & Occupancy (Occupancy is implicit via learners count, but capacity is explicit)
                    using (var cmd = new OleDbCommand("SELECT SUM(Capacity) FROM tbl_Rooms", conn))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != DBNull.Value) totalCapacity = Convert.ToInt32(result);
                    }

                    // 3. Pending Maintenance
                    // Need to ensure table exists first or wrap in try-catch if it might not
                    try {
                        using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM tbl_Maintenance WHERE Status = 'Pending'", conn))
                            pendingMaintenance = (int)cmd.ExecuteScalar();
                    } catch {}

                    // 4. Low Stock (< 5)
                    try {
                        using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM tbl_Inventory WHERE Quantity < 5", conn))
                            lowStockItems = (int)cmd.ExecuteScalar();
                    } catch {}
                }

                return Ok(new
                {
                    TotalStudents = totalStudents,
                    TotalCapacity = totalCapacity,
                    OccupancyRate = totalCapacity > 0 ? (int)((double)totalStudents / totalCapacity * 100) : 0,
                    PendingMaintenance = pendingMaintenance,
                    LowStockItems = lowStockItems
                });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }
    }
}
