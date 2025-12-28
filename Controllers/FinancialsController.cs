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
    public class FinancialsController : ControllerBase
    {
        // Path to the Budget 2025 Excel file
        private readonly string _excelPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Budget 2025.xlsx");

        // Excel Connection String (HDR=YES means first row is headers)
        private string GetExcelConnectionString()
        {
            return $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={_excelPath};Extended Properties='Excel 12.0 Xml;HDR=YES';";
        }

        // --- 1. GET CASH BOOK (Read from Excel) ---
        [HttpGet("cashbook")]
        public IActionResult GetCashBook()
        {
            var transactions = new List<object>();
            try
            {
                EnsureExcelFile(); // Ensure file and sheet exist

                using (OleDbConnection conn = new OleDbConnection(GetExcelConnectionString()))
                {
                    conn.Open();
                    // Select from the "Ledger" sheet (using $ notation)
                    string sql = "SELECT * FROM [Ledger$]";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Map Excel Columns
                            // Assumptions: Date, Description, Vote, Amount, Type, RefNo
                            string type = reader["Type"]?.ToString() ?? "Income";
                            double amount = 0;
                            double.TryParse(reader["Amount"]?.ToString(), out amount);

                            transactions.Add(new
                            {
                                date = Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                                type = type,
                                description = reader["Description"]?.ToString(),
                                vote = reader["Vote"]?.ToString() ?? "General",
                                amount = amount,
                                refNo = reader["RefNo"]?.ToString()
                            });
                        }
                    }
                }
                // Sort by Date Descending in memory (Excel might be unsorted)
                transactions.Sort((a, b) =>
                    DateTime.Parse(((dynamic)b).date).CompareTo(DateTime.Parse(((dynamic)a).date))
                );

                return Ok(transactions);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Error loading Excel: " + ex.Message }); }
        }

        // --- 2. RECORD TRANSACTION (Write to Excel) ---
        [HttpPost("pay")]
        public IActionResult RecordPayment([FromBody] ExcelPaymentRequest req)
        {
            // Include Admission Number in Description so it's not anonymous
            string desc = string.IsNullOrWhiteSpace(req.AdmissionNo) ? "Learner Payment" : $"Payment - {req.AdmissionNo}";
            return AddTransactionToExcel(DateTime.Now, "Income", req.VoteId.ToString(), req.Amount, desc, req.Reference);
        }

        [HttpPost("expense")]
        public IActionResult RecordExpense([FromBody] ExpenseRequest req)
        {
            return AddTransactionToExcel(DateTime.Now, "Expense", req.VoteId.ToString(), req.Amount, req.Payee, req.Reference);
        }

        private IActionResult AddTransactionToExcel(DateTime date, string type, string vote, double amount, string desc, string refNo)
        {
            try
            {
                EnsureExcelFile();
                using (OleDbConnection conn = new OleDbConnection(GetExcelConnectionString()))
                {
                    conn.Open();
                    string sql = "INSERT INTO [Ledger$] ([Date], [Description], [Vote], [Amount], [Type], [RefNo]) VALUES (?, ?, ?, ?, ?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", date.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("?", desc ?? "");
                        cmd.Parameters.AddWithValue("?", vote ?? "General");
                        cmd.Parameters.AddWithValue("?", amount);
                        cmd.Parameters.AddWithValue("?", type);
                        cmd.Parameters.AddWithValue("?", refNo ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Transaction Saved to Excel" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // --- 3. STUDENT STATEMENTS (Filtered View) ---
        [HttpGet("statement/{adNo}")]
        public IActionResult GetLearnerStatement(string adNo)
        {
            var transactions = new List<object>();
            try
            {
                EnsureExcelFile();
                using (OleDbConnection conn = new OleDbConnection(GetExcelConnectionString()))
                {
                    conn.Open();
                    string sql = "SELECT * FROM [Ledger$] WHERE [Description] LIKE ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        // Filter for payments made by this student
                        // We look for "Payment - {adNo}" or just matching description if we had a specific column
                        cmd.Parameters.AddWithValue("?", $"%{adNo}%");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string type = reader["Type"]?.ToString();
                                double amount = 0;
                                double.TryParse(reader["Amount"]?.ToString(), out amount);

                                transactions.Add(new
                                {
                                    date = Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                                    type = type,
                                    description = reader["Description"]?.ToString(),
                                    amount = amount,
                                    refNo = reader["RefNo"]?.ToString()
                                });
                            }
                        }
                    }
                }
                return Ok(transactions);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Error loading statement: " + ex.Message }); }
        }

        // --- 4. SUMMARY (Aggregate from Excel) ---
        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            try
            {
                EnsureExcelFile();
                double totalIncome = 0;
                double hdf = 0;
                double moe = 0;

                using (OleDbConnection conn = new OleDbConnection(GetExcelConnectionString()))
                {
                    conn.Open();
                    string sql = "SELECT [Type], [Vote], [Amount] FROM [Ledger$]";
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string type = reader["Type"]?.ToString();
                            string vote = reader["Vote"]?.ToString();
                            double amt = 0;
                            double.TryParse(reader["Amount"]?.ToString(), out amt);

                            if (type == "Income")
                            {
                                totalIncome += amt;
                                // Simple string match for Vote since IDs are gone
                                if (vote.Contains("MoE") || vote == "101") moe += amt;
                                else hdf += amt; // Assume HDF is everything else
                            }
                        }
                    }
                }
                return Ok(new { total = totalIncome, hdf, moe });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        // --- 4. VOTES (Mocked or read from separate sheet) ---
        [HttpGet("votes")]
        public IActionResult GetVotes()
        {
            // For simplicity, we return a hardcoded list or could read from a "Votes" sheet
            return Ok(new[] {
                new { Id = 100, Name = "HDF General Fund" },
                new { Id = 101, Name = "MoE Q-Book Fund" },
                new { Id = 200, Name = "Maintenance" },
                new { Id = 201, Name = "Food Supplies" },
                new { Id = 202, Name = "Cleaning Materials" }
            });
        }

        // Helper to Create File if Missing (Code First for Excel)
        private void EnsureExcelFile()
        {
            if (!System.IO.File.Exists(_excelPath))
            {
                // Create a new Excel file using ADOX or OLEDB CREATE TABLE
                // Note: Creating a new .xlsx via OLEDB can be tricky.
                // We rely on the connection opening to creating it, or we throw if missing.
                // However, OLEDB usually can create the file if the connection string matches.

                try
                {
                    using (OleDbConnection conn = new OleDbConnection(GetExcelConnectionString()))
                    {
                        conn.Open();
                        // Create Sheet with Headers
                        string sql = "CREATE TABLE Ledger ([Date] DATETIME, [Description] MEMO, [Vote] TEXT, [Amount] DOUBLE, [Type] TEXT, [RefNo] TEXT)";
                        using (var cmd = new OleDbCommand(sql, conn)) cmd.ExecuteNonQuery();
                    }
                }
                catch
                {
                    // Fallback or ignore if file creation isn't supported by driver (often requires existing file)
                    // In a real environment, we'd copy a template.
                    throw new FileNotFoundException("Budget 2025.xlsx not found in Data folder. Please upload the file.");
                }
            }
        }
    }

    public class ExcelPaymentRequest {
        public string AdmissionNo { get; set; } 
        public double Amount { get; set; } 
        public int VoteId { get; set; } 
        public string Reference { get; set; } 
    }

    public class ExpenseRequest { 
        public double Amount { get; set; } 
        public int VoteId { get; set; } 
        public string Payee { get; set; } 
        public string Reference { get; set; } 
    }
}
