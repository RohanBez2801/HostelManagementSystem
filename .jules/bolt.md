## 2024-05-23 - ADO.NET DataReader Optimization
**Learning:** `IDataReader["ColumnName"]` performs a hash lookup on the column name for every single row.
**Action:** Always use `GetOrdinal` before the loop to get integer indices, then use `GetValue(int)` or typed accessors inside the loop for O(1) access.
