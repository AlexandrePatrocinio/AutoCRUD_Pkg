using Npgsql;
using System.Text;
using AutoCRUD.Models;

namespace AutoCRUD.Data.NpgSql;

public class NpgSqlRepository<E,I> : Repository<E, I> 
where E : IEntity<I>
where I : struct
{
    public NpgSqlRepository(string? tablename, string keyfieldname, string connectionstring, string? searchcolumnname = null)
        : base(tablename, keyfieldname, connectionstring, searchcolumnname)
    {
        if (_propertiesSqlInfos is not null && tablename is not null &&
            _propertiesSqlInfos.TryGetValue(tablename, out var sqlInfo))
        {
            var fields = sqlInfo.properties.Select((p) =>
                p.PropertyType != typeof(string[])
                ? p.Name
                : $"REPLACE(REPLACE({p.Name}, '{{',''), '}}','') AS {p.Name}"
            );

            _propertiesSqlInfos[tablename] = (sqlInfo.sqlInsertUpdate, sqlInfo.properties, String.Join(',', fields));
        }

        CreateConnection = () =>
        {
            var conn = new NpgsqlConnection(ConnectionString);
            return conn;
        };

        CreateCommand = connection =>
        {
            var cmd = new NpgsqlCommand();
            cmd.Connection = (NpgsqlConnection)connection;
            return cmd;
        };
        
        BulkCopyAsync = async (connection, data) =>
        {
            using var bulkCopy = new NpgSqlBulkCopy((NpgsqlConnection)connection);

            bulkCopy.DestinationTableName = TableName;          

            await bulkCopy.WriteToServerAsync(data);

            return data.Count();
        };
    }

    protected override string GetSQLInsertUpdateAsync() {
        if (_propertiesSqlInfos is not null && string.IsNullOrWhiteSpace(_propertiesSqlInfos[TableName].sqlInsertUpdate)) {
            const string chartemp1 = " = EXCLUDED.";    

            if (string.IsNullOrWhiteSpace(TableName)) throw new NpgsqlException($"Table {TableName} not found.");

            var fields = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name));
            var valuesupdate = String.Join(',', _propertiesSqlInfos[TableName].properties.Select((p)=> p.Name + chartemp1 + p.Name));

            var sBInsertUpdate = new StringBuilder(@$"
                INSERT INTO {TableName}({fields})
                    VALUES(@{fields.Replace(",",",@")})
                ON CONFLICT ({keyFieldName}) DO 
                    UPDATE SET {valuesupdate}", 
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
} 