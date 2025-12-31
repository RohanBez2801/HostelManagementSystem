using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace HostelManagementSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinancialController : ControllerBase
    {
        /// <summary>
        /// Records a payment and automatically splits it between MoE and HDF funds.
        /// </summary>
        [HttpPost("record-payment")]
        public IActionResult RecordPayment([FromBody] PaymentRequest req)
        {
            if (req == null) return BadRequest("Invalid payment data.");

            using (var conn = Helpers.DbHelper.GetConnection())
            {
                try
                {
                    int currentYear = DateTime.Now.Year;

                    // 1. CHECK PREVIOUS MOE PAYMENTS FOR THIS YEAR
                    // This ensures we don't charge more than N$ 619 to the MoE bucket per year.
                    string checkSql = "SELECT SUM(MoE_Portion) FROM tbl_Payments WHERE LearnerID = @id AND YEAR(PaymentDate) = @yr";
                    decimal alreadyPaidMoE = 0;

                    using (OleDbCommand cmd = new OleDbCommand(checkSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", req.LearnerId);
                        cmd.Parameters.AddWithValue("@yr", currentYear);
                        var result = cmd.ExecuteScalar();
                        alreadyPaidMoE = (result != DBNull.Value) ? Convert.ToDecimal(result) : 0;
                    }

                    // 2. CALCULATE SMART ALLOCATION
                    decimal moeLimit = 619.00m;
                    decimal remainingMoeCap = moeLimit - alreadyPaidMoE;
                    if (remainingMoeCap < 0) remainingMoeCap = 0;

                    decimal moeAllocated = 0;
                    decimal hdfAllocated = 0;

                    if (req.TotalAmount <= remainingMoeCap)
                    {
                        moeAllocated = req.TotalAmount;
                        hdfAllocated = 0;
                    }
                    else
                    {
                        moeAllocated = remainingMoeCap;
                        hdfAllocated = req.TotalAmount - remainingMoeCap;
                    }

                    // 3. SAVE THE SPLIT PAYMENT TO ACCESS
                    string insertSql = @"INSERT INTO tbl_Payments 
                        (LearnerID, TotalAmount, MoE_Portion, HDF_Portion, MinistryReceiptNo, HDFReceiptNo, PaymentDate) 
                        VALUES (?, ?, ?, ?, ?, ?, ?)";

                    using (OleDbCommand ins = new OleDbCommand(insertSql, conn))
                    {
                        // OleDb uses positional parameters (?), so order matters strictly here
                        ins.Parameters.AddWithValue("?", req.LearnerId);
                        ins.Parameters.AddWithValue("?", req.TotalAmount);
                        ins.Parameters.AddWithValue("?", moeAllocated);
                        ins.Parameters.AddWithValue("?", hdfAllocated);
                        ins.Parameters.AddWithValue("?", req.MinReceipt ?? (object)DBNull.Value);
                        ins.Parameters.AddWithValue("?", req.HdfReceipt ?? (object)DBNull.Value);
                        ins.Parameters.AddWithValue("?", req.PaymentDate);

                        ins.ExecuteNonQuery();
                    }

                    return Ok(new
                    {
                        success = true,
                        moe = moeAllocated,
                        hdf = hdfAllocated,
                        message = "Payment split and recorded successfully."
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Database Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Fetches the full payment history for the Financial Ledger view.
        /// </summary>
        [HttpGet("all-payments")]
        public IActionResult GetAllPayments()
        {
            var payments = new List<object>();
            using (var conn = Helpers.DbHelper.GetConnection())
            {
                // Note: In a real app, you'd JOIN with tbl_Learners to get the name.
                // Since Learners are in a different DB context, you may need to map names in the frontend.
                string sql = "SELECT * FROM tbl_Payments ORDER BY PaymentDate DESC";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        payments.Add(new
                        {
                            Id = reader["PaymentID"],
                            LearnerId = reader["LearnerID"],
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            MoE_Portion = Convert.ToDecimal(reader["MoE_Portion"]),
                            HDF_Portion = Convert.ToDecimal(reader["HDF_Portion"]),
                            MinReceipt = reader["MinistryReceiptNo"].ToString(),
                            HdfReceipt = reader["HDFReceiptNo"].ToString(),
                            PaymentDate = Convert.ToDateTime(reader["PaymentDate"])
                        });
                    }
                }
            }
            return Ok(payments);
        }

        /// <summary>
        /// Fetches a detailed statement for a specific learner.
        /// </summary>
        [HttpGet("statement/{learnerId}")]
        public IActionResult GetStatement(int learnerId)
        {
            var statementLines = new List<object>();
            using (var conn = Helpers.DbHelper.GetConnection())
            {
                // Query to get all payments for this student, newest first
                string sql = "SELECT * FROM tbl_Payments WHERE LearnerID = ? ORDER BY PaymentDate DESC";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", learnerId);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            statementLines.Add(new
                            {
                                Date = Convert.ToDateTime(reader["PaymentDate"]).ToShortDateString(),
                                Total = Convert.ToDecimal(reader["TotalAmount"]),
                                MoE = Convert.ToDecimal(reader["MoE_Portion"]),
                                HDF = Convert.ToDecimal(reader["HDF_Portion"]),
                                Receipts = $"{reader["MinistryReceiptNo"]} / {reader["HDFReceiptNo"]}"
                            });
                        }
                    }
                }
            }
            return Ok(statementLines);
        }

        /// <summary>
        /// Fetches a financial summary for the current year.
        /// </summary>
        [HttpGet("summary")]
        public IActionResult GetFinancialSummary()
        {
            using (var conn = Helpers.DbHelper.GetConnection())
            {
                string sql = "SELECT SUM(TotalAmount) as Total, SUM(MoE_Portion) as MoE, SUM(HDF_Portion) as HDF FROM tbl_Payments WHERE YEAR(PaymentDate) = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", DateTime.Now.Year);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new
                            {
                                Total = reader["Total"] != DBNull.Value ? reader["Total"] : 0,
                                MoE = reader["MoE"] != DBNull.Value ? reader["MoE"] : 0,
                                HDF = reader["HDF"] != DBNull.Value ? reader["HDF"] : 0
                            });
                        }
                    }
                }
            }
            return Ok(new { Total = 0, MoE = 0, HDF = 0 });
        }
    }

    /// <summary>
    /// Data Transfer Object (DTO) for incoming payment requests.
    /// Defining this here resolves the 'missing reference' error.
    /// </summary>
    public class PaymentRequest
    {
        public int LearnerId { get; set; }
        public decimal TotalAmount { get; set; }
        public string MinReceipt { get; set; }
        public string HdfReceipt { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}