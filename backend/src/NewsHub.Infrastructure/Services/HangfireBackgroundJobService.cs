using System.Linq.Expressions;
using Hangfire;
using NewsHub.Application.Common.Interfaces;

namespace NewsHub.Infrastructure.Services;

public class HangfireBackgroundJobService(
    IBackgroundJobClient backgroundJobClient,
    IRecurringJobManager recurringJobManager) : IBackgroundJobService
{
    public string Enqueue(Expression<Action> methodCall)
    {
        return backgroundJobClient.Enqueue(methodCall);
    }

    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        return backgroundJobClient.Enqueue(methodCall);
    }

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        return backgroundJobClient.Enqueue(methodCall);
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        return backgroundJobClient.Schedule(methodCall, delay);
    }

    public void AddOrUpdateRecurringJob<T>(string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression)
    {
        recurringJobManager.AddOrUpdate(recurringJobId, methodCall, cronExpression);
    }

    public void RemoveRecurringJob(string recurringJobId)
    {
        recurringJobManager.RemoveIfExists(recurringJobId);
    }
}
