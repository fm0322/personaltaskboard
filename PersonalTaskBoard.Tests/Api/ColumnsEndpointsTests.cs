using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PersonalTaskBoard.Data;
using PersonalTaskBoard.Domain;
using PersonalTaskBoard.Tests.Helpers;

namespace PersonalTaskBoard.Tests.Api;

public class ColumnsEndpointsTests
{
    [Fact]
    public async Task GetColumns_WhenNoColumns_ReturnsEmptyList()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        var response = await scope.Client.GetAsync($"/api/columns?boardId={scope.BoardId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();
        Assert.NotNull(payload);
        Assert.Empty(payload!);
    }

    [Fact]
    public async Task PostColumn_WhenValid_CreatesColumnAndReturnsCreated()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        var response = await scope.Client.PostAsJsonAsync($"/api/columns?boardId={scope.BoardId}", new { name = "Backlog", wipLimit = (int?)null });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostColumn_WhenNameMissing_ReturnsBadRequest()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        var response = await scope.Client.PostAsJsonAsync($"/api/columns?boardId={scope.BoardId}", new { name = "", wipLimit = (int?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostColumn_WhenWipLimitIsZero_ReturnsBadRequest()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        var response = await scope.Client.PostAsJsonAsync($"/api/columns?boardId={scope.BoardId}", new { name = "Backlog", wipLimit = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutColumn_WhenValid_UpdatesColumnAndReturnsOk()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        var created = await CreateColumnAsync(scope.Client, scope.BoardId, "Original", null);

        var response = await scope.Client.PutAsJsonAsync($"/api/columns/{created.Id}", new { name = "Updated", wipLimit = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutColumn_WhenUnknownId_ReturnsNotFound()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        var response = await scope.Client.PutAsJsonAsync($"/api/columns/{Guid.NewGuid()}", new { name = "Updated", wipLimit = (int?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteColumn_WhenExisting_ReturnsNoContent()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        var created = await CreateColumnAsync(scope.Client, scope.BoardId, "ToDelete", null);

        var response = await scope.Client.DeleteAsync($"/api/columns/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteColumn_WhenUnknownId_ReturnsNotFound()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        var response = await scope.Client.DeleteAsync($"/api/columns/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchReorderColumns_WhenValid_ReordersColumns()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        var first = await CreateColumnAsync(scope.Client, scope.BoardId, "First", null);
        var second = await CreateColumnAsync(scope.Client, scope.BoardId, "Second", null);

        var reorderResponse = await SendPatchAsJsonAsync(scope.Client, "/api/columns/reorder", new { ids = new[] { second.Id, first.Id } });
        Assert.Equal(HttpStatusCode.NoContent, reorderResponse.StatusCode);

        var getResponse = await scope.Client.GetAsync($"/api/columns?boardId={scope.BoardId}");
        var payload = await getResponse.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();
        Assert.NotNull(payload);
        Assert.Equal(second.Id, payload![0].GetProperty("id").GetGuid());
        Assert.Equal(first.Id, payload[1].GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task PatchReorderColumns_WhenUnknownId_ReturnsNotFound()
    {
        await using var scope = await CreateScopeWithBoardAsync();
        _ = await CreateColumnAsync(scope.Client, scope.BoardId, "Only", null);

        var response = await SendPatchAsJsonAsync(scope.Client, "/api/columns/reorder", new { ids = new[] { Guid.NewGuid() } });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Seed_RunTwice_YieldsSameRowCount()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.ResetDatabaseAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>();

        for (var i = 0; i < 2; i++)
        {
            if (!db.Boards.Any())
            {
                var now = DateTimeOffset.UtcNow;
                var board = new Board { Id = Guid.NewGuid(), Name = "My Board", CreatedAt = now, UpdatedAt = now };
                db.Boards.Add(board);
                db.Columns.AddRange(
                    new Column { Id = Guid.NewGuid(), BoardId = board.Id, Name = "To Do", DisplayOrder = 0, CreatedAt = now, UpdatedAt = now },
                    new Column { Id = Guid.NewGuid(), BoardId = board.Id, Name = "In Progress", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
                    new Column { Id = Guid.NewGuid(), BoardId = board.Id, Name = "Done", DisplayOrder = 2, CreatedAt = now, UpdatedAt = now });

                db.SaveChanges();
            }
        }

        Assert.Equal(1, db.Boards.Count());
        Assert.Equal(3, db.Columns.Count());
    }

    private static async Task<TestScope> CreateScopeWithBoardAsync()
    {
        var factory = new TestWebApplicationFactory();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();
        var boardId = await SeedBoardAsync(factory);
        return new TestScope(factory, client, boardId);
    }

    private static async Task<Guid> SeedBoardAsync(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>();
        var now = DateTimeOffset.UtcNow;
        var board = new Board { Id = Guid.NewGuid(), Name = "Test Board", CreatedAt = now, UpdatedAt = now };
        db.Boards.Add(board);
        await db.SaveChangesAsync();
        return board.Id;
    }

    private static async Task<(Guid Id, string Name)> CreateColumnAsync(HttpClient client, Guid boardId, string name, int? wipLimit)
    {
        var response = await client.PostAsJsonAsync($"/api/columns?boardId={boardId}", new { name, wipLimit });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return (created.GetProperty("id").GetGuid(), created.GetProperty("name").GetString()!);
    }

    private static Task<HttpResponseMessage> SendPatchAsJsonAsync(HttpClient client, string url, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body)
        };
        return client.SendAsync(request);
    }

    private sealed class TestScope(TestWebApplicationFactory factory, HttpClient client, Guid boardId) : IAsyncDisposable
    {
        public TestWebApplicationFactory Factory { get; } = factory;
        public HttpClient Client { get; } = client;
        public Guid BoardId { get; } = boardId;

        public ValueTask DisposeAsync()
        {
            Client.Dispose();
            Factory.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
