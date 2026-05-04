using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PersonalTaskBoard.Data;
using PersonalTaskBoard.Domain;
using PersonalTaskBoard.Tests.Helpers;

namespace PersonalTaskBoard.Tests.Api;

public class TasksEndpointsTests
{
    [Fact]
    public async Task PostTask_WhenValid_CreatesTaskAndReturnsCreated()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var response = await scope.Client.PostAsJsonAsync("/api/tasks", new { columnId = scope.SourceColumnId, title = "My Task", priority = "High" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostTask_WhenTitleMissing_ReturnsBadRequest()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var response = await scope.Client.PostAsJsonAsync("/api/tasks", new { columnId = scope.SourceColumnId, title = "", priority = "Low" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTask_WhenPriorityInvalid_ReturnsBadRequest()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var response = await scope.Client.PostAsJsonAsync("/api/tasks", new { columnId = scope.SourceColumnId, title = "Task", priority = "SuperHigh" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTask_WhenExisting_ReturnsOk()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var taskId = await CreateTaskAsync(scope.Client, scope.SourceColumnId, "Get Me", "Medium");

        var response = await scope.Client.GetAsync($"/api/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTask_WhenUnknownId_ReturnsNotFound()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var response = await scope.Client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutTask_WhenValid_UpdatesTaskAndReturnsOk()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var taskId = await CreateTaskAsync(scope.Client, scope.SourceColumnId, "Original", "Medium");

        var response = await scope.Client.PutAsJsonAsync($"/api/tasks/{taskId}", new { title = "Updated", priority = "Low" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutTask_WhenUnknownId_ReturnsNotFound()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var response = await scope.Client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", new { title = "Updated", priority = "Low" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_WhenExisting_ReturnsNoContent()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var taskId = await CreateTaskAsync(scope.Client, scope.SourceColumnId, "Delete Me", "Low");

        var response = await scope.Client.DeleteAsync($"/api/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task MoveTask_WhenTargetColumnUnknown_ReturnsNotFound()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var taskId = await CreateTaskAsync(scope.Client, scope.SourceColumnId, "Move Me", "Low");

        var response = await SendPatchAsJsonAsync(scope.Client, $"/api/tasks/{taskId}/move", new { targetColumnId = Guid.NewGuid(), targetIndex = 0 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MoveTask_WhenValid_MovesTaskToTargetColumn()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var taskId = await CreateTaskAsync(scope.Client, scope.SourceColumnId, "Move Me", "Medium");

        var moveResponse = await SendPatchAsJsonAsync(scope.Client, $"/api/tasks/{taskId}/move", new { targetColumnId = scope.TargetColumnId, targetIndex = 0 });
        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var movedTaskResponse = await scope.Client.GetAsync($"/api/tasks/{taskId}");
        var payload = await movedTaskResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(scope.TargetColumnId, payload.GetProperty("columnId").GetGuid());
    }

    [Fact]
    public async Task GetTasks_WhenFilteredByPriority_ReturnsFilteredResults()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        await CreateTaskAsync(scope.Client, scope.SourceColumnId, "High task", "High");
        await CreateTaskAsync(scope.Client, scope.SourceColumnId, "Low task", "Low");

        var response = await scope.Client.GetAsync($"/api/tasks?columnId={scope.SourceColumnId}&priority=High");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();
        Assert.NotNull(payload);
        Assert.All(payload!, t => Assert.Equal("High", t.GetProperty("priority").GetString()));
    }

    [Fact]
    public async Task GetTasks_WhenFilteredBySearch_ReturnsMatchesOnly()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        await CreateTaskAsync(scope.Client, scope.SourceColumnId, "Build API tests", "Medium");
        await CreateTaskAsync(scope.Client, scope.SourceColumnId, "Write documentation", "Medium");

        var response = await scope.Client.GetAsync($"/api/tasks?columnId={scope.SourceColumnId}&search=api");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();
        Assert.Single(payload!);
        Assert.Contains("API", payload[0].GetProperty("title").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PatchReorderTasks_WhenValid_ReordersTasks()
    {
        await using var scope = await CreateScopeWithColumnsAsync();
        var first = await CreateTaskAsync(scope.Client, scope.SourceColumnId, "First", "Medium");
        var second = await CreateTaskAsync(scope.Client, scope.SourceColumnId, "Second", "Medium");

        var reorderResponse = await SendPatchAsJsonAsync(scope.Client, "/api/tasks/reorder", new { columnId = scope.SourceColumnId, orderedIds = new[] { second, first } });
        Assert.Equal(HttpStatusCode.NoContent, reorderResponse.StatusCode);

        var listResponse = await scope.Client.GetAsync($"/api/tasks?columnId={scope.SourceColumnId}");
        var payload = await listResponse.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();
        Assert.Equal(second, payload![0].GetProperty("id").GetGuid());
        Assert.Equal(first, payload[1].GetProperty("id").GetGuid());
    }

    private static async Task<TestScope> CreateScopeWithColumnsAsync()
    {
        var factory = new TestWebApplicationFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var seeded = await SeedBoardAndColumnsAsync(factory);
        return new TestScope(factory, client, seeded.boardId, seeded.sourceColumnId, seeded.targetColumnId);
    }

    private static async Task<(Guid boardId, Guid sourceColumnId, Guid targetColumnId)> SeedBoardAndColumnsAsync(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>();
        var now = DateTimeOffset.UtcNow;
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "Board",
            CreatedAt = now,
            UpdatedAt = now
        };

        var sourceColumn = new Column
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            Name = "Source",
            DisplayOrder = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        var targetColumn = new Column
        {
            Id = Guid.NewGuid(),
            BoardId = board.Id,
            Name = "Target",
            DisplayOrder = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Boards.Add(board);
        db.Columns.AddRange(sourceColumn, targetColumn);
        await db.SaveChangesAsync();

        return (board.Id, sourceColumn.Id, targetColumn.Id);
    }

    private static async Task<Guid> CreateTaskAsync(HttpClient client, Guid columnId, string title, string priority)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new { columnId, title, priority });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> SendPatchAsJsonAsync(HttpClient client, string url, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body)
        };

        return client.SendAsync(request);
    }

    private sealed class TestScope(
        TestWebApplicationFactory factory,
        HttpClient client,
        Guid boardId,
        Guid sourceColumnId,
        Guid targetColumnId) : IAsyncDisposable
    {
        public TestWebApplicationFactory Factory { get; } = factory;
        public HttpClient Client { get; } = client;
        public Guid BoardId { get; } = boardId;
        public Guid SourceColumnId { get; } = sourceColumnId;
        public Guid TargetColumnId { get; } = targetColumnId;

        public ValueTask DisposeAsync()
        {
            Client.Dispose();
            Factory.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
