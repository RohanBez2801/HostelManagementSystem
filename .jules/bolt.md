## 2024-05-22 - [Optimized ADO.NET Reader Loops]
**Learning:** `IDataReader` loops in Access/ADO.NET should always use `GetOrdinal` outside the loop to cache column indices. Repeated string-based lookups (`reader["Col"]`) inside the loop are significantly slower because they perform a hash lookup for every row.
**Action:** When working with `IDataReader` loops in `*Controller.cs`, always refactor to use cached ordinals.
