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
    public class SettingsController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet]
        public IActionResult GetSettings()
        {
            var settings = new Dictionary<string, object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureSettingsTable(conn);
                    string sql = "SELECT * FROM tbl_Settings";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Map all columns to the dictionary
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                settings[reader.GetName(i)] = reader.GetValue(i);
                            }
                        }
                    }
                }
                return Ok(settings);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost]
        public IActionResult SaveSettings([FromBody] SettingsModel model)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureSettingsTable(conn);

                    // Check if row exists
                    bool exists = false;
                    using (var checkCmd = new OleDbCommand("SELECT COUNT(*) FROM tbl_Settings", conn))
                    {
                        exists = (int)checkCmd.ExecuteScalar() > 0;
                    }

                    string sql;
                    if (exists)
                    {
                        // Update
                        sql = @"UPDATE tbl_Settings SET 
                                HostelName=?, LogoText=?, Currency=?, FullFee=?, MoeFee=?, HdfFee=?,
                                AddressPhys=?, AddressPost=?, Phone=?, Email=?, 
                                BankName=?, AccName=?, AccNo=?, Branch=?, LogoData=?
                                "; // Assumes single row
                    }
                    else
                    {
                        // Insert
                        sql = @"INSERT INTO tbl_Settings (
                                HostelName, LogoText, Currency, FullFee, MoeFee, HdfFee,
                                AddressPhys, AddressPost, Phone, Email,
                                BankName, AccName, AccNo, Branch, LogoData
                                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    }

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", model.HostelName ?? "");
                        cmd.Parameters.AddWithValue("?", model.LogoText ?? "");
                        cmd.Parameters.AddWithValue("?", model.Currency ?? "N$");
                        cmd.Parameters.AddWithValue("?", model.FullFee);
                        cmd.Parameters.AddWithValue("?", model.MoeFee);
                        cmd.Parameters.AddWithValue("?", model.HdfFee);
                        cmd.Parameters.AddWithValue("?", model.AddressPhys ?? "");
                        cmd.Parameters.AddWithValue("?", model.AddressPost ?? "");
                        cmd.Parameters.AddWithValue("?", model.Phone ?? "");
                        cmd.Parameters.AddWithValue("?", model.Email ?? "");
                        cmd.Parameters.AddWithValue("?", model.BankName ?? "");
                        cmd.Parameters.AddWithValue("?", model.AccName ?? "");
                        cmd.Parameters.AddWithValue("?", model.AccNo ?? "");
                        cmd.Parameters.AddWithValue("?", model.Branch ?? "");
                        // For large text (LogoData), we might hit limits in Access short text. 
                        // Using Memo/LongText is required in table creation.
                        cmd.Parameters.AddWithValue("?", model.LogoData ?? "");

                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Settings saved" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("license")]
        public IActionResult ActivateLicense([FromBody] LicenseModel model)
        {
            try
            {
                // Validate License Key (Mock Logic)
                // Format: HP-YEAR-XXXX
                DateTime expiryDate;
                string type = "Standard";

                if (string.IsNullOrWhiteSpace(model.Key))
                    return BadRequest(new { Message = "Invalid Key" });

                string key = model.Key.ToUpper().Trim();

                if (key.Contains("LIFETIME"))
                {
                    expiryDate = new DateTime(2099, 12, 31);
                    type = "Lifetime";
                }
                else if (key.Contains("2024"))
                {
                    expiryDate = new DateTime(2024, 12, 31);
                }
                else if (key.Contains("2025"))
                {
                    expiryDate = new DateTime(2025, 12, 31);
                }
                else if (key.Contains("2026"))
                {
                    expiryDate = new DateTime(2026, 12, 31);
                }
                else
                {
                    // Default trial or error
                    return BadRequest(new { Message = "Invalid License Key" });
                }

                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureSettingsTable(conn);
                    
                    // Update License Columns
                    // We assume table exists from EnsureSettingsTable
                    string sql = "UPDATE tbl_Settings SET LicenseKey = ?, LicenseExpiry = ?, LicenseType = ?";
                    using(var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", key);
                        cmd.Parameters.AddWithValue("?", expiryDate.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("?", type);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { Message = "License Activated", Expiry = expiryDate.ToString("yyyy-MM-dd"), Type = type });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        private void EnsureSettingsTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Settings", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                string createSql = @"CREATE TABLE tbl_Settings (
                    ID AUTOINCREMENT PRIMARY KEY,
                    HostelName TEXT(255),
                    LogoText TEXT(50),
                    Currency TEXT(10),
                    FullFee CURRENCY,
                    MoeFee CURRENCY,
                    HdfFee CURRENCY,
                    AddressPhys TEXT(255),
                    AddressPost TEXT(255),
                    Phone TEXT(50),
                    Email TEXT(100),
                    BankName TEXT(100),
                    AccName TEXT(100),
                    AccNo TEXT(50),
                    Branch TEXT(50),
                    LogoData MEMO, 
                    LicenseKey TEXT(100),
                    LicenseExpiry DATETIME,
                    LicenseType TEXT(50)
                )";
                using (var cmd = new OleDbCommand(createSql, conn)) cmd.ExecuteNonQuery();
                
                // Init with empty row
                using (var cmd = new OleDbCommand("INSERT INTO tbl_Settings (HostelName) VALUES ('HostelPro')", conn))
                    cmd.ExecuteNonQuery();
            }
            else
            {
                // Ensure Columns Exist (Migration for License)
                var cols = conn.GetSchema("Columns", new string[] { null, null, "tbl_Settings", null });
                bool hasLicense = false;
                foreach(System.Data.DataRow row in cols.Rows)
                {
                    if (row["COLUMN_NAME"].ToString() == "LicenseKey") hasLicense = true;
                }

                if (!hasLicense)
                {
                    using (var cmd = new OleDbCommand("ALTER TABLE tbl_Settings ADD COLUMN LicenseKey TEXT(100)", conn)) cmd.ExecuteNonQuery();
                    using (var cmd = new OleDbCommand("ALTER TABLE tbl_Settings ADD COLUMN LicenseExpiry DATETIME", conn)) cmd.ExecuteNonQuery();
                    using (var cmd = new OleDbCommand("ALTER TABLE tbl_Settings ADD COLUMN LicenseType TEXT(50)", conn)) cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public class SettingsModel
    {
        public string HostelName { get; set; }
        public string LogoText { get; set; }
        public string Currency { get; set; }
        public decimal FullFee { get; set; }
        public decimal MoeFee { get; set; }
        public decimal HdfFee { get; set; }
        public string AddressPhys { get; set; }
        public string AddressPost { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string BankName { get; set; }
        public string AccName { get; set; }
        public string AccNo { get; set; }
        public string Branch { get; set; }
        public string LogoData { get; set; }
    }

    public class LicenseModel
    {
        public string Key { get; set; }
    }
}
