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
using System.Data;

namespace DAL.SqlMetadata
{
    public class SqlColumn
    {
        public SqlTable Table { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
        public int Length { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPk { get; set; }
        public bool IsIdentity { get; set; }
        public int ColumnOrdinal { get; set; }
        public string DefaultValue { get; set; }

        public SqlDbType SqlDataType
        {
            get
            {
                // HACK - SqlDbType has no entry for 'numeric'. Until the twerps in Redmond add this value:
                if (DataType == "numeric")
                    return SqlDbType.Decimal;

                // HACK - SqlDbType has no entry for 'sql_variant'. Until the twerps in Redmond add this value:
                if (DataType == "sql_variant")
                    return SqlDbType.Variant;

                return (SqlDbType)Enum.Parse(typeof(SqlDbType), DataType, true);
            }
        }
        public eSqlBaseType BaseType
        {
            get { return MapBaseType(); }
        }

        public SqlColumn()
        {
            Table = null;

            DataType = string.Empty;
            Length = 0;
            Precision = 0;
            Scale = 0;
            IsNullable = false;
            IsPk = false;
            IsIdentity = false;
            ColumnOrdinal = 0;
            DefaultValue = string.Empty;
        }

        public SqlColumn(SqlTable sql_table, string column_name) : this(sql_table, column_name, string.Empty, 0, 0, 0, false, false, false, 0, string.Empty) { }

        public SqlColumn(SqlTable sql_table, string column_name, string datatype, int length, int precision, int scale, bool is_nullable, bool is_pk, bool is_identity, int column_ordinal, string default_value)
        {
            Table = sql_table;

            Name = column_name;
            DataType = datatype;
            Length = length;
            Precision = precision;
            Scale = scale;
            IsNullable = is_nullable;
            IsPk = is_pk;
            IsIdentity = is_identity;
            ColumnOrdinal = column_ordinal;
            DefaultValue = default_value;
        }

        protected eSqlBaseType MapBaseType()
        {
            SqlDbType sql_type = (SqlDbType)Enum.Parse(typeof(SqlDbType), DataType, true);

            switch (sql_type)
            {
                case SqlDbType.BigInt: return eSqlBaseType.Integer;
                case SqlDbType.Binary: return eSqlBaseType.BinaryData;
                case SqlDbType.Bit: return eSqlBaseType.Bool;
                case SqlDbType.Char: return eSqlBaseType.String;
                case SqlDbType.Date: return eSqlBaseType.Time;
                case SqlDbType.DateTime: return eSqlBaseType.Time;
                case SqlDbType.DateTime2: return eSqlBaseType.Time;
                case SqlDbType.DateTimeOffset: return eSqlBaseType.Time;
                case SqlDbType.Decimal: return eSqlBaseType.Float;
                case SqlDbType.Float: return eSqlBaseType.Float;
                //case SqlDbType.Geography: 
                //case SqlDbType.Geometry: 
                case SqlDbType.Image: return eSqlBaseType.BinaryData;
                case SqlDbType.Int: return eSqlBaseType.Integer;
                case SqlDbType.Money: return eSqlBaseType.Float;
                case SqlDbType.NChar: return eSqlBaseType.String;
                case SqlDbType.NText: return eSqlBaseType.String;
                case SqlDbType.NVarChar: return eSqlBaseType.String;
                case SqlDbType.Real: return eSqlBaseType.Float;
                case SqlDbType.SmallDateTime: return eSqlBaseType.Time;
                case SqlDbType.SmallInt: return eSqlBaseType.Integer;
                case SqlDbType.SmallMoney: return eSqlBaseType.Float;
                case SqlDbType.Structured: return eSqlBaseType.String;
                case SqlDbType.Text: return eSqlBaseType.String;
                case SqlDbType.Time: return eSqlBaseType.Time;
                case SqlDbType.Timestamp: return eSqlBaseType.BinaryData;
                case SqlDbType.TinyInt: return eSqlBaseType.Integer;
                case SqlDbType.Udt: return eSqlBaseType.String;
                case SqlDbType.UniqueIdentifier: return eSqlBaseType.Guid;
                case SqlDbType.VarBinary: return eSqlBaseType.BinaryData;
                case SqlDbType.VarChar: return eSqlBaseType.String;
                case SqlDbType.Variant: return eSqlBaseType.String;
                case SqlDbType.Xml: return eSqlBaseType.String;

                default:
                    return eSqlBaseType.Unknown;
            }
        }
    }
}
