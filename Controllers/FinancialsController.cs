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
        private readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";
        private const int VOTE_INCOME_HDF = 100;
        private const int VOTE_INCOME_MOE = 101;

        // --- 1. GET CASH BOOK (FIXED: SHOWS PAYEE IF STUDENT IS NULL) ---
        [HttpGet("cashbook")]
        public IActionResult GetCashBook()
        {
            var transactions = new List<object>();
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    // DISTINCTROW ensures we don't get duplicates if join acts up
                    string sql = @"
                        SELECT DISTINCTROW p.[PaymentID], p.[PaymentDate], p.[TotalAmount], p.[TransactionType], p.[Payee], p.[MinistryReceiptNo], 
                               l.[Surname], l.[Names], l.[AdmissionNo], v.[VoteName]
                        FROM ([tbl_Payments] p 
                        LEFT JOIN [tbl_Learners] l ON p.[LearnerID] = l.[LearnerID])
                        LEFT JOIN [tbl_Votes] v ON p.[VoteID] = v.[VoteID]
                        ORDER BY p.[PaymentDate] DESC";

                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string type = reader["TransactionType"]?.ToString() ?? "Income";
                            string surname = reader["Surname"]?.ToString() ?? "";
                            string names = reader["Names"]?.ToString() ?? "";
                            string studentName = $"{surname} {names}".Trim();
                            string payeeText = reader["Payee"]?.ToString() ?? "Unknown";

                            // LOGIC: Use Student Name if available, otherwise use Payee Text
                            string displayDesc = !string.IsNullOrEmpty(studentName)
                                ? $"{studentName} ({reader["AdmissionNo"]})"
                                : payeeText;

                            transactions.Add(new
                            {
                                id = reader["PaymentID"],
                                date = Convert.ToDateTime(reader["PaymentDate"]).ToString("yyyy-MM-dd"),
                                type = type,
                                description = displayDesc, // Correctly populated now
                                vote = reader["VoteName"]?.ToString() ?? "-",
                                amount = Convert.ToDouble(reader["TotalAmount"]),
                                refNo = reader["MinistryReceiptNo"]?.ToString()
                            });
                        }
                    }
                }
                return Ok(transactions);
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Error loading cashbook: " + ex.Message }); }
        }

        // --- 2. IMPORT REVENUE (With Duplicate Check & Name Matching) ---
        [HttpPost("import/revenue")]
        public IActionResult ImportRevenue([FromBody] List<RevenueImportModel> rows)
        {
            int count = 0;
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    foreach (var row in rows)
                    {
                        if (row.AmountHDF > 0)
                        {
                            InsertImportedPayment(conn, row, row.AmountHDF, VOTE_INCOME_HDF);
                            count++;
                        }
                        if (row.AmountMoE > 0)
                        {
                            InsertImportedPayment(conn, row, row.AmountMoE, VOTE_INCOME_MOE);
                            count++;
                        }
                    }
                }
                return Ok(new { Message = $"Processed {rows.Count} rows. Duplicates were skipped automatically." });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        private void InsertImportedPayment(OleDbConnection conn, RevenueImportModel row, double amount, int voteId)
        {
            // A. DUPLICATE CHECK
            if (!string.IsNullOrEmpty(row.ReceiptNo))
            {
                string checkSql = "SELECT COUNT(*) FROM [tbl_Payments] WHERE [MinistryReceiptNo] = ?";
                using (var cmd = new OleDbCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("?", row.ReceiptNo.Trim());
                    if ((int)cmd.ExecuteScalar() > 0) return; // Skip
                }
            }

            // B. FIND LEARNER (Safe Brackets)
            int learnerId = 0;
            string payeeInput = row.Payee?.Trim() ?? "";

            string findSql = @"
                SELECT TOP 1 [LearnerID] FROM [tbl_Learners] 
                WHERE ([Surname] + ' ' + [Names]) LIKE ? 
                   OR ([Names] + ' ' + [Surname]) LIKE ?";

            using (var cmd = new OleDbCommand(findSql, conn))
            {
                string searchPattern = $"%{payeeInput}%";
                cmd.Parameters.AddWithValue("?", searchPattern);
                cmd.Parameters.AddWithValue("?", searchPattern);

                var result = cmd.ExecuteScalar();
                if (result != null) learnerId = Convert.ToInt32(result);
            }

            // C. INSERT (Uses PayeeInput if learnerId is 0)
            string sql = @"INSERT INTO [tbl_Payments] 
                ([LearnerID], [TotalAmount], [PaymentDate], [TransactionType], [VoteID], [Payee], [MinistryReceiptNo]) 
                VALUES (?, ?, ?, 'Income', ?, ?, ?)";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                if (learnerId == 0) cmd.Parameters.AddWithValue("?", DBNull.Value);
                else cmd.Parameters.AddWithValue("?", learnerId);

                cmd.Parameters.AddWithValue("?", amount);
                cmd.Parameters.AddWithValue("?", row.Date);
                cmd.Parameters.AddWithValue("?", voteId);
                cmd.Parameters.AddWithValue("?", payeeInput); // Saves the text for display fallback
                cmd.Parameters.AddWithValue("?", row.ReceiptNo ?? "");
                cmd.ExecuteNonQuery();
            }
        }

        // --- 3. OTHER METHODS (Unchanged but included for completeness) ---

        [HttpPost("pay")]
        public IActionResult RecordPayment([FromBody] PaymentRequest req)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    // Check Receipt Duplicate
                    if (!string.IsNullOrEmpty(req.Reference))
                    {
                        using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM [tbl_Payments] WHERE [MinistryReceiptNo] = ?", conn))
                        {
                            cmd.Parameters.AddWithValue("?", req.Reference);
                            if ((int)cmd.ExecuteScalar() > 0) return StatusCode(409, new { Message = "Receipt exists" });
                        }
                    }

                    int learnerId = 0;
                    using (var findCmd = new OleDbCommand("SELECT [LearnerID] FROM [tbl_Learners] WHERE [AdmissionNo] = ?", conn))
                    {
                        findCmd.Parameters.AddWithValue("?", req.AdmissionNo);
                        var result = findCmd.ExecuteScalar();
                        if (result == null) return BadRequest(new { Message = "Learner Not Found" });
                        learnerId = Convert.ToInt32(result);
                    }

                    int dbVoteId = req.VoteId == 2 ? VOTE_INCOME_MOE : VOTE_INCOME_HDF;
                    string sql = @"INSERT INTO [tbl_Payments] ([LearnerID], [TotalAmount], [PaymentDate], [TransactionType], [VoteID], [Payee], [MinistryReceiptNo]) VALUES (?, ?, ?, 'Income', ?, ?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", learnerId);
                        cmd.Parameters.AddWithValue("?", req.Amount);
                        cmd.Parameters.AddWithValue("?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("?", dbVoteId);
                        cmd.Parameters.AddWithValue("?", "Learner Payment");
                        cmd.Parameters.AddWithValue("?", req.Reference ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Payment Saved" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpPost("expense")]
        public IActionResult RecordExpense([FromBody] ExpenseRequest req)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    string sql = @"INSERT INTO [tbl_Payments] ([TotalAmount], [PaymentDate], [TransactionType], [VoteID], [Payee], [MinistryReceiptNo]) VALUES (?, ?, 'Expense', ?, ?, ?)";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", req.Amount);
                        cmd.Parameters.AddWithValue("?", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("?", req.VoteId);
                        cmd.Parameters.AddWithValue("?", req.Payee);
                        cmd.Parameters.AddWithValue("?", req.Reference ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
                return Ok(new { Message = "Expenditure Recorded" });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }

        [HttpGet("votes")]
        public IActionResult GetVotes()
        {
            var votes = new List<object>();
            using (OleDbConnection conn = new OleDbConnection(_connString))
            {
                try
                {
                    conn.Open();
                    var cmd = new OleDbCommand("SELECT * FROM [tbl_Votes] ORDER BY [VoteCode]", conn);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read()) { votes.Add(new { Id = reader["VoteID"], Name = reader["VoteName"] }); }
                }
                catch { }
            }
            return Ok(votes);
        }

        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    string sql = $@"
                        SELECT 
                            SUM(IIF([TransactionType]='Income', [TotalAmount], 0)) as TotalIncome,
                            SUM(IIF([VoteID]={VOTE_INCOME_HDF}, [TotalAmount], 0)) as HDF,
                            SUM(IIF([VoteID]={VOTE_INCOME_MOE}, [TotalAmount], 0)) as MoE
                        FROM [tbl_Payments]";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            double total = reader["TotalIncome"] != DBNull.Value ? Convert.ToDouble(reader["TotalIncome"]) : 0;
                            double hdf = reader["HDF"] != DBNull.Value ? Convert.ToDouble(reader["HDF"]) : 0;
                            double moe = reader["MoE"] != DBNull.Value ? Convert.ToDouble(reader["MoE"]) : 0;
                            return Ok(new { total, hdf, moe });
                        }
                    }
                }
                return Ok(new { total = 0, hdf = 0, moe = 0 });
            }
            catch (Exception ex) { return StatusCode(500, new { Message = "Summary Error: " + ex.Message }); }
        }

        [HttpGet("statement/{learnerId}")]
        public IActionResult GetLearnerStatement(int learnerId)
        {
            // Use logic from previous response, just ensuring brackets
            var transactions = new List<object>();
            double totalPaid = 0;
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connString))
                {
                    conn.Open();
                    string learnerName = "", admissionNo = "", parentName = "Parent", parentPhone = "", address = "";
                    string learnerSql = @"SELECT [Surname], [Names], [AdmissionNo], [FatherName], [FatherPhone], [MotherName], [MotherPhone], [HomeAddress] 
                                          FROM [tbl_Learners] WHERE [LearnerID] = ?";
                    using (var cmd = new OleDbCommand(learnerSql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", learnerId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                learnerName = $"{reader["Surname"]} {reader["Names"]}".Trim();
                                admissionNo = reader["AdmissionNo"].ToString();
                                string dad = reader["FatherName"]?.ToString();
                                string mom = reader["MotherName"]?.ToString();
                                if (!string.IsNullOrEmpty(dad)) { parentName = dad; parentPhone = reader["FatherPhone"]?.ToString(); }
                                else if (!string.IsNullOrEmpty(mom)) { parentName = mom; parentPhone = reader["MotherPhone"]?.ToString(); }
                                address = reader["HomeAddress"]?.ToString() ?? "";
                            }
                            else return NotFound(new { Message = "Learner not found" });
                        }
                    }
                    string sql = @"SELECT [PaymentDate], [TransactionType], [TotalAmount], [MinistryReceiptNo], [Payee] 
                                   FROM [tbl_Payments] WHERE [LearnerID] = ? ORDER BY [PaymentDate] DESC";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", learnerId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                double amt = Convert.ToDouble(reader["TotalAmount"]);
                                totalPaid += amt;
                                transactions.Add(new
                                {
                                    Date = Convert.ToDateTime(reader["PaymentDate"]).ToString("yyyy-MM-dd"),
                                    Type = reader["TransactionType"].ToString(),
                                    Amount = amt,
                                    Receipt = reader["MinistryReceiptNo"]?.ToString() ?? "-",
                                    Description = reader["Payee"]?.ToString()
                                });
                            }
                        }
                    }
                    return Ok(new
                    {
                        Learner = learnerName,
                        AdmissionNo = admissionNo,
                        ParentName = parentName,
                        ParentPhone = parentPhone,
                        Address = address,
                        GeneratedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                        TotalPaid = totalPaid,
                        Transactions = transactions
                    });
                }
            }
            catch (Exception ex) { return StatusCode(500, new { Message = ex.Message }); }
        }
    }

    public class RevenueImportModel { 
        public DateTime Date { get; set; } 
        public string ReceiptNo { get; set; } 
        public string Payee { get; set; } 
        public double AmountHDF { get; set; } 
        public double AmountMoE { get; set; } 
    }

    public class PaymentRequest { 
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
