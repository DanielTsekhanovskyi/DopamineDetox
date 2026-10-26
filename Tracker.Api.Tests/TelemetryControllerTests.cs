using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Tracker.Api.Controllers;
// Перевір, щоб тут був правильний namespace твого контексту бази даних (наприклад, Tracker.Api.Data або Tracker.Api.Models)
using Tracker.Api.Data; 
using Tracker.Api.Models;

namespace Tracker.Api.Tests;

public class TelemetryControllerTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new AppDbContext(options);
    }

    [Fact]
    public void GetWeeklyStats_ReturnsOnlyDataForLast7Days()
    {
        // Arrange (Підготовка)
        using var context = GetInMemoryDbContext();
        
        // Додаємо старий запис (10 днів тому) - не має потрапити у видачу
        context.ActivityLogs.Add(new ActivityLog 
        { 
            Id = 1, ActivityName = "OldGame", Category = "Distracting", 
            StartTime = DateTime.UtcNow.AddDays(-10), DurationSec = 600 
        });
        
        // Додаємо свіжий запис (2 дні тому) - має потрапити у видачу
        context.ActivityLogs.Add(new ActivityLog 
        { 
            Id = 2, ActivityName = "VS Code", Category = "Productive", 
            StartTime = DateTime.UtcNow.AddDays(-2), DurationSec = 3600 
        });
        
        context.SaveChanges();

        var controller = new TelemetryController(context);

        // Act (Дія)
        var result = controller.GetWeeklyStats() as OkObjectResult;
        var returnedLogs = result?.Value as IEnumerable<ActivityLog>;

        // Assert (Перевірка результату)
        Assert.NotNull(result);
        Assert.NotNull(returnedLogs);
        Assert.Single(returnedLogs); // Має повернутися лише 1 запис (свіжий)
        Assert.Equal("VS Code", returnedLogs.First().ActivityName);
    }
    
    [Fact]
    public void GetDailyStats_ReturnsOnlyTodayData()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        // Вчорашній лог
        context.ActivityLogs.Add(new ActivityLog 
        { 
            Id = 3, ActivityName = "YesterdayApp", Category = "Neutral", 
            StartTime = DateTime.UtcNow.AddDays(-1), DurationSec = 100 
        });
        // Сьогоднішній лог
        context.ActivityLogs.Add(new ActivityLog 
        { 
            Id = 4, ActivityName = "TodayApp", Category = "Productive", 
            StartTime = DateTime.UtcNow, DurationSec = 500 
        });
        
        context.SaveChanges();
        var controller = new TelemetryController(context);

        // Act
        var result = controller.GetDailyStats() as OkObjectResult;
        var returnedLogs = result?.Value as IEnumerable<ActivityLog>;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(returnedLogs);
        Assert.Single(returnedLogs); // Очікуємо лише 1 запис (за сьогодні)
        Assert.Equal("TodayApp", returnedLogs.First().ActivityName);
    }

    [Fact]
    public void GetWeeklyStats_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = new TelemetryController(context);

        // Act
        var result = controller.GetWeeklyStats() as OkObjectResult;
        var returnedLogs = result?.Value as IEnumerable<ActivityLog>;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(returnedLogs);
        Assert.Empty(returnedLogs); // Очікуємо порожній список, а не помилку
    }
}