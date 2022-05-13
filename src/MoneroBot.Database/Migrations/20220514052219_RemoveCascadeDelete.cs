#nullable disable

namespace MoneroBot.Database.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class RemoveCascadeDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BountyContributions_Bounties_BountyId",
                table: "BountyContributions");

            migrationBuilder.AddForeignKey(
                name: "FK_BountyContributions_Bounties_BountyId",
                table: "BountyContributions",
                column: "BountyId",
                principalTable: "Bounties",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BountyContributions_Bounties_BountyId",
                table: "BountyContributions");

            migrationBuilder.AddForeignKey(
                name: "FK_BountyContributions_Bounties_BountyId",
                table: "BountyContributions",
                column: "BountyId",
                principalTable: "Bounties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
