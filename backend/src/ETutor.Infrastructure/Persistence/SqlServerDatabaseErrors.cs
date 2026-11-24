using ETutor.Application.Abstractions;
using Microsoft.Data.SqlClient;

namespace ETutor.Infrastructure.Persistence;

/// <summary>SQL Server error numbers for duplicate keys: 2601 (unique index) and 2627 (unique/primary key constraint).</summary>
internal sealed class SqlServerDatabaseErrors : IDatabaseErrors
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public bool IsUniqueViolation(Exception exception) =>
        exception.GetBaseException() is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };
}
