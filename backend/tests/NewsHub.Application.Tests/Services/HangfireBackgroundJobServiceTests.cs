using System.Linq.Expressions;
using Hangfire;
using Moq;
using NewsHub.Infrastructure.Services;
using Xunit;

namespace NewsHub.Application.Tests.Services;

public class HangfireBackgroundJobServiceTests
{
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly Mock<IRecurringJobManager> _mockRecurringJobManager;
    private readonly HangfireBackgroundJobService _service;

    public HangfireBackgroundJobServiceTests()
    {
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        _mockRecurringJobManager = new Mock<IRecurringJobManager>();

        _mockBackgroundJobClient
            .Setup(c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()))
            .Returns("test-job-id-123");

        _service = new HangfireBackgroundJobService(
            _mockBackgroundJobClient.Object,
            _mockRecurringJobManager.Object);
    }

    public interface ISampleJob
    {
        void ExecuteSync();
        Task ExecuteAsync();
    }

    [Fact]
    public void Enqueue_Action_ShouldDelegateToBackgroundJobClient()
    {
        // Act
        var jobId = _service.Enqueue(() => Console.WriteLine("Test"));

        // Assert
        Assert.Equal("test-job-id-123", jobId);
    }

    [Fact]
    public void Enqueue_Task_ShouldDelegateToBackgroundJobClient()
    {
        // Act
        var jobId = _service.Enqueue(() => Task.Delay(1));

        // Assert
        Assert.Equal("test-job-id-123", jobId);
    }

    [Fact]
    public void Enqueue_GenericServiceTask_ShouldDelegateToBackgroundJobClient()
    {
        // Act
        var jobId = _service.Enqueue<ISampleJob>(j => j.ExecuteAsync());

        // Assert
        Assert.Equal("test-job-id-123", jobId);
    }

    [Fact]
    public void Schedule_ShouldDelegateToBackgroundJobClient()
    {
        // Act
        var jobId = _service.Schedule<ISampleJob>(j => j.ExecuteAsync(), TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal("test-job-id-123", jobId);
    }

    [Fact]
    public void AddOrUpdateRecurringJob_ShouldDelegateToRecurringJobManager()
    {
        // Act
        _service.AddOrUpdateRecurringJob<ISampleJob>("daily-cleanup", j => j.ExecuteAsync(), Cron.Daily());

        // Assert
        _mockRecurringJobManager.Verify(
            m => m.AddOrUpdate(
                "daily-cleanup",
                It.IsAny<Hangfire.Common.Job>(),
                Cron.Daily(),
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }

    [Fact]
    public void RemoveRecurringJob_ShouldDelegateToRecurringJobManager()
    {
        // Act
        _service.RemoveRecurringJob("daily-cleanup");

        // Assert
        _mockRecurringJobManager.Verify(
            m => m.RemoveIfExists("daily-cleanup"),
            Times.Once);
    }
}
