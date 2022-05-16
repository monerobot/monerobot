namespace MoneroBot.Fider.Models;

using System.Net;
using Microsoft.Extensions.Logging;

public record FiderResponse<T>(T? Result, Err? Error)
{
    public static FiderResponse<T> Ok(T result) => new(result, default);
    public static FiderResponse<T> Err(Err err) => new(default, err);
}

public record Unit();

public abstract record Err();

public record BadRequest(Error[] Errors) : Err;

public record Forbidden() : Err;

public record NotFound() : Err;

public record InternalError() : Err;

public record MalformedResponse() : Err;

public record UnkownError(HttpStatusCode StatusCode, string? Message) : Err;
