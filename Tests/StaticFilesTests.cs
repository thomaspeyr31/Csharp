using System.Net;

namespace TaskBoard.Tests;

public class StaticFilesTests : IClassFixture<JwtTests.TaskBoardFactory>
{
    private readonly JwtTests.TaskBoardFactory _factory;

    public StaticFilesTests(JwtTests.TaskBoardFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Root_Serves_Index_Html()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("text/html", resp.Content.Headers.ContentType?.MediaType);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("TaskBoard", body);
    }

    [Theory]
    [InlineData("/css/styles.css", "text/css")]
    [InlineData("/js/api.js", "text/javascript")]
    [InlineData("/js/board.js", "text/javascript")]
    [InlineData("/js/realtime.js", "text/javascript")]
    [InlineData("/js/main.js", "text/javascript")]
    public async Task Static_Asset_Is_Served(string path, string expectedContentType)
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(expectedContentType, resp.Content.Headers.ContentType?.MediaType);
    }
}

