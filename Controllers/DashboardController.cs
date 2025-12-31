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
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            try
            {
                int totalStudents = 0;
                int totalCapacity = 0;
                int pendingMaintenance = 0;
                int lowStockItems = 0;

                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();

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

                    // 5. License Status
                    string licenseStatus = "Trial";
                    int daysLeft = 0;
                    try {
                        using (var cmd = new OleDbCommand("SELECT LicenseExpiry FROM tbl_Settings", conn))
                        {
                            var result = cmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value) {
                                DateTime expiry = Convert.ToDateTime(result);
                                daysLeft = (int)(expiry - DateTime.Now).TotalDays;
                                licenseStatus = daysLeft > 0 ? "Active" : "Expired";
                            }
                        }
                    } catch { /* Table might not exist yet */ }
                
                    return Ok(new
                    {
                        TotalStudents = totalStudents,
                        TotalCapacity = totalCapacity,
                        OccupancyRate = totalCapacity > 0 ? (int)((double)totalStudents / totalCapacity * 100) : 0,
                        PendingMaintenance = pendingMaintenance,
                        LowStockItems = lowStockItems,
                        LicenseStatus = licenseStatus,
                        LicenseDaysLeft = daysLeft
                    });
                }
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }
    }
}
