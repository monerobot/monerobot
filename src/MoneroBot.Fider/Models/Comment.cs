namespace MoneroBot.Fider.Models;

public record Comment(int Id, string Content, string[]? Attachments, DateTime CreatedAt, User User);
