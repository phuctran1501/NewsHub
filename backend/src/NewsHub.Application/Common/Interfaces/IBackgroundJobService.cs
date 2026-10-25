using System.Linq.Expressions;

namespace NewsHub.Application.Common.Interfaces;

public interface IBackgroundJobService
{
    /// <summary>
    /// Đưa một tác vụ đồng bộ vào hàng đợi thực thi ngay (Fire-and-forget).
    /// </summary>
    string Enqueue(Expression<Action> methodCall);

    /// <summary>
    /// Đưa một tác vụ bất đồng bộ vào hàng đợi thực thi ngay (Fire-and-forget).
    /// </summary>
    string Enqueue(Expression<Func<Task>> methodCall);

    /// <summary>
    /// Đưa một tác vụ bất đồng bộ cần inject service kiểu T vào hàng đợi thực thi ngay (Fire-and-forget).
    /// </summary>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Lên lịch thực thi tác vụ bất đồng bộ sau một khoảng thời gian delay.
    /// </summary>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Đăng ký hoặc cập nhật một tác vụ định kỳ lặp lại theo cron expression.
    /// </summary>
    void AddOrUpdateRecurringJob<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression);

    /// <summary>
    /// Xóa một tác vụ định kỳ theo ID.
    /// </summary>
    void RemoveRecurringJob(string recurringJobId);
}
