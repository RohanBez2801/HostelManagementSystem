## 2025-10-26 - ADO.NET Column Lookup Overhead
**Learning:** Using string-based column lookups (e.g., `reader["ColumnName"]`) inside an `IDataReader` loop causes repeated linear searches for the column ordinal for every row. This is O(N*M) where N is rows and M is columns.
**Action:** Always cache column ordinals using `reader.GetOrdinal("ColumnName")` before the loop and use the integer index inside the loop for O(1) access.
