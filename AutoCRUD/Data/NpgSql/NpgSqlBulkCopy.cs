using NpgsqlTypes;
using System.Reflection;
using System.Text;
using Npgsql;

namespace AutoCRUD.Data.NpgSql;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class NpgSqlBulkCopy : IDisposable
{
    private bool _disposed = false;

    public NpgSqlBulkCopy(NpgsqlConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public NpgsqlConnection connection { get; private set; }

    public string? DestinationTableName { get; set; }

    public async Task WriteToServerAsync<T>(IEnumerable<T> data)
    {
        var openConnection = false;
        string? query = null;
        try
        {
            ArgumentNullException.ThrowIfNull(DestinationTableName, nameof(DestinationTableName));

            PropertyInfo[] properties = typeof(T).GetProperties();
            int colCount = properties.Length;

            NpgsqlDbType[] types = new NpgsqlDbType[colCount];

            openConnection = connection.State == System.Data.ConnectionState.Open;

            if (!openConnection)
                await connection.OpenAsync();

            (query, types) = await GetSQLInformationsAsync(colCount);

            using var writer = connection.BeginBinaryImport($"COPY {DestinationTableName} ({query}) FROM STDIN (FORMAT BINARY)");

            foreach (var t in data)
            {
                writer.StartRow();

                for (int i = 0; i < colCount; i++)
                {
                    var value = properties[i].PropertyType.IsArray ?
                    string.Join(",", (string[])(properties[i].GetValue(t) ?? Array.Empty<string>()))
                    : properties[i].GetValue(t);

                    if (value is null)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        switch (types[i])
                        {
                            case NpgsqlDbType.Uuid:
                                writer.Write((Guid)value, types[i]);
                                break;
                            case NpgsqlDbType.Bigint:
                                writer.Write((long)value, types[i]);
                                break;
                            case NpgsqlDbType.Integer:
                                writer.Write((int)value, types[i]);
                                break;
                            case NpgsqlDbType.Smallint:
                                writer.Write((short)value, types[i]);
                                break;
                            case NpgsqlDbType.Char:
                                writer.Write((char)value, types[i]);
                                break;
                            case NpgsqlDbType.Varchar:
                                writer.Write(value.ToString(), types[i]);
                                break;
                            case NpgsqlDbType.Bit:
                            case NpgsqlDbType.Boolean:
                                writer.Write((bool)value, types[i]);
                                break;
                            case NpgsqlDbType.Date:
                            case NpgsqlDbType.Timestamp:
                            case NpgsqlDbType.TimestampTz:
                                writer.Write((DateTime)value, types[i]);
                                break;
                            case NpgsqlDbType.Double:
                            case NpgsqlDbType.Money:
                            case NpgsqlDbType.Real:
                                writer.Write((float)value, types[i]);
                                break;
                            case NpgsqlTypes.NpgsqlDbType.Array:
                            case NpgsqlTypes.NpgsqlDbType.Text:
                                writer.Write(properties[i].GetValue(t), NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text);
                                break;
                                // ... other cases for different types
                        }
                    }
                }
            }

            writer.Complete();
        }
        catch (Exception ex)
        {
            throw new Exception("Error executing NpgSqlBulkCopy.WriteToServer ().", ex);
        }
        finally
        {
            if (openConnection)
                await connection.CloseAsync();
        }
    }

    private async Task<(string, NpgsqlDbType[])> GetSQLInformationsAsync(int columnCount)
    {
        var sB = new StringBuilder(384);
        NpgsqlDbType[] types = new NpgsqlDbType[columnCount];

        using var cmd = new NpgsqlCommand($"SELECT * FROM {DestinationTableName} LIMIT 1", connection);

        using var rdr = await cmd.ExecuteReaderAsync();

        if (rdr.FieldCount < columnCount)
        {
            throw new ArgumentOutOfRangeException("data", "Column count in Destination Table does not match propertie count in source data class.");
        }

        var columns = rdr.GetColumnSchema();
        for (int i = 0; i < columnCount; i++)
        {
            types[i] = columns[i].NpgsqlDbType ?? NpgsqlDbType.Varchar;
            sB.Append(", " + columns[i].ColumnName);
        }

        return (sB.ToString().TrimStart(',', ' '), types);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            connection?.Dispose();

        _disposed = true;
    }
}