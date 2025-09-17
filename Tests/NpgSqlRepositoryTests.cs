using AutoCRUD.Data;
using AutoCRUD.Data.NpgSql;
using Tests.Models;

namespace Tests;

public class NpgSqlRepositoryTests : RepositoryTests
{
    public override Repository<Person, Guid> CreateRepository()
    {
        return
            new NpgSqlRepository<Person, Guid>("Persons", "Id", string.Empty, "Search");
    }

    public override Repository<Person, Guid> CreateRepository(string? tablename, string? keyfieldname, string connectionstring, string? searchcolumnname)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        return
            new NpgSqlRepository<Person, Guid>(tablename, keyfieldname, connectionstring, searchcolumnname);
#pragma warning restore CS8604 // Possible null reference argument.
    }
}