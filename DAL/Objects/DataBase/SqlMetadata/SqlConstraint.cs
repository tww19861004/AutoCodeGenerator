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

using System.Text;

namespace DAL.SqlMetadata
{
    public class SqlConstraint
    {
        public string ConstraintName { get; set; }
        public string FKTable { get; set; }
        public string FKColumn { get; set; }
        public string PKTable { get; set; }
        public string PKColumn { get; set; }

        public SqlConstraint()
        {
            ConstraintName = string.Empty;
            FKTable = string.Empty;
            FKColumn = string.Empty;
            PKTable = string.Empty;
            PKColumn = string.Empty;
        }

        public SqlConstraint(string constraint_name, string fk_table, string fk_column, string pk_table, string pk_column)
        {
            ConstraintName = constraint_name;
            FKTable = fk_table;
            FKColumn = fk_column;
            PKTable = pk_table;
            PKColumn = pk_column;
        }

        public string GenerateSQLScript()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(" ALTER TABLE " + FKTable);
            sb.AppendLine(" ADD CONSTRAINT " + ConstraintName);
            sb.AppendLine(" FOREIGN KEY(" + FKColumn + ")");
            sb.AppendLine(" REFERENCES " + PKTable + "(" + PKColumn + ");");

            return sb.ToString();
        }

        public void GenerateConstraintName()
        {
            ConstraintName = $"FK_{FKTable}_{PKTable}_{GetHashCode()}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this.GetType() != obj.GetType())
                return false;

            SqlConstraint other = (SqlConstraint)obj;

            if (FKTable != other.FKTable)
                return false;

            if (FKColumn != other.FKColumn)
                return false;

            if (PKTable != other.PKTable)
                return false;

            if (PKColumn != other.PKColumn)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (FKTable.GetHashCode() * 1709) ^ (FKColumn.GetHashCode() * 1997) ^ (PKTable.GetHashCode() * 83) ^ (PKColumn.GetHashCode() * 389);
            }
        }

        public override string ToString()
        {
            return $"[{PKTable}].[{PKColumn}] = [{FKTable}].[{FKColumn}]";
        }
    }
}
