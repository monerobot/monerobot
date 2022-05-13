using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneroBot.Database.Migrations
{
    public partial class AddSeperateTransactionTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "BountyContributions");

            migrationBuilder.DropColumn(
                name: "Confirmations",
                table: "BountyContributions");

            migrationBuilder.CreateTable(
                name: "XmrTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<string>(type: "TEXT", nullable: false),
                    BlockHeight = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AccountIndex = table.Column<uint>(type: "INTEGER", nullable: false),
                    SubAddress = table.Column<string>(type: "TEXT", nullable: false),
                    SubAddressIndex = table.Column<uint>(type: "INTEGER", nullable: false),
                    Amount = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsSpent = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnlocked = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XmrTransactions", x => x.TransactionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BountyContributions_TransactionId",
                table: "BountyContributions",
                column: "TransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BountyContributions_XmrTransactions_TransactionId",
                table: "BountyContributions",
                column: "TransactionId",
                principalTable: "XmrTransactions",
                principalColumn: "TransactionId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BountyContributions_XmrTransactions_TransactionId",
                table: "BountyContributions");

            migrationBuilder.DropTable(
                name: "XmrTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BountyContributions_TransactionId",
                table: "BountyContributions");

            migrationBuilder.AddColumn<uint>(
                name: "Amount",
                table: "BountyContributions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "Confirmations",
                table: "BountyContributions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }
    }
}
