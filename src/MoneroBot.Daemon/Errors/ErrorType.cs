namespace MoneroBot.Daemon.Errors;

public abstract record Error(ErrorType ErrorType);

public enum ErrorType
{
    FiderApiErr,
    FiderPostNotFound,
    FiderUnexpectedResult,
}

public record FiderError(ErrorType ErrorType = ErrorType.FiderApiErr)
    : Error(ErrorType)
{
    public sealed record ApiError(HttpRequestException ApiException)
        : FiderError(ErrorType.FiderApiErr);

    public sealed record PostNotFound(HttpRequestException ApiException, int PostNumber)
        : FiderError(ErrorType.FiderPostNotFound);

    public sealed record UnexpectedResult(string Message)
        : FiderError(ErrorType.FiderUnexpectedResult);
}
