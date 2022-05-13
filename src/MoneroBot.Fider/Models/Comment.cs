namespace MoneroBot.Fider.Models;

public record Comment(int Id, string Content, DateTime CreatedAt, User User);
