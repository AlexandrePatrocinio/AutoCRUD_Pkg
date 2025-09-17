using System.Data.Common;
using AutoCRUD.Data;
using Dapper;
using FluentAssertions;
using Moq;
using Moq.Dapper;
using Tests.Models;

namespace Tests;

public abstract class RepositoryTests
{
    public abstract Repository<Person, Guid> CreateRepository();

    public abstract Repository<Person, Guid> CreateRepository(string? tablename, string? keyfieldname, string connectionstring, string? searchcolumnname);

    private Mock<DbConnection> CreateMockConnection()
    {
        var mockConnection = new Mock<DbConnection>();
        mockConnection.Setup(c => c.OpenAsync(default)).Returns(Task.CompletedTask);
        return mockConnection;
    }


    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var tablename = "Persons";
        var keyfieldname = "Id";
        var connectionstring = string.Empty;
        var searchcolumnname = "Search";
        // Act
        var repository = CreateRepository(tablename, keyfieldname, connectionstring, searchcolumnname);
        // Assert
        Assert.Equal(tablename, repository.TableName);
        Assert.Equal(keyfieldname, repository.keyFieldName);
        Assert.Equal(connectionstring, repository.ConnectionString);
        Assert.Equal(searchcolumnname, repository.SearchColumnName);
    }

    [Fact]
    public void Constructor_TableNameShouldBeEntityClassName_WhenTableNameIsNull()
    {
        // Arrange
        string? tablename = null;
        var keyfieldname = "Id";
        var connectionstring = string.Empty;
        var searchcolumnname = "Search";

        // Act
        var repository = CreateRepository(tablename, keyfieldname, connectionstring, searchcolumnname);

        // Assert
        Assert.Equal("Person", repository.TableName);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenTableNameIsInvalid()
    {
        // Arrange
        var tablename = "1InvalidTable"; // Invalid because it starts with a digit
        var keyfieldname = "Id";
        var connectionstring = string.Empty;
        var searchcolumnname = "Search";
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CreateRepository(tablename, keyfieldname, connectionstring, searchcolumnname));
    }

    [Theory]
    [InlineData("1InvalidKey")] // Invalid because it starts with a digit
    [InlineData(null)]
    public void Constructor_ShouldThrowArgumentException_WhenKeyFieldNameIsInvalid(string? keyfieldname)
    {
        // Arrange
        var tablename = "Person";
        var connectionstring = string.Empty;
        var searchcolumnname = "Search";
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CreateRepository(tablename, keyfieldname, connectionstring, searchcolumnname));
    }

    [Fact]
    public void Constructor_SearchColumnNameShouldBekeyfieldname_WhenSearchColumnNameIsNull()
    {
        // Arrange
        string tablename = "Persons";
        var keyfieldname = "Id";
        var connectionstring = string.Empty;
        string? searchcolumnname = null;

        // Act
        var repository = CreateRepository(tablename, keyfieldname, connectionstring, searchcolumnname);

        // Assert
        Assert.Equal(keyfieldname, repository.SearchColumnName);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenSearchColumnNameIsInvalid()
    {
        // Arrange
        var tablename = "Persons";
        var keyfieldname = "Id";
        var connectionstring = string.Empty;
        var searchcolumnname = "1InvalidSearch"; // Invalid because it starts with a digit
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CreateRepository(tablename, keyfieldname, connectionstring, searchcolumnname));
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount_WhenCalled()
    {
        // Arrange
        var mockConnection = CreateMockConnection();

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        var expectedCount = 5L;
        var mockCmd = new Mock<DbCommand>();
        mockCmd.Setup(c => c.ExecuteScalarAsync(default)).ReturnsAsync(expectedCount);

        _repo.CreateCommand = (connection) =>
        {
            mockCmd.Object.Connection = connection;
            return mockCmd.Object ?? throw new InvalidOperationException("Mock command is not of type NpgsqlCommand.");
        };

        // Act
        var result = await _repo.CountAsync();

        // Assert
        result.Should().Be(expectedCount);
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockCmd.Verify(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000001")]
    [InlineData("00000000-0000-0000-0000-000000000002", null)]
    public async Task FindByIdAsync_ShouldReturnEntity_WhenFound(string id, string? expectedid)
    {
        // Arrange
        var searchId = Guid.Parse(id);
        Guid? expectedGuid = expectedid is not null ? Guid.Parse(expectedid) : null;
        var expectedEntity = new Person { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Test", Alias = "Alias", Birthdate = DateTime.Now, Stack = new[] { "C#", "SQL" } };

        var mockConnection = CreateMockConnection();
        mockConnection.SetupDapperAsync(
            c => c.QueryAsync<Person?>(It.IsAny<string>(), id, null, null, null)
        ).ReturnsAsync(() => searchId == expectedEntity.Id ? [expectedEntity] : []);

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        // Act
        var result = await _repo.FindByIDAsync(searchId);

        // Assert
        Assert.Equal(expectedGuid, result?.Id);
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenRowDeleted()
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        mockConnection.SetupDapperAsync(
            c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>(), null, null, null)
        ).ReturnsAsync(1);

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        // Act
        var result = await _repo.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeTrue();
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InsertAsync_ShouldReturnTrue_WhenInsertSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new Person { Id = id, Name = "Test", Alias = "Alias", Birthdate = DateTime.Now, Stack = new[] { "C#", "SQL" } };

        var mockConnection = CreateMockConnection();
        mockConnection.SetupDapperAsync(
            c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>(), null, null, null)
        ).ReturnsAsync(1);

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        // Act
        var result = await _repo.InsertAsync(entity);

        // Assert
        result.Should().BeTrue();
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InsertAsync_ShouldReturnFalse_WhenEntityIsNull()
    {
        // Arrange
        Person? entity = null;

        var _repo = CreateRepository();

        // Act
#pragma warning disable CS8604 // Possible null reference argument.
        var result = await _repo.InsertAsync(entity);
#pragma warning restore CS8604 // Possible null reference argument.

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task InsertAsync_ShouldReturnTrue_WhenInsertBatchSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entities = new[] {
            new Person { Id = Guid.NewGuid(), Name = "Test", Alias = "Alias", Birthdate = DateTime.Now, Stack = ["C#", "T-SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "AAA", Alias = "Test2", Birthdate = DateTime.Now, Stack = ["F#", "PL/SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "BBB", Alias = "CCC", Birthdate = DateTime.Now, Stack = ["C++", "SQL"] }
        };

        var mockConnection = CreateMockConnection();

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        _repo.BulkCopyAsync = async (connection, data) =>
        {
            await connection.OpenAsync();
            return await Task.FromResult(data.Count());
        };

        // Act
        var result = await _repo.InsertAsync(entities);

        // Assert
        result.Should().Be(entities.Length);
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private (Mock<DbConnection>, Person) Arrange_FindByFieldAsyn_Test(string fieldname, object? value)
    {
        var entities = new[] {
            new Person { Id = Guid.NewGuid(), Name = "Test", Alias = "Alias", Birthdate = DateTime.Now, Stack = ["C#", "T-SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "AAA", Alias = "Test2", Birthdate = DateTime.Now, Stack = ["F#", "PL/SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "BBB", Alias = "CCC", Birthdate = DateTime.Now, Stack = ["C++", "SQL"] }
        };

        var expectedEntity = entities[0];
        var properties = expectedEntity.GetType().GetProperties();
        var prop = properties.Single(p => p.Name.Equals(fieldname, StringComparison.OrdinalIgnoreCase));
        value ??= prop.GetValue(expectedEntity);

        var mockConnection = CreateMockConnection();
        mockConnection.SetupDapperAsync(
            c => c.QueryAsync<Person?>(It.IsAny<string>(), It.IsAny<object>(), null, null, null)
        ).ReturnsAsync(() =>
        {
            var result = entities.Where(e =>
            {
                var propValue = prop.GetValue(e);
                return propValue == value || propValue?.ToString()!.Contains(value?.ToString()!, StringComparison.OrdinalIgnoreCase) == true;
            });

            return result;
        });

        return (mockConnection, expectedEntity);
    }

    [Theory]
    [InlineData("Name", "Test")]
    [InlineData("Alias", "Alias")]
    public async Task FindByFieldAsyn_SearchValue_ShouldReturnEntity_WhenFound(string fieldname, string value)
    {
        // Arrange
        var (mockConnection, expectedEntity) = Arrange_FindByFieldAsyn_Test(fieldname, value);

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        // Act
        var result = await _repo.FindByFieldAsync(fieldname, value);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedEntity.Id);
        result.Name.Should().Be(expectedEntity.Name);
        result.Alias.Should().Be(expectedEntity.Alias);
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Alias")]
    public async Task FindByFieldAsyn_SearchEntity_ShouldReturnEntity_WhenFound(string fieldname)
    {
        // Arrange
        var (mockConnection, expectedEntity) = Arrange_FindByFieldAsyn_Test(fieldname, null);

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        // Act
        var result = await _repo.FindByFieldAsync(fieldname, expectedEntity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedEntity.Id);
        result.Name.Should().Be(expectedEntity.Name);
        result.Alias.Should().Be(expectedEntity.Alias);
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData("Test", 2)]
    [InlineData("Test2", 1)]
    public async Task SearchAsync_ShouldReturnEntity_WhenFound_or_AllEntities_IfNoSearchTerm(string? searchterm, int expectedCount)
    {
        // Arrange
        var expectedEntitys = new[] {
            new Person { Id = Guid.NewGuid(), Name = "Test1", Alias = "Alias1", Birthdate = DateTime.Now, Stack = ["C#", "T-SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "AAA", Alias = "Test2", Birthdate = DateTime.Now, Stack = ["F#", "PL/SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "BBB", Alias = "CCC", Birthdate = DateTime.Now, Stack = ["C++", "SQL"] }
        };

        var mockConnection = CreateMockConnection();
        mockConnection.SetupDapperAsync(
            c => c.QueryAsync<Person?>(It.IsAny<string>(), It.IsAny<object>(), null, null, null)
        ).ReturnsAsync([..
            searchterm is null ?
            expectedEntitys :
            expectedEntitys.Where(e => e.Name.Contains(searchterm) || e.Alias.Contains(searchterm) || e.Stack!.Any(s => s.Contains(searchterm)))
        ]);

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        // Act
        var result = await _repo.SearchAsync("Test");

        // Assert
        result.Should().NotBeNull();
        result?.Count().Should().Be(expectedCount);
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(1, 2, 2)]
    [InlineData(3, 2, 1)]
    public async Task SearchAsync_ShouldReturnPaginatedEntities_WhenCalled(int pageNumber, int pageSize, int expectedCount)
    {
        // Arrange
        var expectedEntitys = new[] {
            new Person { Id = Guid.NewGuid(), Name = "Test1", Alias = "Alias1", Birthdate = DateTime.Now, Stack = ["C#", "T-SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "AAA", Alias = "Test2", Birthdate = DateTime.Now, Stack = ["F#", "PL/SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "BBB", Alias = "CCC", Birthdate = DateTime.Now, Stack = ["C++", "SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "DDD", Alias = "EEE", Birthdate = DateTime.Now, Stack = ["Java", "NoSQL"] },
            new Person { Id = Guid.NewGuid(), Name = "FFF", Alias = "GGG", Birthdate = DateTime.Now, Stack = ["Python", "GraphQL"] }
        };

        var mockConnection = CreateMockConnection();
        mockConnection.SetupDapperAsync(
            c => c.QueryAsync<Person?>(It.IsAny<string>(), It.IsAny<object>(), null, null, null)
        ).ReturnsAsync(() => expectedEntitys.Skip((pageNumber - 1) * pageSize).Take(pageSize));

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        // Act
        var result = await _repo.SearchAsync(null, null, pageNumber, pageSize);

        // Assert
        result.Should().NotBeNull();
        result?.Count().Should().Be(expectedCount);
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Name", "AAA,BBB,DDD,FFF,Test1")]
    [InlineData("Alias", "Alias1,CCC,EEE,GGG,Test2")]
    public async Task SearchAsync_ShouldReturnOrderedEntities_WhenCalled(string? orderbyfield, string expectedordered)
    {
        // Arrange
        var expectedEntitys = new[] {
            new Person { Id = Guid.NewGuid(), Name = "Test1", Alias = "Alias1", Birthdate = DateTime.Now, Stack = ["C#", "T-SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "DDD", Alias = "EEE", Birthdate = DateTime.Now, Stack = ["Java", "NoSQL"] },
            new Person { Id = Guid.NewGuid(), Name = "AAA", Alias = "Test2", Birthdate = DateTime.Now, Stack = ["F#", "PL/SQL"] },
            new Person { Id = Guid.NewGuid(), Name = "FFF", Alias = "GGG", Birthdate = DateTime.Now, Stack = ["Python", "GraphQL"] },
            new Person { Id = Guid.NewGuid(), Name = "BBB", Alias = "CCC", Birthdate = DateTime.Now, Stack = ["C++", "SQL"] }
        };

        var mockConnection = CreateMockConnection();
        mockConnection.SetupDapperAsync(
            c => c.QueryAsync<Person?>(It.IsAny<string>(), It.IsAny<object>(), null, null, null)
        ).ReturnsAsync(() => expectedEntitys.OrderBy(e => orderbyfield?.ToLower() == "name" ? e.Name : e.Alias));

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        // Act
        var result = await _repo.SearchAsync(null, orderbyfield);

        // Assert
        result.Should().NotBeNull();
        string.Join(",", result!.Select(e => orderbyfield?.ToLower() == "name" ? e!.Name : e!.Alias)).Should().Be(expectedordered);
        mockConnection.Verify(c => c.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Theory]
    [InlineData("AAA", 1, 2)]
    [InlineData("Name", 0, 1)]
    [InlineData("Alias", 1, -3)]
    public async Task SearchAsync_ShouldThrowException_WhenCalledWithInvalidPagination_or_InvalidOrderbyField(string orderbyfield, int pageNumber, int pageSize)
    {
        // Arrange
        var mockConnection = CreateMockConnection();
        mockConnection.SetupDapperAsync(
            c => c.QueryAsync<Person?>(It.IsAny<string>(), It.IsAny<object>(), null, null, null)
        ).ReturnsAsync([]);

        var _repo = CreateRepository();
        _repo.CreateConnection = () => mockConnection.Object;

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await _repo.SearchAsync(null, orderbyfield, pageNumber, pageSize));
    }        
}