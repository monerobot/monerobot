namespace MoneroBot.Fider.Models;

public record Error(string?Field, string Message);

public record ErrorSet(Error[] Errors);
