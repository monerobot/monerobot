#nullable disable

namespace MoneroBot.Database.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bounties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PostNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SubAddressIndex = table.Column<uint>(type: "INTEGER", nullable: false),
                    SubAddress = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bounties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BountyContributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TransactionId = table.Column<string>(type: "TEXT", nullable: false),
                    Confirmations = table.Column<uint>(type: "INTEGER", nullable: false),
                    CommentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<uint>(type: "INTEGER", nullable: false),
                    BountyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BountyContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BountyContributions_Bounties_BountyId",
                        column: x => x.BountyId,
                        principalTable: "Bounties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "PostsHaveASingleBounty",
                table: "Bounties",
                column: "PostNumber");

            migrationBuilder.CreateIndex(
                name: "SubAddressesCannotBeReused",
                table: "Bounties",
                column: "SubAddress");

            migrationBuilder.CreateIndex(
                name: "SubAddressIndexesCannotBeReused",
                table: "Bounties",
                column: "SubAddressIndex");

            migrationBuilder.CreateIndex(
                name: "IX_BountyContributions_BountyId",
                table: "BountyContributions",
                column: "BountyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BountyContributions");

            migrationBuilder.DropTable(
                name: "Bounties");
        }
    }
}
