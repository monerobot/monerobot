namespace MoneroBot.Fider.Models;

public record Post(
    int Id,
    int Number,
    string Title,
    string Slug,
    string Description,
    DateTime CreatedAt,
    User user,
    bool HasVoted,
    int VotesCount,
    int CommentsCount,
    string Status,
    Response Response,
    string[] Tags);
