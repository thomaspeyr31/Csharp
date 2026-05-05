using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Controllers;

namespace TaskBoard.Tests;

public class BoardControllerTests
{
    [Fact]
    public void BoardController_Type_Is_Discovered_By_Compiler()
    {
        var type = typeof(BoardController);

        Assert.NotNull(type);
        Assert.True(type.IsSubclassOf(typeof(ControllerBase)));
    }

    [Fact]
    public void BoardController_Has_Authorize_Attribute()
    {
        var attr = typeof(BoardController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attr);
    }

    [Fact]
    public void BoardController_Is_Routed_To_Api_Boards()
    {
        var route = typeof(BoardController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);
        Assert.Equal("api/boards", route!.Template);
    }
}

