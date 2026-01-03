using System;
using System.Data.OleDb;
using System.IO;
using System.Collections.Generic;

namespace HostelManagementSystem.Helpers
{
    public static class DbHelper
    {
        // Path to your database
        private static string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        /// <summary>
        /// Returns a new Open connection to the database.
        /// This fixes the "DbHelper does not contain a definition for GetConnection" error.
        /// </summary>
        public static OleDbConnection GetConnection()
        {
            return new OleDbConnection(_connString);
        }

        public static void InitializeDatabase()
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb");

            // 1. Force Check: Does the file exist?
            if (!File.Exists(dbPath))
            {
                Console.WriteLine("CRITICAL: Database file not found. Creating new one...");
                return;
            }

            using (OleDbConnection conn = GetConnection()) // Use the new method here too
            {
                try
                {
                    conn.Open();
                    Console.WriteLine("--- STARTING DATABASE AUDIT ---");

                    // --- PHASE 1: FINANCIALS ---
                    EnsureTable(conn, "tbl_Ledger", @"
                        [TransactionID] AUTOINCREMENT PRIMARY KEY,
                        [TxDate] DATETIME,
                        [VoteID] INT,
                        [Description] MEMO,
                        [Reference] TEXT(50),
                        [Debit] CURRENCY DEFAULT 0,
                        [Credit] CURRENCY DEFAULT 0,
                        [PaymentMethod] TEXT(50),
                        [BankReconciled] BIT DEFAULT 0,
                        [LearnerID] INT DEFAULT 0
                    ");

                    EnsureTable(conn, "tbl_Votes", @"
                        [VoteID] AUTOINCREMENT PRIMARY KEY,
                        [VoteNumber] TEXT(20),
                        [VoteName] TEXT(100),
                        [Type] TEXT(20)
                    ");
                    SeedVotes(conn);

                    EnsureTable(conn, "tbl_Budget", @"
                        [BudgetID] AUTOINCREMENT PRIMARY KEY,
                        [VoteID] INT,
                        [FiscalYear] INT,
                        [Amount] CURRENCY
                    ");

                    // --- PHASE 2: ASSETS & STOCK ---
                    EnsureTable(conn, "tbl_Inventory", @"
                        [AssetID] AUTOINCREMENT PRIMARY KEY,
                        [ItemName] TEXT(100),
                        [SerialNumber] TEXT(100),
                        [Category] TEXT(50),
                        [Value] CURRENCY,
                        [Location] TEXT(50),
                        [Condition] TEXT(50),
                        [DateAcquired] DATETIME
                    ");

                    EnsureTable(conn, "tbl_StockControl", @"
                        [StockID] AUTOINCREMENT PRIMARY KEY,
                        [ItemType] TEXT(50),
                        [BookNumber] TEXT(50),
                        [SerialFrom] TEXT(50),
                        [SerialTo] TEXT(50),
                        [DateReceived] DATETIME,
                        [IssuedTo] TEXT(100),
                        [DateIssued] DATETIME
                    ");

                    // --- PHASE 3: OPERATIONS ---
                    EnsureTable(conn, "tbl_DiningLog", @"
                        [LogID] AUTOINCREMENT PRIMARY KEY,
                        [DateLogged] DATETIME,
                        [Supplier] TEXT(100),
                        [Item] TEXT(100),
                        [Quantity] TEXT(50),
                        [ReceivedBy] TEXT(100)
                    ");

                    EnsureTable(conn, "tbl_Maintenance", @"
                        [MaintenanceID] AUTOINCREMENT PRIMARY KEY,
                        [RoomID] INT,
                        [IssueDescription] MEMO,
                        [Priority] TEXT(20),
                        [ReportedDate] DATETIME,
                        [Status] TEXT(20) DEFAULT 'Pending',
                        [ResolvedDate] DATETIME
                    ");

                    // --- PHASE 4: PEOPLE ---
                    EnsureTable(conn, "tbl_Learners", @"
                        [LearnerID] AUTOINCREMENT PRIMARY KEY,
                        [AdmissionNo] TEXT(20),
                        [Name] TEXT(100),
                        [Surname] TEXT(100),
                        [Gender] TEXT(20),
                        [Grade] INT,
                        [RoomID] INT,
                        [Block] TEXT(50),
                        [DOB] DATETIME,
                        [HomeLanguage] TEXT(50),
                        [Citizenship] TEXT(50),
                        [ParentID] INT,
                        [MedicalConditions] MEMO,
                        [MedicalAidName] TEXT(100),
                        [MedicalAidNo] TEXT(50),
                        [DoctorName] TEXT(100),
                        [EmergencyContact] MEMO
                    ");

                    EnsureTable(conn, "tbl_Parents", @"
                        [ParentID] AUTOINCREMENT PRIMARY KEY,
                        [FatherName] TEXT(100),
                        [FatherPhone] TEXT(50),
                        [FatherEmail] TEXT(100),
                        [MotherName] TEXT(100),
                        [MotherPhone] TEXT(50),
                        [MotherEmail] TEXT(100),
                        [Address] MEMO
                    ");

                    EnsureTable(conn, "tbl_Staff", @"
                        [StaffID] AUTOINCREMENT PRIMARY KEY,
                        [FullName] TEXT(100),
                        [IDNumber] TEXT(50),
                        [JobTitle] TEXT(100),
                        [Shift] TEXT(50),
                        [ContactNo] TEXT(50),
                        [Email] TEXT(100)
                    ");

                    EnsureTable(conn, "tbl_Users", @"
                        [UserID] AUTOINCREMENT PRIMARY KEY,
                        [FullName] TEXT(100),
                        [Username] TEXT(50) UNIQUE,
                        [Password] TEXT(100),
                        [UserRole] TEXT(50),
                        [Status] TEXT(20) DEFAULT 'Active'
                    ");

                    EnsureTable(conn, "tbl_Settings", "[SettingKey] TEXT(50) PRIMARY KEY, [SettingValue] MEMO");
                    EnsureTable(conn, "tbl_Notices", "[NoticeID] AUTOINCREMENT PRIMARY KEY, [Message] MEMO, [Priority] TEXT(20), [DatePosted] DATETIME");
                    EnsureTable(conn, "tbl_Events", "[EventID] AUTOINCREMENT PRIMARY KEY, [Title] TEXT(100), [Date] DATETIME, [Type] TEXT(50), [Description] MEMO");
                    EnsureTable(conn, "tbl_Rooms", "[RoomID] AUTOINCREMENT PRIMARY KEY, [RoomNumber] TEXT(20), [BlockName] TEXT(50), [Capacity] INT DEFAULT 4, [OccupancyStatus] TEXT(20)");

                    Console.WriteLine("--- DATABASE AUDIT COMPLETE: ALL TABLES VERIFIED ---");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("CRITICAL DATABASE ERROR: " + ex.Message);
                }
            }
        }

        private static void EnsureTable(OleDbConnection conn, string tableName, string schema)
        {
            var schemaTable = conn.GetSchema("Tables", new string[] { null, null, tableName, "TABLE" });
            if (schemaTable.Rows.Count == 0)
            {
                try
                {
                    using (var cmd = new OleDbCommand($"CREATE TABLE [{tableName}] ({schema})", conn))
                    {
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"[CREATED] Table: {tableName}");
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[ERROR] creating {tableName}: {ex.Message}"); }
            }
        }

        private static void SeedVotes(OleDbConnection conn)
        {
            int count = 0;
            try
            {
                using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM tbl_Votes", conn))
                    count = (int)cmd.ExecuteScalar();
            }
            catch { return; }

            if (count == 0)
            {
                string[] votes = {
                    "1|Office Administration|Expense",
                    "2|Equipment & Machinery|Expense",
                    "3|Co-Extra / Social Activities|Expense",
                    "4|Kitchen & Laundry|Expense",
                    "5|Transport|Expense",
                    "6|Minor Maintenance / Sports|Expense",
                    "7|Institutional / Temp Workers|Expense",
                    "8|Health & Hygiene / Cleaning|Expense",
                    "9|CPD|Expense",
                    "10|Hostel Board|Expense",
                    "11|Petty Cash|Expense",
                    "12|External Auditing|Expense",
                    "13|Govt Hostel Boarding Fee|Expense",
                    "14|Bank Charges|Expense",
                    "A|Learners Hostel Fees|Income",
                    "B|Learners Govt Contribution|Income",
                    "C|Donations|Income",
                    "D|Fundraising|Income"
                };

                foreach (var v in votes)
                {
                    var parts = v.Split('|');
                    using (var cmd = new OleDbCommand("INSERT INTO tbl_Votes (VoteNumber, VoteName, Type) VALUES (?, ?, ?)", conn))
                    {
                        cmd.Parameters.AddWithValue("?", parts[0]);
                        cmd.Parameters.AddWithValue("?", parts[1]);
                        cmd.Parameters.AddWithValue("?", parts[2]);
                        cmd.ExecuteNonQuery();
                    }
                }
                Console.WriteLine("System: Seeded Vote Categories.");
            }
        }
    }
}