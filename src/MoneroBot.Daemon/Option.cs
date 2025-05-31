namespace MoneroBot.Daemon;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public sealed record Result<T, E>
{
    private readonly bool _isOk;
    private readonly T? _value;
    private readonly E? _error;

    private Result(bool isOk, T? value = default, E? error = default)
    {
        this._isOk = isOk;
        this._value = value;
        this._error = error;
    }

    public T? Unwrap() => _value;

    public bool IsOk([NotNullWhen(true)] out T? value, [NotNullWhen(false)] out E? error)
    {
        value = this._value;
        error = this._error;
        return this._isOk;
    }

    public bool IsOk([NotNullWhen(true)] out T? value) => IsOk(out value, out _);

    public bool IsErr([NotNullWhen(true)] out E? error) => IsOk(out _, out error) is false;

    public bool IsErr([NotNullWhen(true)] out E? error, [NotNullWhen(false)] out T? value) => !IsOk(out value, out error);

    public Result<N, E> Map<N>(Func<T, N> selector) => IsOk(out var value, out var error)
        ? Result<N, E>.Ok(selector(value))
        : Result<N, E>.Err(error);

    public Result<T, E> ErrIf(Func<T, bool> predicate, E error) => IsOk(out var value, out var _) && predicate(value) is false
        ? Err(error)
        : this;

    public async ValueTask<Result<N, E>> MapAsync<N>(Func<T, ValueTask<N>> selector) => IsOk(out var value, out var error)
        ? Result<N, E>.Ok(await selector(value))
        : Result<N, E>.Err(error);

    public static Result<T, E> Ok(T value) => new(isOk: true, value: value);

    public static Result<T, E> Err(E error) => new(isOk: false, error: error);
}

public record Option<T>
        where T : notnull
{
    public T? UnsafeValue { get; init; }
    public bool HasValue { get; init; }

    public Option(T value)
    {
        this.HasValue = true;
        this.UnsafeValue = value;
    }

    public Option()
    {
        this.UnsafeValue = default(T?);
        this.HasValue = false;
    }
}

public static class Option
{
    public static Option<T> For<T>(T? value)
        where T : class => value is null ? None<T>() : Some(value);

    public static Option<T> For<T>(T? value)
        where T : struct => value is null ? None<T>() : Some(value.Value);

    public static Option<T> Some<T>(T value)
        where T : notnull => new (value);

    public static Option<T> None<T>()
        where T : notnull => new ();
}

public static class ReferenceOption
{
    public static T? Unwrap<T>(this Option<T> option, T? fallback = default)
        where T : class => option.HasValue ? option.UnsafeValue : fallback;

    public static bool TryUnwrapValue<T>(this Option<T> option, [NotNullWhen(true)] out T? value)
        where T : class
    {
        var unwrappedValue = option.Unwrap();
        if (unwrappedValue != null)
        {
            value = unwrappedValue;
            return true;
        }

        value = default;
        return false;
    }

    public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> projection)
        where T : class
        where TResult : notnull
    {
        if (option.TryUnwrapValue(out var value))
        {
            return Option.Some(projection(value));
        }

        return Option.None<TResult>();
    }
}

public static class StructOption
{
    public static T? Unwrap<T>(this Option<T> option)
        where T : struct => option.HasValue ? option.UnsafeValue as T? : default;

    public static T Unwrap<T>(this Option<T> option, T fallback)
        where T : struct => option.HasValue ? option.UnsafeValue : fallback;

    public static T? Unwrap<T>(this Option<T> option, T? fallback = default)
        where T : struct => option.HasValue ? option.UnsafeValue : fallback;

    public static bool TryUnwrapValue<T>(this Option<T> option, [NotNullWhen(true)] out T value)
        where T : struct
    {
        var unwrappedValue = option.Unwrap();
        if (unwrappedValue.HasValue)
        {
            value = unwrappedValue.Value;
            return true;
        }

        value = default;
        return false;
    }

    public static Option<TResult> Map<T, TResult>(this Option<T> option, Func<T, TResult> projection)
        where T : struct
        where TResult : notnull
    {
        if (option.TryUnwrapValue(out var value))
        {
            return Option.Some(projection(value));
        }

        return Option.None<TResult>();
    }
}
