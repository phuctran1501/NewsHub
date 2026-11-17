namespace NewsHub.Application.Common.Extensions;

using System;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Cung cấp các phương thức mở rộng nhận diện chính xác nguyên nhân lỗi cơ sở dữ liệu từ DbUpdateException.
/// Phân biệt giữa vi phạm Unique, Foreign Key, Concurrency và các lỗi hạ tầng khác.
/// Hỗ trợ PostgreSQL (Npgsql) qua SqlState, SQLite và generic fallback.
/// </summary>
public static class DatabaseExceptionExtensions
{
    private const string PostgresUniqueViolationCode = "23505";
    private const string PostgresForeignKeyViolationCode = "23503";

    /// <summary>
    /// Kiểm tra xem ngoại lệ DbUpdateException có bắt nguồn từ vi phạm ràng buộc Unique (trùng lặp dữ liệu) hay không.
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            return false;
        }

        for (Exception? current = exception.InnerException; current is not null; current = current.InnerException)
        {
            var type = current.GetType();
            if (type.Name == "PostgresException")
            {
                var sqlStateProp = type.GetProperty("SqlState");
                var sqlState = sqlStateProp?.GetValue(current) as string;
                if (sqlState == PostgresUniqueViolationCode)
                {
                    return true;
                }
            }

            var message = current.Message;
            if (message.Contains("23505", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("duplicate entry", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return exception.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("duplicate entry", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kiểm tra xem ngoại lệ DbUpdateException có bắt nguồn từ vi phạm ràng buộc Foreign Key (khóa ngoại) hay không.
    /// </summary>
    public static bool IsForeignKeyViolation(this DbUpdateException exception)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            return false;
        }

        for (Exception? current = exception.InnerException; current is not null; current = current.InnerException)
        {
            var type = current.GetType();
            if (type.Name == "PostgresException")
            {
                var sqlStateProp = type.GetProperty("SqlState");
                var sqlState = sqlStateProp?.GetValue(current) as string;
                if (sqlState == PostgresForeignKeyViolationCode)
                {
                    return true;
                }
            }

            var message = current.Message;
            if (message.Contains("23503", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("reference constraint", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("violates foreign key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return exception.Message.Contains("23503", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("reference constraint", StringComparison.OrdinalIgnoreCase) ||
               exception.Message.Contains("violates foreign key", StringComparison.OrdinalIgnoreCase);
    }
}
