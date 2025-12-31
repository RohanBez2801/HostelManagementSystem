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
    public class SettingsController : ControllerBase
    {
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        [HttpGet]
        public IActionResult GetSettings()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    EnsureTable(conn);

                    // We only store one row of settings
                    using (var cmd = new OleDbCommand("SELECT TOP 1 * FROM tbl_Settings", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var settings = new
                            {
                                hostelName = reader["HostelName"]?.ToString(),
                                logoText = reader["LogoText"]?.ToString(),
                                logoData = reader["LogoData"]?.ToString(), // Base64
                                logoFileName = reader["LogoFileName"]?.ToString(),
                                currency = reader["Currency"]?.ToString(),
                                fullFee = reader["FullFee"] != DBNull.Value ? Convert.ToDouble(reader["FullFee"]) : 0,
                                moeFee = reader["MoeFee"] != DBNull.Value ? Convert.ToDouble(reader["MoeFee"]) : 619,
                                hdfFee = reader["HdfFee"] != DBNull.Value ? Convert.ToDouble(reader["HdfFee"]) : 0,
                                addressPhys = reader["AddressPhys"]?.ToString(),
                                addressPost = reader["AddressPost"]?.ToString(),
                                phone = reader["Phone"]?.ToString(),
                                email = reader["Email"]?.ToString(),
                                bankName = reader["BankName"]?.ToString(),
                                accName = reader["AccName"]?.ToString(),
                                accNo = reader["AccNo"]?.ToString(),
                                branch = reader["Branch"]?.ToString()
                            };
                            return Ok(settings);
                        }
                    }
                }
                // Return empty defaults if no row exists
                return Ok(new { });
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
                    EnsureTable(conn);

                    // Check if row exists
                    bool exists = false;
                    using (var cmdCheck = new OleDbCommand("SELECT COUNT(*) FROM tbl_Settings", conn))
                    {
                        exists = (int)cmdCheck.ExecuteScalar() > 0;
                    }

                    string sql;
                    if (exists)
                    {
                        sql = @"UPDATE tbl_Settings SET
                                [HostelName]=?, [LogoText]=?, [LogoData]=?, [LogoFileName]=?,
                                [Currency]=?, [FullFee]=?, [MoeFee]=?, [HdfFee]=?,
                                [AddressPhys]=?, [AddressPost]=?, [Phone]=?, [Email]=?,
                                [BankName]=?, [AccName]=?, [AccNo]=?, [Branch]=?";
                    }
                    else
                    {
                        sql = @"INSERT INTO tbl_Settings
                                ([HostelName], [LogoText], [LogoData], [LogoFileName],
                                 [Currency], [FullFee], [MoeFee], [HdfFee],
                                 [AddressPhys], [AddressPost], [Phone], [Email],
                                 [BankName], [AccName], [AccNo], [Branch])
                                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    }

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", model.HostelName ?? "");
                        cmd.Parameters.AddWithValue("?", model.LogoText ?? "");
                        cmd.Parameters.AddWithValue("?", model.LogoData ?? ""); // Warning: large base64 strings might hit Memo limits
                        cmd.Parameters.AddWithValue("?", model.LogoFileName ?? "");
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

                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Settings Saved" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        private void EnsureTable(OleDbConnection conn)
        {
            var schema = conn.GetSchema("Tables", new string[] { null, null, "tbl_Settings", "TABLE" });
            if (schema.Rows.Count == 0)
            {
                // Access 'MEMO' type is needed for LogoData (Base64) as it exceeds 255 chars
                string sql = @"CREATE TABLE tbl_Settings (
                                ID AUTOINCREMENT PRIMARY KEY,
                                HostelName TEXT(100),
                                LogoText TEXT(100),
                                LogoData MEMO,
                                LogoFileName TEXT(100),
                                Currency TEXT(10),
                                FullFee CURRENCY,
                                MoeFee CURRENCY,
                                HdfFee CURRENCY,
                                AddressPhys MEMO,
                                AddressPost MEMO,
                                Phone TEXT(50),
                                Email TEXT(100),
                                BankName TEXT(100),
                                AccName TEXT(100),
                                AccNo TEXT(50),
                                Branch TEXT(50)
                              )";
                using (var cmd = new OleDbCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
        }
    }

    public class SettingsModel
    {
        public string HostelName { get; set; }
        public string LogoText { get; set; }
        public string LogoData { get; set; }
        public string LogoFileName { get; set; }
        public string Currency { get; set; }
        public double FullFee { get; set; }
        public double MoeFee { get; set; }
        public double HdfFee { get; set; }
        public string AddressPhys { get; set; }
        public string AddressPost { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string BankName { get; set; }
        public string AccName { get; set; }
        public string AccNo { get; set; }
        public string Branch { get; set; }
    }
}
