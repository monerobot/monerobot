#nullable disable

namespace MoneroBot.Database.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class AddCommentIdToBountyEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommentId",
                table: "Bounties",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "Bounties");
        }
    }
}
