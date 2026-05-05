using Microsoft.AspNetCore.Mvc;

namespace TaskBoard.Common;

public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result) =>
        result.Status switch
        {
            ResultStatus.Ok => new OkObjectResult(result.Value),
            ResultStatus.NotFound => new NotFoundObjectResult(result.Error ?? "Not found"),
            ResultStatus.Forbidden => new ObjectResult(result.Error ?? "Forbidden") { StatusCode = StatusCodes.Status403Forbidden },
            ResultStatus.BadRequest => new BadRequestObjectResult(result.Error ?? "Bad request"),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    public static IActionResult ToActionResult(this ServiceResult result) =>
        result.Status switch
        {
            ResultStatus.Ok => new OkResult(),
            ResultStatus.NotFound => new NotFoundObjectResult(result.Error ?? "Not found"),
            ResultStatus.Forbidden => new ObjectResult(result.Error ?? "Forbidden") { StatusCode = StatusCodes.Status403Forbidden },
            ResultStatus.BadRequest => new BadRequestObjectResult(result.Error ?? "Bad request"),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
}

