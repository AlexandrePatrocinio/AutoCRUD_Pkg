using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoCRUD.Models;
using Dapper;

namespace AutoCRUD.Data;

public abstract class Repository<E, I> : IRepository<E, I>
where E : IEntity<I>
where I : struct
{
    protected static readonly Regex _securityRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$");

    protected Dictionary<string, (string? sqlInsertUpdate, IEnumerable<PropertyInfo> properties, string fields)>? _propertiesSqlInfos;

    public Repository(string? tablename, string keyfieldname, string connectionstring, string? searchcolumnname = null)
    {
        ConnectionString = connectionstring;

        _propertiesSqlInfos ??= new Dictionary<string, (string?, IEnumerable<PropertyInfo>, string)>();

        if (!string.IsNullOrWhiteSpace(tablename) && !_securityRegex.IsMatch(tablename))
            throw new ArgumentException("Invalid table name.", nameof(tablename));

        if (string.IsNullOrWhiteSpace(keyfieldname) || !_securityRegex.IsMatch(keyfieldname))
            throw new ArgumentException("Invalid key field name.", nameof(keyfieldname));

        if (!string.IsNullOrWhiteSpace(searchcolumnname) && !_securityRegex.IsMatch(searchcolumnname))
            throw new ArgumentException("Invalid search column name.", nameof(searchcolumnname));

        if (string.IsNullOrWhiteSpace(searchcolumnname)) searchcolumnname = null;

        TableName = tablename ?? typeof(E).Name;
        keyFieldName = keyfieldname;
        SearchColumnName = searchcolumnname ?? keyfieldname;

        var properties = typeof(E).GetProperties().Where(
            (p) => !p.Name.Equals(SearchColumnName, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Equals(keyfieldname, StringComparison.OrdinalIgnoreCase)
        );

        var fields = properties.Select((p) => p.Name);

        _propertiesSqlInfos.Add(TableName, (null, properties, String.Join(',', fields)));

        SqlMapper.AddTypeHandler(new StringArrayTypeHandler());
    }

    public string TableName { get; private set; }

    public string keyFieldName { get; private set; }

    public string SearchColumnName { get; private set; }

    public string ConnectionString { get; set; }

    public Func<DbConnection> CreateConnection { get; set; } = null!;

    public Func<DbConnection, DbCommand> CreateCommand { get; set; } = null!;

    public Func<DbConnection, IEnumerable<E>, Task<int>> BulkCopyAsync { get; set; } = null!;

    public virtual async Task<long> CountAsync()
    {
        using (var connection = CreateConnection())
        {
            await connection.OpenAsync();

            using (var cmd = CreateCommand(connection))
            {
                cmd.CommandText = $"SELECT COUNT({keyFieldName}) FROM {TableName}";

                var result = (long)(await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);

                return result;
            }
        }
    }

    public virtual async Task<bool> DeleteAsync(I id) {
        using (var conn = CreateConnection())
        {
            string sql = @$"
                Delete from {TableName}
                WHERE {keyFieldName} = @value";

            var affecteds = await conn.ExecuteAsync(sql, new { value = id }).ConfigureAwait(false);

            return affecteds > 0;
        }
    }

    public virtual async Task<E?> FindByFieldAsync(string fieldname, object searchvalue)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(fieldname, nameof(fieldname));

        var field = _propertiesSqlInfos?[TableName].properties.FirstOrDefault((p) => p.Name.Equals(fieldname, StringComparison.OrdinalIgnoreCase));
        if (field is null)
            throw new ArgumentException("Invalid field name.", nameof(fieldname));
        else
            fieldname = field.Name;

        searchvalue = searchvalue ?? throw new ArgumentNullException(nameof(searchvalue));

        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();

            string sql = @$"
                SELECT {_propertiesSqlInfos?[TableName].fields}
                FROM {TableName} 
                WHERE {fieldname} = @value";

            var result = (
                await conn.QueryAsync<E?>(sql, new { value = searchvalue })
                .ConfigureAwait(false)
            ).FirstOrDefault();

            return result;
        }
    }

    public virtual async Task<E?> FindByFieldAsync(string fieldname, E searchentity)
    {
        var field = _propertiesSqlInfos?[TableName].properties.FirstOrDefault((p) => p.Name.Equals(fieldname, StringComparison.OrdinalIgnoreCase));
        if (field is null)
            throw new ArgumentException("Invalid field name.", nameof(fieldname));
        else
            fieldname = field.Name;

        return await FindByFieldAsync(fieldname, field?.GetValue(searchentity) ?? string.Empty);
    }

    public virtual async Task<E?> FindByIDAsync(I id) => await FindByFieldAsync(keyFieldName, id);

    public virtual async Task<bool> InsertAsync(E data)
    {

        if (data is null) return false;

        using (var conn = CreateConnection())
        {
            try
            {
                var sql = GetSQLInsertUpdateAsync();

                var rowsAffected = await conn.ExecuteAsync(sql, data).ConfigureAwait(false);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }

    public virtual async Task<int> InsertAsync(IEnumerable<E> data)
    {
        if (data is null) return 0;

        using var conn = CreateConnection();

        var result = await BulkCopyAsync(conn, data);
        
        return result;
    }    

    public virtual async Task<IEnumerable<E?>> SearchAsync(string? searchterm, string? orderbyfield = null, int pageNumber = 1, int pageSize = 25)
    {
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
                {(string.IsNullOrWhiteSpace(searchterm) ? string.Empty : $"WHERE {SearchColumnName} ILIKE '%' || @value || '%'")}
                ORDER BY {orderbyfield ?? keyFieldName}
                LIMIT @pageSize OFFSET @offset";

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

    protected abstract string GetSQLInsertUpdateAsync();
}