using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.OleDb;
using HostelManagementSystem.Helpers;
using System.Runtime.Versioning;

namespace HostelManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SupportedOSPlatform("windows")]
    public class LicenseController : ControllerBase
    {
        [HttpPost("activate")]
        public IActionResult Activate([FromBody] LicenseRequest request)
        {
            if (string.IsNullOrEmpty(request.Key)) return BadRequest("Key is empty");

            DateTime? newExpiry = null;
            string type = "";
            string key = request.Key.ToUpper().Trim();

            // --- ROBUST VALIDATION LOGIC ---
            if (key == "HP-LIFETIME-PRO")
            {
                newExpiry = DateTime.Now.AddYears(99);
                type = "Lifetime Edition";
            }
            else if (key == "HP-2025-EDITION")
            {
                newExpiry = new DateTime(2025, 12, 31);
                type = "Annual 2025";
            }
            else if (key == "HP-2026-EDITION")
            {
                newExpiry = new DateTime(2026, 12, 31);
                type = "Annual 2026";
            }
            else if (key.StartsWith("HP-TRIAL-"))
            {
                newExpiry = DateTime.Now.AddDays(30);
                type = "30 Day Trial";
            }
            else
            {
                return BadRequest("Invalid License Key. Please contact support.");
            }

            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    // Save Key, Expiry, and Type to tbl_Settings
                    UpsertSetting(conn, "LicenseKey", key);
                    UpsertSetting(conn, "LicenseExpiry", newExpiry.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    UpsertSetting(conn, "LicenseType", type);
                }
                return Ok(new { Message = "Activation Successful!", Expiry = newExpiry, Type = type });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            try
            {
                using (var conn = DbHelper.GetConnection())
                {
                    conn.Open();
                    string expiryStr = GetSetting(conn, "LicenseExpiry");
                    string type = GetSetting(conn, "LicenseType") ?? "Unlicensed";

                    if (string.IsNullOrEmpty(expiryStr))
                        return Ok(new { Status = "Inactive", DaysLeft = 0, Type = "None" });

                    DateTime expiry = DateTime.Parse(expiryStr);
                    int daysLeft = (int)(expiry - DateTime.Now).TotalDays;

                    return Ok(new
                    {
                        Status = daysLeft > 0 ? "Active" : "Expired",
                        DaysLeft = daysLeft > 0 ? daysLeft : 0,
                        Type = type,
                        ExpiryDate = expiry.ToString("yyyy-MM-dd")
                    });
                }
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        // Helper to Update or Insert settings
        private void UpsertSetting(OleDbConnection conn, string key, string value)
        {
            var cmdCheck = new OleDbCommand("SELECT COUNT(*) FROM tbl_Settings WHERE SettingKey = ?", conn);
            cmdCheck.Parameters.AddWithValue("?", key);
            int count = (int)cmdCheck.ExecuteScalar();

            if (count == 0)
            {
                var cmdInsert = new OleDbCommand("INSERT INTO tbl_Settings (SettingKey, SettingValue) VALUES (?, ?)", conn);
                cmdInsert.Parameters.AddWithValue("?", key);
                cmdInsert.Parameters.AddWithValue("?", value);
                cmdInsert.ExecuteNonQuery();
            }
            else
            {
                var cmdUpdate = new OleDbCommand("UPDATE tbl_Settings SET SettingValue = ? WHERE SettingKey = ?", conn);
                cmdUpdate.Parameters.AddWithValue("?", value);
                cmdUpdate.Parameters.AddWithValue("?", key);
                cmdUpdate.ExecuteNonQuery();
            }
        }

        private string GetSetting(OleDbConnection conn, string key)
        {
            try
            {
                var cmd = new OleDbCommand("SELECT SettingValue FROM tbl_Settings WHERE SettingKey = ?", conn);
                cmd.Parameters.AddWithValue("?", key);
                var res = cmd.ExecuteScalar();
                return res != null && res != DBNull.Value ? res.ToString() : null;
            }
            catch { return null; }
        }
    }

    public class LicenseRequest { public string Key { get; set; } }
}