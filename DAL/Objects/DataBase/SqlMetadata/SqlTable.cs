/*
The MIT License (MIT)

Copyright (c) 2007 Roger Hill

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files 
(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, 
publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do 
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Data;

namespace DAL.SqlMetadata
{
    public class SqlTable
    {
        public SqlDatabase Database { get; set; }
        public string Name { get; set; }
        public Dictionary<string, SqlColumn> Columns { get; set; }
        public Dictionary<string, SqlConstraint> TableConsraints
        {
            get
            {
                Dictionary<string, SqlConstraint> output = new Dictionary<string, SqlConstraint>();

                if (Database != null)
                {
                    foreach (KeyValuePair<string, SqlConstraint> kvp in Database.Constraints)
                    {
                        if (kvp.Value.PKTable == Name || kvp.Value.FKTable == Name)
                        {
                            if (!output.ContainsKey(kvp.Key))
                                output.Add(kvp.Key, kvp.Value);
                        }
                    }
                }

                return output;
            }
        }
        public List<SqlColumn> PkList
        {
            get
            {
                List<SqlColumn> output = new List<SqlColumn>();

                foreach (SqlColumn sql_column in Columns.Values)
                {
                    if (sql_column.IsPk)
                        output.Add(sql_column);
                }

                return output;
            }
        }

        public SqlTable()
        {
            Database = null;
            Name = string.Empty;
            Columns = new Dictionary<string, SqlColumn>();
        }

        public SqlTable(SqlDatabase sql_database, string table_name)
        {
            Database = sql_database;
            Name = table_name;
            Columns = new Dictionary<string, SqlColumn>();
        }

        protected void GetColumnMetaData(DataTable dt)
        {
            Columns.Clear();

            if (dt != null && dt.Rows.Count != 0 && dt.Columns.Count != 0)
            {
                SqlColumn obj;

                foreach (DataRow dr in dt.Rows)
                {
                    // For some strange reason, if a column's type is nvarchar SQL2K
                    // will add an additional entry to the syscolumns table with the 
                    // type listed as a sysname. Since we don't want duplicate entries, omit.

                    //if ((string)dr["DataType"] == "sysname")
                    //    continue;

                    obj = new SqlColumn
                    (
                            this,
                            (string)dr["ColumnName"],
                            (string)dr["DataType"],
                            (int)dr["Length"],
                            (int)dr["Precision"],
                            (int)dr["Scale"],
                            (bool)dr["IsNullable"],
                            (bool)dr["IsPK"],
                            (bool)dr["IsIdentity"],
                            (int)dr["ColumnOrdinal"],
                            (string)dr["DefaultValue"]
                    );

                    Columns.Add(obj.Name, obj);
                }
            }
            else
            {
                throw new Exception("Cannot retrieve metadata for table " + Name + ".");
            }
        }
    }
}
