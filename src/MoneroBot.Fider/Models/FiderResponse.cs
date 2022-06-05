namespace MoneroBot.Fider.Models;

using System.Net;

public record FiderResponse<T>(T? Result, Err? Error)
{
    public static FiderResponse<T> Ok(T result) => new(result, default);

    public static FiderResponse<T> Err(Err err) => new(default, err);

    public FiderResponse<U> Map<U>(Func<T, U> map) => this switch
    {
        { Result: { } result } => FiderResponse<U>.Ok(map(result)),
        { Error: { } err } => FiderResponse<U>.Err(err),
        _ => throw new NotImplementedException()
    };

    public async Task<FiderResponse<U>> Map<U>(Func<T, Task<U>> map) => this switch
    {
        { Result: { } result } => FiderResponse<U>.Ok(await map(result)),
        { Error: { } err } => FiderResponse<U>.Err(err),
        _ => throw new NotImplementedException()
    };
}

public record Unit;

public abstract record Err;

public record BadRequest(Error[] Errors) : Err;

public record Forbidden : Err;

public record NotFound : Err;

public record InternalError : Err;

public record MalformedResponse : Err;

public record UnkownError(HttpStatusCode StatusCode, string? Message) : Err;
