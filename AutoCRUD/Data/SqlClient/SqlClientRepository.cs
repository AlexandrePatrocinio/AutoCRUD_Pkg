using System.Reflection;
using AutoCRUD.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Text;
using System.Data;

namespace AutoCRUD.Data.SqlClient;

public class SqlClientRepository<E, I>  : Repository<E, I>
where E : IEntity<I>
where I : struct 
{
    public SqlClientRepository(string? tablename, string keyfieldname, string connectionstring, string? searchcolumnname = null)
    : base(tablename, keyfieldname, connectionstring, searchcolumnname)
    {
        CreateConnection = () =>
        {
            var conn = new SqlConnection(ConnectionString);
            return conn;
        };

        CreateCommand = connection =>
        {
            var cmd = new SqlCommand();
            cmd.Connection = (SqlConnection)connection;
            return cmd;
        };

        BulkCopyAsync = async (connection, data) =>
        {
            await connection.OpenAsync();

            using var bulkCopy = new SqlBulkCopy((SqlConnection)connection);

            bulkCopy.DestinationTableName = TableName;
            bulkCopy.BatchSize = data.Count();            

            await bulkCopy.WriteToServerAsync(ConvertToDataTable(data));

            return bulkCopy.BatchSize;
        };
    }

    public override async Task<long> CountAsync() {
        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();
            
            using (var cmd = CreateCommand(conn))
            {
                cmd.CommandText = $"SELECT COUNT(id) FROM {TableName}";
                var count = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                return Convert.ToInt64(count ?? 0);
            }
        }
    }

    public override async Task<IEnumerable<E?>> SearchAsync(string? searchterm, string? orderbyfield = null, int pageNumber = 1, int pageSize = 25) {

        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1, nameof(pageNumber));
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1, nameof(pageSize));

        if (orderbyfield is not null)
        {
            var field = _propertiesSqlInfos?[TableName].properties.FirstOrDefault((p) => p.Name.Equals(orderbyfield, StringComparison.OrdinalIgnoreCase));
            if (field is null)
                throw new ArgumentException("Invalid order by clause.", nameof(orderbyfield));
            else
                orderbyfield = field.Name;
        }

        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();

            int offset = (pageNumber - 1) * pageSize;

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields}
                FROM {TableName} 
                {(string.IsNullOrWhiteSpace(searchterm) ? string.Empty : $"WHERE {SearchColumnName} like '%' + @value + '%'")}
                ORDER BY {orderbyfield ?? keyFieldName}
                OFFSET @offset ROWS
                FETCH NEXT @pageSize ROWS ONLY;";

            return await conn.QueryAsync<E?>(sql,
                string.IsNullOrWhiteSpace(searchterm)
                    ? new
                    {
                        pageSize,
                        offset
                    }
                    : new
                    {
                        value = searchterm,
                        pageSize,
                        offset
                    }
            ).ConfigureAwait(false);
        }
    }

    protected override string GetSQLInsertUpdateAsync() {
        if (_propertiesSqlInfos is not null && string.IsNullOrWhiteSpace(_propertiesSqlInfos[TableName].sqlInsertUpdate)) {
            const string chartemp1 = " = @";

            if (string.IsNullOrWhiteSpace(TableName)) throw new Exception($"Table {TableName} not found.");

            var fields = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name));
            var valuesupdate = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name + chartemp1 + p.Name));

            var sBInsertUpdate = new StringBuilder(@$"
            if not exists(Select 1 from {TableName} where {keyFieldName}=@{keyFieldName})
                INSERT INTO {TableName}({fields})
                VALUES(@{fields.Replace(",",",@")})
            else
                UPDATE {TableName}
                set {valuesupdate}
                where {keyFieldName}=@{keyFieldName}",
                1152
            );

            if (_propertiesSqlInfos is not null)
                _propertiesSqlInfos[TableName] = (
                    sBInsertUpdate.ToString().TrimEnd([' ', ',']), 
                    _propertiesSqlInfos[TableName].properties, 
                    _propertiesSqlInfos[TableName].fields
                );

            return _propertiesSqlInfos?[TableName].sqlInsertUpdate ?? string.Empty;
        }
        else 
            return _propertiesSqlInfos?[TableName].sqlInsertUpdate ?? string.Empty;
    }

    public DataTable ConvertToDataTable(IEnumerable<E> items)
    {
        DataTable dataTable = new DataTable();

        PropertyInfo[] properties = _propertiesSqlInfos?[TableName].properties.ToArray() ?? Array.Empty<PropertyInfo>();

        foreach (PropertyInfo property in properties)
        {
            dataTable.Columns.Add(property.Name, property.PropertyType.IsArray ? typeof(string) : property.PropertyType);
        }

        foreach (E item in items)
        {
            DataRow row = dataTable.NewRow();
            foreach (PropertyInfo property in properties)
            {
                row[property.Name] = property.PropertyType.IsArray ?
                    string.Join(",", (string[])(property.GetValue(item) ?? Array.Empty<string>()))
                    : property.GetValue(item);
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }    
}
