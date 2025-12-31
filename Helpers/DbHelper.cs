using System;
using System.Data.OleDb;
using System.IO;
using System.Threading;

namespace HostelManagementSystem.Helpers
{
    public static class DbHelper
    {
        private static readonly string _connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "HostelDb.accdb")};";

        public static OleDbConnection GetConnection()
        {
            var conn = new OleDbConnection(_connString);
            int retries = 3;
            int delay = 100; // milliseconds

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    conn.Open();
                    return conn;
                }
                catch (OleDbException ex)
                {
                    // Access Error 3045: "Could not use ''; file already in use."
                    // Access Error 3006: "Database is exclusively locked."
                    // We check if it's the last attempt
                    if (i == retries - 1) throw;

                    Thread.Sleep(delay);
                    delay += 100; // Backoff
                }
                catch (InvalidOperationException)
                {
                    // Connection state issues
                    if (i == retries - 1) throw;
                    Thread.Sleep(delay);
                }
            }
            return conn; // Should not reach here
        }
    }
}
