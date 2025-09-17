using System.Data;
using FluentAssertions;
using Moq;
using Npgsql;

namespace Tests;

public class StringArrayTypeHandlerTests
{
    [Theory]
    [InlineData(new[] { "C#", "T-SQL", "Java" }, "C#,T-SQL,Java")]
    [InlineData(new[] { "Python" }, "Python")]
    [InlineData(new string[0], "")]
    [InlineData(null, "")]
    public void SetValue_ShouldAssignStringListorEmptyString_WhenCalled(string[]? input, string expectedString)
    {
        // Arrange
        var handler = new StringArrayTypeHandler();
        var parameter = new NpgsqlParameter();

        // Act
        handler.SetValue(parameter, input);

        // Assert
        parameter.Value.Should().NotBeNull();
        parameter.Value.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("C#,T-SQL,Java", 3)]
    [InlineData("Python", 1)]
    [InlineData("", 1)]
    [InlineData(null, 1)]
    public void Parse_ShouldReturnEmptyArray_WhenCalledWithEmptyString(string? input, int expectedLength)
    {
        // Arrange
        var handler = new StringArrayTypeHandler();

        // Act
#pragma warning disable CS8604 // Possible null reference argument.
        var result = handler.Parse(input);
#pragma warning restore CS8604 // Possible null reference argument.

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(expectedLength);
    }
}