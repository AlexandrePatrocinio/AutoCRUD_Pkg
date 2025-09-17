using AutoCRUD.Data;
using AutoCRUD.Data.SqlClient;
using Tests.Models;
using Xunit;

namespace Tests;

public class SqlClientRepositoryTests : RepositoryTests
{

    public override Repository<Person, Guid> CreateRepository()
    {
        return
            new SqlClientRepository<Person, Guid>("Person", "Id", string.Empty, "Search");
    }

    public override Repository<Person, Guid> CreateRepository(string? tablename, string? keyfieldname, string connectionstring, string? searchcolumnname)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        return
            new SqlClientRepository<Person, Guid>(tablename, keyfieldname, connectionstring, searchcolumnname);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Fact]
    public void ConvertToDataTable_ShouldReturnDataTable()
    {
        // Arrange
        var repository = CreateRepository() as SqlClientRepository<Person, Guid>;
        var persons = new List<Person>
        {
            new Person { Id = Guid.NewGuid(), Alias = "Johnny", Name = "John Doe", Birthdate = new DateTime(1990, 1, 1), Stack = new[] { "C#", "SQL" } },
            new Person { Id = Guid.NewGuid(), Alias = "Janey", Name = "Jane Smith", Birthdate = new DateTime(1995, 5, 5), Stack = new[] { "JavaScript", "HTML" } }
        };

        // Act
        var dataTable = repository!.ConvertToDataTable(persons);

        // Assert
        Assert.NotNull(dataTable);
        Assert.Equal(5, dataTable.Columns.Count); // Id, Name, Alias, Birthdate, Stack
        Assert.Equal(2, dataTable.Rows.Count);    // Two persons

        Assert.Equal("Id", dataTable.Columns[0].ColumnName);
        Assert.Equal("Alias", dataTable.Columns[1].ColumnName);        
        Assert.Equal("Name", dataTable.Columns[2].ColumnName);
        Assert.Equal("Birthdate", dataTable.Columns[3].ColumnName);
        Assert.Equal("Stack", dataTable.Columns[4].ColumnName);

        Assert.Equal(persons[0].Id, dataTable.Rows[0]["Id"]);        
        Assert.Equal(persons[0].Name, dataTable.Rows[0]["Name"]);
        Assert.Equal(persons[0].Birthdate, dataTable.Rows[0]["Birthdate"]);
        Assert.Equal(string.Join(",", persons[0].Stack ?? []), dataTable.Rows[0]["Stack"]);

        Assert.Equal(persons[1].Id, dataTable.Rows[1]["Id"]);
        Assert.Equal(persons[1].Alias, dataTable.Rows[1]["Alias"]);
        Assert.Equal(persons[1].Name, dataTable.Rows[1]["Name"]);
        Assert.Equal(persons[1].Birthdate, dataTable.Rows[1]["Birthdate"]);
        Assert.Equal(string.Join(",", persons[1].Stack ?? []), dataTable.Rows[1]["Stack"]);
    }
}