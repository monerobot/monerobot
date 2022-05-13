namespace MoneroBot.Fider.Models;

using System.Net;
using Microsoft.Extensions.Logging;

public record FiderResponse<T>(T? Result, Err? Error)
{
    public static FiderResponse<T> Ok(T result) => new(result, default);
    public static FiderResponse<T> Err(Err err) => new(default, err);
}

public record Unit();

public abstract record Err()
{
    public virtual void Log(ILogger logger, LogLevel level)
    {
        logger.Log(level, this.GetType().Name);
    }
}

public record BadRequest(Error[] Errors) : Err
{
    public override void Log(ILogger logger, LogLevel level)
    {
        logger.Log(level, "Bad Request: {errors}", this.Errors);
    }
}

public record Forbidden() : Err;

public record NotFound() : Err;

public record InternalError() : Err;

public record MalformedResponse() : Err;

public record UnkownError(HttpStatusCode StatusCode, string? Message) : Err
{
    public override void Log(ILogger logger, LogLevel level)
    {
        logger.Log(level, "Unkown API Error ({status}): {message}", this.StatusCode, this.Message);
    }
}
