## 2024-05-23 - ADO.NET Loop Optimization
**Learning:** In ADO.NET `while(reader.Read())` loops, repeatedly calling `reader["ColumnName"]` performs a hash lookup for every row, which is inefficient for large datasets.
**Action:** Always cache the column ordinals using `reader.GetOrdinal("ColumnName")` *before* the loop, and access the reader using these integer indices (e.g., `reader[ordCol]`). This reduces overhead significantly.
