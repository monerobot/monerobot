namespace MoneroBot.Database.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Index(nameof(CommentId), IsUnique = true)]
public class Comment
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Comment() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Comment(int commentId, string content)
    {
        this.CommentId = commentId;
        this.Content = content;
    }

    [Key]
    public int Id { get; set; }

    public int CommentId { get; set; }

    public string Content { get; set; }
}
