using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PropertyInventory.Api.Middleware;

namespace PropertyInventory.IntegrationTests.Middleware;

/// <summary>
/// Focused unit coverage for SQL unique-constraint detection used by <see cref="GlobalExceptionHandler"/>.
/// </summary>
public class UniqueConstraintDetectionTests
{
    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public void Unique_sql_error_numbers_are_detected_as_unique_conflicts(int sqlErrorNumber)
    {
        var exception = new DbUpdateException(
            "Save failed.",
            CreateSqlException(sqlErrorNumber));

        Assert.True(GlobalExceptionHandler.IsUniqueConstraintViolation(exception));
    }

    [Fact]
    public void Nested_unique_sql_exception_is_detected()
    {
        var exception = new DbUpdateException(
            "Save failed.",
            new InvalidOperationException(
                "wrapper",
                CreateSqlException(2627)));

        Assert.True(GlobalExceptionHandler.IsUniqueConstraintViolation(exception));
    }

    [Fact]
    public void Non_unique_sql_error_number_is_not_a_unique_conflict()
    {
        var exception = new DbUpdateException(
            "Save failed.",
            CreateSqlException(547));

        Assert.False(GlobalExceptionHandler.IsUniqueConstraintViolation(exception));
    }

    [Fact]
    public void DbUpdateException_without_SqlException_is_not_a_unique_conflict()
    {
        var exception = new DbUpdateException(
            "Save failed.",
            new InvalidOperationException("UNIQUE duplicate IX_Contacts_Email"));

        Assert.False(GlobalExceptionHandler.IsUniqueConstraintViolation(exception));
    }

    /// <summary>
    /// Minimal reflection helper: SqlException has no public constructor for tests.
    /// </summary>
    private static SqlException CreateSqlException(int number)
    {
        var sqlError = CreateSqlError(number);
        var errors = (SqlErrorCollection)Activator.CreateInstance(
            typeof(SqlErrorCollection),
            nonPublic: true)!;

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(errors, [sqlError]);

        var createException = typeof(SqlException).GetMethod(
            "CreateException",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(SqlErrorCollection), typeof(string)],
            modifiers: null);

        Assert.NotNull(createException);

        return (SqlException)createException.Invoke(null, [errors, string.Empty])!;
    }

    private static SqlError CreateSqlError(int number)
    {
        var constructors = typeof(SqlError)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (var constructor in constructors.OrderByDescending(c => c.GetParameters().Length))
        {
            var parameters = constructor.GetParameters();
            var args = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var type = parameters[i].ParameterType;
                args[i] = type == typeof(int) && i == 0
                    ? number
                    : type == typeof(int)
                        ? 0
                        : type == typeof(byte)
                            ? (byte)0
                            : type == typeof(uint)
                                ? 0u
                                : type == typeof(string)
                                    ? string.Empty
                                    : type == typeof(Exception)
                                        ? null
                                        : type.IsValueType
                                            ? Activator.CreateInstance(type)
                                            : null;
            }

            try
            {
                return (SqlError)constructor.Invoke(args)!;
            }
            catch (TargetInvocationException)
            {
                // Try the next constructor signature.
            }
        }

        throw new InvalidOperationException("Unable to construct SqlError for tests.");
    }
}
