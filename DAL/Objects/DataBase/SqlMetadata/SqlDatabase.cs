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
using System.Text;

namespace DAL.SqlMetadata
{
    public class SqlDatabase
    {
        protected static string DEFAULT_CONNECTION_STRING = "Data Source=Localhost;Initial Catalog=Master;Integrated Security=SSPI;Connect Timeout=1;";

        public string Name { get; set; }
        public Dictionary<string, SqlTable> Tables { get; set; }
        public Dictionary<string, SqlScript> StoredProcedures { get; set; }
        public Dictionary<string, SqlScript> Functions { get; set; }
        public Dictionary<string, SqlConstraint> Constraints { get; set; }
        public string ConnectionString { get; set; }
        public List<Exception> ErrorList { get; set; }

        public string FormattedDatabaseName
        {
            get { return $"[{Name}]"; }
        }

        public SqlDatabase()
        {
            Reset();
        }

        private void Reset()
        {
            Name = string.Empty;
            Tables = new Dictionary<string, SqlTable>();
            StoredProcedures = new Dictionary<string, SqlScript>();
            Functions = new Dictionary<string, SqlScript>();
            Constraints = new Dictionary<string, SqlConstraint>();
            ConnectionString = string.Empty;
            ErrorList = new List<Exception>();
        }

        public bool LoadDatabaseMetadata(string database_name, string connection_string)
        {
            if (string.IsNullOrEmpty(database_name))
                throw new ArgumentException("Database name is null or empty");

            Reset();

            Name = database_name;
            ConnectionString = connection_string;

            // load and parse out table data
            try
            {
                string sql_query = GetTableData();

                DataTable dt = Database.ExecuteQuery(sql_query, null, ConnectionString);

                if (dt != null && dt.Rows.Count != 0 && dt.Columns.Count != 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        string table_name = (string)dr["TableName"];
                        string column_name = (string)dr["ColumnName"];

                        if (!Tables.ContainsKey(table_name))
                        {
                            SqlTable sql_table = new SqlTable(this, table_name);
                            Tables.Add(table_name, sql_table);
                        }

                        SqlColumn sql_column = new SqlColumn();

                        sql_column.Table = Tables[table_name];
                        sql_column.Name = (string)dr["ColumnName"];
                        sql_column.DataType = (string)dr["DataType"];
                        sql_column.Length = Convert.ToInt32(dr["Length"]);
                        sql_column.Precision = Convert.ToInt32(dr["Precision"]);
                        sql_column.IsNullable = Convert.ToBoolean(dr["IsNullable"]);
                        sql_column.IsPk = Convert.ToBoolean(dr["IsPK"]);
                        sql_column.IsIdentity = Convert.ToBoolean(dr["IsIdentity"]);
                        sql_column.ColumnOrdinal = Convert.ToInt32(dr["ColumnOrdinal"]);

                        if (Tables[table_name].Columns.ContainsKey(column_name))
                            throw new Exception($"Column {column_name} already exists in table {Tables[table_name]}");
                        else
                            Tables[table_name].Columns.Add(column_name, sql_column);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorList.Add(ex);
            }

            // get SP
            try
            {
                string sql_query = GetStoredProcedures();

                DataTable dt = Database.ExecuteQuery(sql_query, null, ConnectionString);

                if (dt != null && dt.Rows.Count != 0 && dt.Columns.Count != 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        SqlScript sql_script = new SqlScript();

                        sql_script.Name = (string)dr["Name"];
                        sql_script.Body = (string)dr["Body"];

                        if (StoredProcedures.ContainsKey(sql_script.Name))
                            StoredProcedures[sql_script.Name].Body += sql_script.Body;
                        else
                            StoredProcedures.Add(sql_script.Name, sql_script);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorList.Add(ex);
            }

            // get functions
            try
            {
                string sql_query = GetFunctions();

                DataTable dt = Database.ExecuteQuery(sql_query, null, ConnectionString);

                if (dt != null && dt.Rows.Count != 0 && dt.Columns.Count != 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        SqlScript sql_script = new SqlScript();

                        sql_script.Name = (string)dr["Name"];
                        sql_script.Body = (string)dr["Body"];

                        if (Functions.ContainsKey(sql_script.Name))
                            Functions[sql_script.Name].Body += sql_script.Body;
                        else
                            Functions.Add(sql_script.Name, sql_script);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorList.Add(ex);
            }

            // get constraints
            try
            {
                string sql_query = GetConstraints();

                DataTable dt = Database.ExecuteQuery(sql_query, null, ConnectionString);

                if (dt != null && dt.Rows.Count != 0 && dt.Columns.Count != 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        SqlConstraint sql_constraint = new SqlConstraint();

                        sql_constraint.ConstraintName = (string)dr["ConstraintName"];
                        sql_constraint.FKTable = (string)dr["FKTable"];
                        sql_constraint.FKColumn = (string)dr["FKColumn"];
                        sql_constraint.PKTable = (string)dr["PKTable"];
                        sql_constraint.PKColumn = (string)dr["PKColumn"];

                        if (Constraints.ContainsKey(sql_constraint.ConstraintName))
                            throw new Exception(string.Format("Constraint {0} already exists.", sql_constraint.ConstraintName));
                        else
                            Constraints.Add(sql_constraint.ConstraintName, sql_constraint);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorList.Add(ex);
            }

            // load default values
            try
            {
                string sql_query = GetDefaultValues();

                DataTable dt = Database.ExecuteQuery(sql_query, null, ConnectionString);

                if (dt != null && dt.Rows.Count != 0 && dt.Columns.Count != 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (Tables.ContainsKey((string)dr["TableName"]))
                            if (Tables[(string)dr["TableName"]].Columns.ContainsKey((string)dr["ColumnName"]))
                                Tables[(string)dr["TableName"]].Columns[(string)dr["ColumnName"]].DefaultValue = RemoveWrappingCharacters((string)dr["DefaultValue"]);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorList.Add(ex);
            }

            return ErrorList.Count == 0;
        }

        /// <summary>
        /// Generates SQL procedure to pull db info out of a table.
        /// Updated for SQL 2008, but several types are not supported:
        /// sysname, timestamp, hierarchyid, geometry, geography
        /// </summary>
        protected string GetTableData()
        {
            /*
            USE [<db>] 

            SELECT	sys.Objects.[Name]				            AS [TableName],
                    sys.columns.[Name]				            AS [ColumnName],
                    sys.types.[name]				            AS [DataType],
                    sys.columns.[max_length]		            AS [Length],
                    sys.columns.[precision]			            AS [Precision],
                    sys.columns.[scale]				            AS [Scale],	 
                    sys.columns.[is_nullable]		            AS [IsNullable],
                    CAST(ISNULL(PrimaryKeys.IsPK,0) AS BIT)     AS [IsPK],
                    sys.columns.[is_identity]		            AS [IsIdentity],
                    sys.columns.column_id			            AS [ColumnOrdinal]

            FROM	sys.objects 
                    INNER JOIN sys.columns ON sys.objects.object_id = sys.columns.object_id
                    INNER JOIN sys.types ON sys.columns.system_type_id = sys.types.system_type_id
                    LEFT JOIN
                    ( 
                        SELECT 	DISTINCT C.[TABLE_NAME]		AS [TableName],
                                K.[COLUMN_NAME]				AS [ColumnName],
                                1							AS [IsPK]				

                        FROM 	INFORMATION_SCHEMA.KEY_COLUMN_USAGE K
                                INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS C ON K.TABLE_NAME = C.TABLE_NAME
                        WHERE	C.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ) PrimaryKeys ON PrimaryKeys.[TableName] = sys.Objects.[Name] AND PrimaryKeys.[ColumnName] = sys.columns.[Name]

            WHERE	sys.objects.type = 'U'
            AND     sys.types.[name] NOT IN ('sysname','timestamp','hierarchyid','geometry','geography')
            AND     sys.types.is_user_defined = 0

            ORDER	BY sys.Objects.name,sys.columns.column_id
            */

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(" USE [" + Name + "]");
            sb.AppendLine(" SELECT sys.Objects.[Name] AS [TableName],");
            sb.AppendLine(" sys.columns.[Name] AS [ColumnName],");
            sb.AppendLine(" sys.types.[name] AS [DataType],");
            sb.AppendLine(" sys.columns.[max_length] AS [Length],");
            sb.AppendLine(" sys.columns.[precision] AS [Precision],");
            sb.AppendLine(" sys.columns.[scale] AS [Scale],");
            sb.AppendLine(" sys.columns.[is_nullable] AS [IsNullable],");
            sb.AppendLine(" CAST(ISNULL(PrimaryKeys.IsPK,0) AS BIT) AS [IsPK],");
            sb.AppendLine(" sys.columns.[is_identity] AS [IsIdentity],");
            sb.AppendLine(" sys.columns.column_id AS [ColumnOrdinal]");
            sb.AppendLine(" FROM sys.objects ");
            sb.AppendLine(" INNER JOIN sys.columns ON sys.objects.object_id = sys.columns.object_id");
            sb.AppendLine(" INNER JOIN sys.types ON sys.columns.system_type_id = sys.types.system_type_id");
            sb.AppendLine(" LEFT JOIN");
            sb.AppendLine(" ( ");
            sb.AppendLine(" SELECT DISTINCT C.[TABLE_NAME] AS [TableName],");
            sb.AppendLine(" K.[COLUMN_NAME] AS [ColumnName],");
            sb.AppendLine(" 1 AS [IsPK] ");
            sb.AppendLine(" FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE K");
            sb.AppendLine(" INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS C ON K.TABLE_NAME = C.TABLE_NAME");
            sb.AppendLine(" WHERE C.CONSTRAINT_TYPE = 'PRIMARY KEY'");
            sb.AppendLine(" ) PrimaryKeys ON PrimaryKeys.[TableName] = sys.Objects.[Name] AND PrimaryKeys.[ColumnName] = sys.columns.[Name]");
            sb.AppendLine(" WHERE sys.objects.type = 'U'");
            sb.AppendLine(" AND sys.types.[name] NOT IN ('sysname','timestamp','hierarchyid','geometry','geography')");
            sb.AppendLine(" AND sys.types.is_user_defined = 0");
            sb.AppendLine(" ORDER BY sys.Objects.name,sys.columns.column_id");

            return sb.ToString();
        }

        protected string GetStoredProcedures()
        {
            /*
            USE [<db>] 

            SELECT	sys.objects.name	AS [Name],
                    syscomments.text	AS [Body] 
            FROM	sys.objects
                    INNER JOIN syscomments ON sys.objects.object_id = syscomments.id
            WHERE	sys.objects.type = 'p'
            AND		sys.objects.is_ms_shipped = 0
            ORDER	BY sys.objects.name
             */

            StringBuilder sb = new StringBuilder();

            sb.Append(" USE [" + Name + "]");
            sb.Append(" SELECT sys.objects.name	AS [Name],");
            sb.Append(" syscomments.text AS [Body]");
            sb.Append(" FROM sys.objects");
            sb.Append(" INNER JOIN syscomments ON sys.objects.object_id = syscomments.id");
            sb.Append(" WHERE sys.objects.type = 'p'");
            sb.Append(" AND sys.objects.is_ms_shipped = 0");
            sb.Append(" ORDER BY sys.objects.name, syscomments.colid");

            return sb.ToString();
        }

        protected string GetFunctions()
        {
            /*
            USE [<db>] 

            SELECT	sys.objects.name	AS [Name],
                    syscomments.text	AS [Body] 
            FROM	sys.objects
                    INNER JOIN syscomments ON sys.objects.object_id = syscomments.id
            WHERE	sys.objects.type = 'fn'
            AND		sys.objects.is_ms_shipped = 0
            ORDER	BY sys.objects.name
            */

            StringBuilder sb = new StringBuilder();

            sb.Append(" USE [" + Name + "]");
            sb.Append(" SELECT sys.objects.name AS [Name],");
            sb.Append(" syscomments.text AS [Body]");
            sb.Append(" FROM sys.objects");
            sb.Append(" INNER JOIN syscomments ON sys.objects.object_id = syscomments.id");
            sb.Append(" WHERE sys.objects.type = 'fn'");
            sb.Append(" AND sys.objects.is_ms_shipped = 0");
            sb.Append(" ORDER BY sys.objects.name, syscomments.colid");

            return sb.ToString();
        }

        protected string GetConstraints()
        {
            /*
            USE [<db>] 

            SELECT	C.CONSTRAINT_NAME	AS [ConstraintName],
                    FK.TABLE_NAME		AS FKTable,
                    CU.COLUMN_NAME		AS FKColumn,
                    PK.TABLE_NAME		AS PKTable,
                    PT.COLUMN_NAME		AS PKColumn

            FROM	INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C
                    INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME
                    INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME
                    INNER JOIN 
                    (
                        SELECT	i1.TABLE_NAME, 
                                i2.COLUMN_NAME
                        FROM	INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1
                                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME
                        WHERE	i1.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ) PT ON PT.TABLE_NAME = PK.TABLE_NAME

            ORDER BY
            C.CONSTRAINT_NAME
            */

            StringBuilder sb = new StringBuilder();

            sb.Append(" USE [" + Name + "]");
            sb.Append(" SELECT C.CONSTRAINT_NAME AS [ConstraintName],");
            sb.Append(" FK.TABLE_NAME AS FKTable,");
            sb.Append(" CU.COLUMN_NAME AS FKColumn,");
            sb.Append(" PK.TABLE_NAME AS PKTable,");
            sb.Append(" PT.COLUMN_NAME AS PKColumn");

            sb.Append(" FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C");
            sb.Append(" INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME");
            sb.Append(" INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME");
            sb.Append(" INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME");
            sb.Append(" INNER JOIN (");
            sb.Append(" SELECT i1.TABLE_NAME,");
            sb.Append(" i2.COLUMN_NAME");
            sb.Append(" FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1");
            sb.Append(" INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME");
            sb.Append(" WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY'");
            sb.Append(" ) PT ON PT.TABLE_NAME = PK.TABLE_NAME");
            sb.Append(" ORDER BY");
            sb.Append(" C.CONSTRAINT_NAME");

            return sb.ToString();
        }

        protected string GetDefaultValues()
        {
            /*
            USE [<db>] 

            SELECT	sys.Objects.[Name]  AS [TableName],
                    syscolumns.name     AS [ColumnName],
                    syscomments.text    AS [DefaultValue],
            FROM	sys.objects 
                    INNER JOIN syscolumns ON sys.objects.[object_id] = syscolumns.[id]
                    INNER JOIN syscomments ON syscomments.id = syscolumns.cdefault
            WHERE	syscolumns.cdefault > 0
            AND	    is_ms_shipped = 0 
            ORDER	BY sys.objects.[name],syscolumns.colorder
            */

            StringBuilder sb = new StringBuilder();

            sb.Append(" USE [" + Name + "]");
            sb.Append(" SELECT sys.Objects.[Name] AS [TableName],");
            sb.Append(" syscolumns.[Name] AS [ColumnName],");
            sb.Append(" syscomments.text AS [DefaultValue]");
            sb.Append(" FROM sys.objects");
            sb.Append(" INNER JOIN syscolumns ON sys.objects.[object_id] = syscolumns.[id]");
            sb.Append(" INNER JOIN syscomments ON syscomments.id = syscolumns.cdefault");
            sb.Append(" WHERE syscolumns.cdefault > 0");
            sb.Append(" AND	is_ms_shipped = 0");
            sb.Append(" ORDER BY sys.objects.[name],syscolumns.colorder");

            return sb.ToString();
        }

        /// <summary>
        /// gets rid of characters that wrap a sql default value
        /// Ex: ('Something') -> Something
        /// </summary>
        protected string RemoveWrappingCharacters(string input)
        {
            if (input.Length > 1 && (input[0] == '(' || input[0] == '\''))
                input = input.Substring(1, input.Length - 2);

            if (input.Length > 1 && (input[0] == '(' || input[0] == '\''))
                input = input.Substring(1, input.Length - 2);

            return input;
        }
    }
}
