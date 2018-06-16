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
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace DAL
{
    public partial class Database : IDatabase
    {
        //var parameters = new SqlParameter[]
        //{
        //    new SqlParameter() {  SqlDbType= SqlDbType.Int, Value = skip, ParameterName = "Skip" }
        //};

        //Func<SqlDataReader, Dictionary<int, string>> processer = delegate (SqlDataReader reader)
        //{
        //    var output = new Dictionary<int, string>();

        //    while (reader.Read())
        //    {
        //        int id = (int)reader["Id"];
        //        string name = (string)reader["Name"];

        //        output.Add(id, name);
        //    }

        //    return output;
        //};

        protected string _ConnectionString;

        public Database(string connection_string)
        {
            _ConnectionString = connection_string;
        }

        public DataTable ExecuteQuery(string sql_query, SqlParameter[] parameters)
        {
            return ExecuteQuery(sql_query, parameters, _ConnectionString, false);
        }

        public DataTable ExecuteQuerySp(string sql_query, SqlParameter[] parameters)
        {
            return ExecuteQuery(sql_query, parameters, _ConnectionString, true);
        }

        public List<T> ExecuteQuery<T>(string sql_query, SqlParameter[] parameters) where T : class, new()
        {
            return ExecuteQuery<T>(sql_query, parameters, _ConnectionString, false);
        }

        public T ExecuteQuery<T>(string sql_query, SqlParameter[] parameters, Func<SqlDataReader, T> processor)
        {
            return ExecuteQuery<T>(sql_query, parameters, _ConnectionString, false, processor);
        }

        public List<T> ExecuteQuerySp<T>(string sql_query, SqlParameter[] parameters) where T : class, new()
        {
            return ExecuteQuery<T>(sql_query, parameters, _ConnectionString, true);
        }

        public T ExecuteQuerySp<T>(string sql_query, SqlParameter[] parameters, Func<SqlDataReader, T> processor)
        {
            return ExecuteQuery<T>(sql_query, parameters, _ConnectionString, true, processor);
        }

        public int ExecuteNonQuery(string sql_query, SqlParameter[] parameters)
        {
            return ExecuteNonQuery(sql_query, parameters, _ConnectionString, false);
        }

        public int ExecuteNonQuerySp(string sql_query, SqlParameter[] parameters)
        {
            return ExecuteNonQuery(sql_query, parameters, _ConnectionString, true);
        }

        public T ExecuteScalar<T>(string sql_query, SqlParameter[] parameters)
        {
            return ExecuteScalar<T>(sql_query, parameters, _ConnectionString, false);
        }

        public T ExecuteScalarSp<T>(string sql_query, SqlParameter[] parameters)
        {
            return ExecuteScalar<T>(sql_query, parameters, _ConnectionString, true);
        }

        private static DataTable ExecuteQuery(string sql_query, SqlParameter[] parameters, string connection, bool stored_procedure)
        {
            if (string.IsNullOrWhiteSpace(sql_query))
                throw new ArgumentNullException("Query string is null or empty");

            if (string.IsNullOrWhiteSpace(connection))
                throw new ArgumentNullException("Connection string is null or empty");

            using (SqlConnection conn = new SqlConnection(connection))
            {
                using (SqlCommand cmd = new SqlCommand(sql_query, conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter())
                    {
                        cmd.CommandType = (stored_procedure) ? CommandType.StoredProcedure : CommandType.Text;

                        if (parameters != null)
                        {
                            foreach (SqlParameter parameter in parameters)
                                cmd.Parameters.Add(parameter);
                        }

#if (DEBUG)
                        string SQL_debug_string = GenerateSqlDebugString(sql_query, parameters);
#endif

                        DataTable dt = new DataTable();

                        conn.Open();
                        adapter.SelectCommand = cmd;
                        adapter.Fill(dt);
                        conn.Close();

                        if (parameters != null)
                        {
                            for (int i = 0; i < cmd.Parameters.Count; i++)
                                parameters[i].Value = cmd.Parameters[i].Value;
                        }

                        return dt;
                    }
                }
            }
        }

        private static List<T> ExecuteQuery<T>(string sql_query, SqlParameter[] parameters, string connection, bool stored_procedure) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(sql_query))
                throw new ArgumentNullException("Query string is null or empty");

            if (string.IsNullOrWhiteSpace(connection))
                throw new ArgumentNullException("Connection string is null or empty");

            using (SqlConnection conn = new SqlConnection(connection))
            {
                using (SqlCommand cmd = new SqlCommand(sql_query, conn))
                {
                    cmd.CommandType = (stored_procedure) ? CommandType.StoredProcedure : CommandType.Text;

                    if (parameters != null)
                    {
                        foreach (SqlParameter parameter in parameters)
                            cmd.Parameters.Add(parameter);
                    }

#if (DEBUG)
                    string SQL_debug_string = GenerateSqlDebugString(sql_query, parameters);
#endif

                    conn.Open();

                    using (SqlDataReader data_reader = cmd.ExecuteReader())
                    {
                        var output = ParseDatareaderResult<T>(data_reader, true);

                        if (parameters != null)
                        {
                            for (int i = 0; i < cmd.Parameters.Count; i++)
                                parameters[i].Value = cmd.Parameters[i].Value;
                        }

                        data_reader.Close();
                        conn.Close();

                        return output;
                    }
                }
            }
        }

        private static T ExecuteQuery<T>(string sql_query, SqlParameter[] parameters, string connection, bool stored_procedure, Func<SqlDataReader, T> processor)
        {
            if (string.IsNullOrWhiteSpace(sql_query))
                throw new ArgumentNullException("Query string is null or empty");

            if (string.IsNullOrWhiteSpace(connection))
                throw new ArgumentNullException("Connection string is null or empty");

            if (processor == null)
                throw new ArgumentNullException("Processor method is null");

            using (SqlConnection conn = new SqlConnection(connection))
            {
                using (SqlCommand cmd = new SqlCommand(sql_query, conn))
                {
                    cmd.CommandType = (stored_procedure) ? CommandType.StoredProcedure : CommandType.Text;

                    if (parameters != null)
                    {
                        foreach (SqlParameter parameter in parameters)
                            cmd.Parameters.Add(parameter);
                    }

#if (DEBUG)
                    string SQL_debug_string = GenerateSqlDebugString(sql_query, parameters);
#endif

                    conn.Open();

                    using (SqlDataReader data_reader = cmd.ExecuteReader())
                    {
                        var output = processor.Invoke(data_reader);

                        if (parameters != null)
                        {
                            for (int i = 0; i < cmd.Parameters.Count; i++)
                                parameters[i].Value = cmd.Parameters[i].Value;
                        }

                        data_reader.Close();
                        conn.Close();

                        return output;
                    }
                }
            }
        }

        private static int ExecuteNonQuery(string sql_query, SqlParameter[] parameters, string connection, bool stored_procedure)
        {
            if (string.IsNullOrWhiteSpace(sql_query))
                throw new ArgumentNullException("Query string is null or empty");

            if (string.IsNullOrWhiteSpace(connection))
                throw new ArgumentNullException("Connection string is null or empty");

            using (SqlConnection conn = new SqlConnection(connection))
            {
                using (SqlCommand cmd = new SqlCommand(sql_query, conn))
                {
                    cmd.CommandType = (stored_procedure) ? CommandType.StoredProcedure : CommandType.Text;

                    if (parameters != null)
                    {
                        foreach (SqlParameter parameter in parameters)
                            cmd.Parameters.Add(parameter);
                    }

#if (DEBUG)
                    string SQL_debug_string = GenerateSqlDebugString(sql_query, parameters);
#endif

                    conn.Open();
                    int results = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (parameters != null)
                    {
                        for (int i = 0; i < cmd.Parameters.Count; i++)
                            parameters[i].Value = cmd.Parameters[i].Value;
                    }

                    return results;
                }
            }
        }

        private static T ExecuteScalar<T>(string sql_query, SqlParameter[] parameters, string connection, bool stored_procedure)
        {
            if (string.IsNullOrWhiteSpace(sql_query))
                throw new ArgumentNullException("Query string is null or empty");

            if (string.IsNullOrWhiteSpace(connection))
                throw new ArgumentNullException("Connection string is null or empty");

            using (SqlConnection conn = new SqlConnection(connection))
            {
                using (SqlCommand cmd = new SqlCommand(sql_query, conn))
                {
                    T results = default(T);

                    cmd.CommandType = (stored_procedure) ? CommandType.StoredProcedure : CommandType.Text;

                    if (parameters != null)
                    {
                        foreach (SqlParameter parameter in parameters)
                            cmd.Parameters.Add(parameter);
                    }

#if (DEBUG)
                    string SQL_debug_string = GenerateSqlDebugString(sql_query, parameters);
#endif

                    conn.Open();

                    object buffer = cmd.ExecuteScalar();

                    if (buffer == null)
                    {
                        results = default(T);
                    }
                    else
                    {
                        if (buffer.GetType() == typeof(DBNull))
                            results = default(T);
                        else if (buffer is T)
                            return (T)buffer;
                        else
                            return (T)Convert.ChangeType(buffer, typeof(T));
                    }

                    conn.Close();

                    if (parameters != null)
                    {
                        for (int i = 0; i < cmd.Parameters.Count; i++)
                            parameters[i].Value = cmd.Parameters[i].Value;
                    }

                    return results;
                }
            }
        }

        public static DataTable GetSchema(string connection_string)
        {
            if (string.IsNullOrWhiteSpace(connection_string))
                throw new ArgumentNullException("Connection string is null or empty");

            using (SqlConnection conn = new SqlConnection(connection_string))
            {
                DataTable dt = null;

                conn.Open();
                dt = conn.GetSchema("Databases");
                conn.Close();

                return dt;
            }
        }

        /// <summary>
        /// Converts a list of IEnumerable objects to a string of comma delimited items.
        /// </summary>
        public static string GenericListToStringList<T>(IEnumerable<T> list)
        {
            return GenericListToStringList<T>(list, null, null);
        }

        /// <summary>
        /// Converts a list of IEnumerable objects to a string of comma delimited items. If a quote_character
        /// is defined, this will wrap each item with the character(s) passed.
        /// </summary>
        public static string GenericListToStringList<T>(IEnumerable<T> list, string quote_character, string quote_escape_character)
        {
            if (list == null)
                throw new ArgumentNullException("Cannot convert a null IEnumerable object");

            StringBuilder sb = new StringBuilder();
            bool first_flag = true;

            foreach (T item in list)
            {
                if (first_flag)
                    first_flag = false;
                else
                    sb.Append(",");

                if (item == null)
                {
                    sb.Append("null");
                }
                else
                {
                    string buffer = item.ToString();

                    if (!string.IsNullOrWhiteSpace(quote_escape_character))
                        buffer = buffer.Replace(quote_character, quote_escape_character);

                    if (!string.IsNullOrWhiteSpace(quote_character))
                        sb.Append(quote_character + buffer + quote_character);
                    else
                        sb.Append(buffer);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Method creates sql debugging strings with parameterized argument lists
        /// </summary>
        private static string GenerateSqlDebugString(string sql_query, SqlParameter[] parameter_list)
        {
            if (string.IsNullOrWhiteSpace(sql_query))
                throw new ArgumentNullException("Query string is null or empty");

            if (parameter_list == null || parameter_list.Length == 0)
                return sql_query;

            var value_list = new List<string>();

            foreach (var item in parameter_list)
            {
                if (item.Direction == ParameterDirection.ReturnValue)
                    continue;

                if (item.IsNullable)
                {
                    value_list.Add($"{item.ParameterName} = null");
                }
                else
                {
                    switch (item.SqlDbType)
                    {
                        case SqlDbType.Char:
                        case SqlDbType.NChar:
                        case SqlDbType.Text:
                        case SqlDbType.NText:
                        case SqlDbType.NVarChar:
                        case SqlDbType.VarChar:
                        case SqlDbType.UniqueIdentifier:
                        case SqlDbType.DateTime:
                        case SqlDbType.Date:
                        case SqlDbType.Time:
                        case SqlDbType.DateTime2:
                            value_list.Add($"@{item.ParameterName} = '{item.Value}'");
                            break;

                        default:
                            value_list.Add($"@{item.ParameterName} = {item.Value}");
                            break;
                    }
                }

            }

            return $"{sql_query} {GenericListToStringList(value_list, null, null)}";
        }

        //private static List<SqlParameter> AddReturnValueParameter(List<SqlParameter> parameters)
        //{
        //    if (parameters == null)
        //        parameters = new List<SqlParameter>();

        //    var result = parameters.Find(a => a.ParameterName == RETURN_VALUE_NAME);

        //    if (result == null)
        //        parameters.Add(new SqlParameter {ParameterName = RETURN_VALUE_NAME, SqlDbType = SqlDbType.Int, Size = -1, Value = -1, Direction = ParameterDirection.ReturnValue });

        //    return parameters;
        //}

        private static List<T> ParseDatareaderResult<T>(SqlDataReader reader, bool throwUnmappedFieldsError = false) where T : class, new()
        {
            var output_type = typeof(T);
            var results = new List<T>();
            var property_lookup = new Dictionary<string, PropertyInfo>();

            foreach (var property_info in output_type.GetProperties())
                property_lookup.Add(property_info.Name, property_info);

            T new_object;
            object field_value;

            while (reader.Read())
            {
                new_object = new T();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string column_name = reader.GetName(i);

                    if (property_lookup.TryGetValue(column_name, out PropertyInfo property_info))
                    {
                        Type property_type = property_info.PropertyType;
                        string property_name = property_info.PropertyType.FullName;

                        // in the event that we are looking at a nullable type, we need to look at the underlying type.
                        if (property_type.IsGenericType && property_type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            property_name = Nullable.GetUnderlyingType(property_type).ToString();
                            property_type = Nullable.GetUnderlyingType(property_type);
                        }

                        switch (property_name)
                        {
                            case "System.Int32":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as int? ?? null;
                                else
                                    field_value = (int)reader[column_name];
                                break;

                            case "System.String":
                                if (reader[i] == DBNull.Value)
                                    field_value = null;
                                else
                                    field_value = (string)reader[column_name];
                                break;

                            case "System.Double":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as double? ?? null;
                                else
                                    field_value = (double)reader[column_name];
                                break;

                            case "System.Float":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as float? ?? null;
                                else
                                    field_value = (float)reader[column_name];
                                break;

                            case "System.Boolean":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as bool? ?? null;
                                else
                                    field_value = (bool)reader[column_name];
                                break;

                            case "System.Boolean[]":
                                if (reader[i] == DBNull.Value)
                                {
                                    field_value = null;
                                }
                                else
                                {
                                    // inline conversion, blech. improve later.
                                    var byte_array = (byte[])reader[i];
                                    var bool_array = new bool[byte_array.Length];

                                    for (int index = 0; index < byte_array.Length; index++)
                                        bool_array[index] = Convert.ToBoolean(byte_array[index]);

                                    field_value = bool_array;
                                }
                                break;

                            case "System.DateTime":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as DateTime? ?? null;
                                else
                                    field_value = DateTime.Parse(reader[column_name].ToString());
                                break;

                            case "System.Guid":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as Guid? ?? null;
                                else
                                    field_value = (Guid)reader[column_name];
                                break;

                            case "System.Single":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as float? ?? null;
                                else
                                    field_value = float.Parse(reader[column_name].ToString());
                                break;

                            case "System.Decimal":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as decimal? ?? null;
                                else
                                    field_value = (decimal)reader[column_name];
                                break;

                            case "System.Byte":
                                if (reader[i] == DBNull.Value)
                                    field_value = null;
                                else
                                    field_value = (byte)reader[column_name];
                                break;

                            case "System.Byte[]":
                                if (reader[i] == DBNull.Value)
                                {
                                    field_value = null;
                                }
                                else
                                {
                                    string byte_array = reader[column_name].ToString();

                                    byte[] bytes = new byte[byte_array.Length * sizeof(char)];
                                    Buffer.BlockCopy(byte_array.ToCharArray(), 0, bytes, 0, bytes.Length);
                                    field_value = bytes;
                                }
                                break;

                            case "System.SByte":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as sbyte? ?? null;
                                else
                                    field_value = (sbyte)reader[column_name];
                                break;

                            case "System.Char":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as char? ?? null;
                                else
                                    field_value = (char)reader[column_name];
                                break;

                            case "System.UInt32":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as uint? ?? null;
                                else
                                    field_value = (uint)reader[column_name];
                                break;

                            case "System.Int64":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as long? ?? null;
                                else
                                    field_value = (long)reader[column_name];
                                break;

                            case "System.UInt64":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as ulong? ?? null;
                                else
                                    field_value = (ulong)reader[column_name];
                                break;

                            case "System.Object":
                                if (reader[i] == DBNull.Value)
                                    field_value = null;
                                else
                                    field_value = reader[column_name];
                                break;

                            case "System.Int16":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as short? ?? null;
                                else
                                    field_value = (short)reader[column_name];
                                break;

                            case "System.UInt16":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as ushort? ?? null;
                                else
                                    field_value = (ushort)reader[column_name];
                                break;

                            case "System.Udt":
                                // no idea how to handle a custom type
                                throw new Exception("System.Udt is an unsupported datatype");

                            case "Microsoft.SqlServer.Types.SqlGeometry":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as Microsoft.SqlServer.Types.SqlGeometry ?? null;
                                else
                                    field_value = (Microsoft.SqlServer.Types.SqlGeometry)reader[column_name];
                                break;

                            case "Microsoft.SqlServer.Types.SqlGeography":
                                if (reader[i] == DBNull.Value)
                                    field_value = reader[column_name] as Microsoft.SqlServer.Types.SqlGeography ?? null;
                                else
                                    field_value = (Microsoft.SqlServer.Types.SqlGeography)reader[column_name];
                                break;

                            default:
                                if (property_type.IsEnum)
                                {
                                    // enums are common, but don't fit into the above buckets. 
                                    if (reader[i] == DBNull.Value)
                                        field_value = null;
                                    else
                                        field_value = Enum.ToObject(property_type, reader[column_name]);
                                    break;
                                }
                                else
                                    throw new Exception($"Column '{property_lookup[column_name]}' has an unknown data type: '{property_lookup[column_name].PropertyType.FullName}'.");
                        }

                        property_lookup[column_name].SetValue(new_object, field_value, null);
                    }
                    else
                    {
                        // found a row in data reader that cannot be mapped to a property in object.
                        // might be an error, but it is dependent on the specific use case.
                        if (throwUnmappedFieldsError)
                        {
                            throw new Exception($"Cannot map datareader field '{column_name}' to object property on object '{output_type}'");
                        }
                    }
                }

                results.Add(new_object);
            }

            return results;
        }

        private DataTable ConvertObjectToDataTable<T>(IEnumerable<T> input)
        {
            DataTable dt = new DataTable();

            Type output_type = typeof(T);
            Dictionary<string, PropertyInfo> property_lookup = new Dictionary<string, PropertyInfo>();

            var object_properties = output_type.GetProperties();

            foreach (var property_info in object_properties)
                dt.Columns.Add(property_info.Name);

            foreach (var item in input)
            {
                var dr = dt.NewRow();

                foreach (var property in object_properties)
                    dr[property.Name] = output_type.GetProperty(property.Name).GetValue(item, null);

                dt.Rows.Add(dr);
            }

            return dt;
        }

        /// <summary>
        /// Generates a SqlParameter object from a generic object list.
        /// 
        /// Sample: ConvertObjectCollectionToParameter("@Foo", "dbo.SomeUserType", a_generic_object_collection);
        /// </summary>
        public SqlParameter ConvertObjectCollectionToParameter<T>(string parameter_name, string sql_type_name, IEnumerable<T> input)
        {
            DataTable dt = ConvertObjectToDataTable(input);

            SqlParameter sql_parameter = new SqlParameter(parameter_name, dt)
            {
                SqlDbType = SqlDbType.Structured,
                TypeName = sql_type_name
            };

            return sql_parameter;
        }
    }
}
