using System;
using System.Data.OleDb;
using System.IO;
using System.Collections.Generic;

namespace HostelManagementSystem.Helpers
{
    public static class DbHelper
    {
        // CENTRAL CONNECTION STRING - Single Source of Truth
        private static string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};Persist Security Info=False;";

        /// <summary>
        /// Returns a new Open connection to the database.
        /// Fixes "DbHelper does not contain a definition for GetConnection"
        /// </summary>
        public static OleDbConnection GetConnection()
        {
            return new OleDbConnection(_connString);
        }

        public static void InitializeDatabase()
        {
            string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb");

            if (!File.Exists(dbPath))
            {
                Console.WriteLine("CRITICAL: Database file not found. Please ensure Data/HostelDb.accdb exists.");
                return;
            }

            using (OleDbConnection conn = GetConnection())
            {
                try
                {
                    conn.Open();
                    Console.WriteLine("--- CHECKING DATABASE SCHEMA ---");

                    // 1. Users & Auth
                    EnsureTable(conn, "tbl_Users", "[UserID] AUTOINCREMENT PRIMARY KEY, [FullName] TEXT(100), [Username] TEXT(50) UNIQUE, [Password] TEXT(100), [UserRole] TEXT(50), [Status] TEXT(20) DEFAULT 'Active'");

                    // 2. Staff
                    EnsureTable(conn, "tbl_Staff", "[StaffID] AUTOINCREMENT PRIMARY KEY, [FullName] TEXT(100), [IDNumber] TEXT(50), [JobTitle] TEXT(100), [Shift] TEXT(50), [ContactNo] TEXT(50), [Email] TEXT(100)");

                    // 3. Rooms
                    EnsureTable(conn, "tbl_Rooms", "[RoomID] AUTOINCREMENT PRIMARY KEY, [RoomNumber] TEXT(20), [BlockName] TEXT(50), [Capacity] INT DEFAULT 4, [OccupancyStatus] TEXT(20)");

                    // 4. Learners (Students)
                    EnsureTable(conn, "tbl_Learners", "[LearnerID] AUTOINCREMENT PRIMARY KEY, [AdmissionNo] TEXT(20), [Name] TEXT(100), [Surname] TEXT(100), [Gender] TEXT(20), [Grade] INT, [RoomID] INT, [Block] TEXT(50), [DOB] DATETIME, [HomeLanguage] TEXT(50), [Citizenship] TEXT(50), [ParentID] INT, [MedicalConditions] MEMO, [MedicalAidName] TEXT(100), [MedicalAidNo] TEXT(50), [DoctorName] TEXT(100), [EmergencyContact] MEMO");

                    // 5. Parents
                    EnsureTable(conn, "tbl_Parents", "[ParentID] AUTOINCREMENT PRIMARY KEY, [FatherName] TEXT(100), [FatherPhone] TEXT(50), [FatherEmail] TEXT(100), [MotherName] TEXT(100), [MotherPhone] TEXT(50), [MotherEmail] TEXT(100), [Address] MEMO");

                    // 6. Maintenance
                    EnsureTable(conn, "tbl_Maintenance", "[MaintenanceID] AUTOINCREMENT PRIMARY KEY, [RoomID] INT, [IssueDescription] MEMO, [Priority] TEXT(20), [ReportedDate] DATETIME, [Status] TEXT(20) DEFAULT 'Pending', [ResolvedDate] DATETIME");

                    // 7. Dining Log
                    EnsureTable(conn, "tbl_DiningLog", "[LogID] AUTOINCREMENT PRIMARY KEY, [DateLogged] DATETIME, [Supplier] TEXT(100), [Item] TEXT(100), [Quantity] TEXT(50), [ReceivedBy] TEXT(100)");

                    // 8. Inventory
                    EnsureTable(conn, "tbl_Inventory", "[AssetID] AUTOINCREMENT PRIMARY KEY, [ItemName] TEXT(100), [SerialNumber] TEXT(100), [Category] TEXT(50), [Value] CURRENCY, [Location] TEXT(50), [Condition] TEXT(50), [DateAcquired] DATETIME");

                    // 9. Financials
                    EnsureTable(conn, "tbl_Ledger", "[TransactionID] AUTOINCREMENT PRIMARY KEY, [TxDate] DATETIME, [VoteID] INT, [Description] MEMO, [Reference] TEXT(50), [Debit] CURRENCY DEFAULT 0, [Credit] CURRENCY DEFAULT 0, [PaymentMethod] TEXT(50), [BankReconciled] BIT DEFAULT 0, [LearnerID] INT DEFAULT 0");
                    EnsureTable(conn, "tbl_Votes", "[VoteID] AUTOINCREMENT PRIMARY KEY, [VoteNumber] TEXT(20), [VoteName] TEXT(100), [Type] TEXT(20)");
                    EnsureTable(conn, "tbl_Budget", "[BudgetID] AUTOINCREMENT PRIMARY KEY, [VoteID] INT, [FiscalYear] INT, [Amount] CURRENCY");

                    // 10. Communication & Misc
                    EnsureTable(conn, "tbl_Notices", "[NoticeID] AUTOINCREMENT PRIMARY KEY, [Message] MEMO, [Priority] TEXT(20), [DatePosted] DATETIME, [PostedBy] TEXT(50)");
                    EnsureTable(conn, "tbl_Events", "[EventID] AUTOINCREMENT PRIMARY KEY, [Title] TEXT(100), [Date] DATETIME, [Type] TEXT(50), [Description] MEMO");
                    EnsureTable(conn, "tbl_Communication", "[CommID] AUTOINCREMENT PRIMARY KEY, [DateSent] DATETIME, [Type] TEXT(20), [Recipient] TEXT(100), [Subject] TEXT(150), [Status] TEXT(20)");
                    EnsureTable(conn, "tbl_Settings", "[SettingKey] TEXT(50) PRIMARY KEY, [SettingValue] MEMO");
                    EnsureTable(conn, "tbl_Attendance", "[AttendanceID] AUTOINCREMENT PRIMARY KEY, [Date] DATETIME, [LearnerID] INT, [Status] TEXT(20), [Reason] TEXT(100)");
                    EnsureTable(conn, "tbl_Discipline", "[IncidentID] AUTOINCREMENT PRIMARY KEY, [LearnerID] INT, [Date] DATETIME, [Description] MEMO, [Severity] TEXT(20), [ReportedBy] TEXT(50)");

                    SeedVotes(conn);
                    SeedAdminUser(conn);

                    Console.WriteLine("--- DATABASE AUDIT COMPLETE ---");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DB INIT ERROR: " + ex.Message);
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
                    using (var cmd = new OleDbCommand($"CREATE TABLE [{tableName}] ({schema})", conn)) cmd.ExecuteNonQuery();
                    Console.WriteLine($"CREATED: {tableName}");
                }
                catch { /* Ignore creation errors if partial exists */ }
            }
        }

        private static void SeedVotes(OleDbConnection conn)
        {
            try
            {
                int count = (int)new OleDbCommand("SELECT COUNT(*) FROM tbl_Votes", conn).ExecuteScalar();
                if (count == 0)
                {
                    string[] votes = { "1|Office Admin|Expense", "2|Equipment|Expense", "4|Kitchen|Expense", "6|Maintenance|Expense", "A|Hostel Fees|Income" };
                    foreach (var v in votes)
                    {
                        var p = v.Split('|');
                        new OleDbCommand($"INSERT INTO tbl_Votes (VoteNumber, VoteName, Type) VALUES ('{p[0]}', '{p[1]}', '{p[2]}')", conn).ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private static void SeedAdminUser(OleDbConnection conn)
        {
            try
            {
                int count = (int)new OleDbCommand("SELECT COUNT(*) FROM tbl_Users", conn).ExecuteScalar();
                if (count == 0)
                {
                    // Default Admin: admin / admin123
                    new OleDbCommand("INSERT INTO tbl_Users (FullName, Username, Password, UserRole, Status) VALUES ('System Admin', 'admin', 'admin123', 'Administrator', 'Active')", conn).ExecuteNonQuery();
                    Console.WriteLine("Seeded Default Admin User.");
                }
            }
            catch { }
        }
    }
}