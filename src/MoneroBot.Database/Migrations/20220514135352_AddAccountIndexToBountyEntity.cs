#nullable disable

namespace MoneroBot.Database.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class AddAccountIndexToBountyEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "AccountIndex",
                table: "Bounties",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountIndex",
                table: "Bounties");
        }
    }
}
