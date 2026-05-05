namespace TaskBoard.Common;

public enum ResultStatus
{
    Ok,
    NotFound,
    Forbidden,
    BadRequest
}

public record ServiceResult(ResultStatus Status, string? Error)
{
    public static ServiceResult Ok() => new(ResultStatus.Ok, null);
    public static ServiceResult NotFound(string? error = null) => new(ResultStatus.NotFound, error);
    public static ServiceResult Forbidden(string? error = null) => new(ResultStatus.Forbidden, error);
    public static ServiceResult BadRequest(string error) => new(ResultStatus.BadRequest, error);
}

public record ServiceResult<T>(ResultStatus Status, T? Value, string? Error)
{
    public static ServiceResult<T> Ok(T value) => new(ResultStatus.Ok, value, null);
    public static ServiceResult<T> NotFound(string? error = null) => new(ResultStatus.NotFound, default, error);
    public static ServiceResult<T> Forbidden(string? error = null) => new(ResultStatus.Forbidden, default, error);
    public static ServiceResult<T> BadRequest(string error) => new(ResultStatus.BadRequest, default, error);
}

