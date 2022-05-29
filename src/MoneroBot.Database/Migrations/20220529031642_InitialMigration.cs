using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneroBot.Database.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bounties",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    post_number = table.Column<uint>(type: "INTEGER", nullable: false),
                    slug = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bounties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    comment_id = table.Column<int>(type: "INTEGER", nullable: false),
                    content = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "enotes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    pub_key = table.Column<string>(type: "TEXT", nullable: false),
                    address = table.Column<string>(type: "TEXT", nullable: false),
                    tx_hash = table.Column<string>(type: "TEXT", nullable: false),
                    block_height = table.Column<ulong>(type: "INTEGER", nullable: false),
                    amount = table.Column<ulong>(type: "INTEGER", nullable: false),
                    is_spent = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_unlocked = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enotes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "donation_addresses",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    bounty_id = table.Column<int>(type: "INTEGER", nullable: false),
                    comment_id = table.Column<int>(type: "INTEGER", nullable: true),
                    address = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_donation_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_donation_addresses_bounties_bounty_id",
                        column: x => x.bounty_id,
                        principalTable: "bounties",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_donation_addresses_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "comments",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "donations",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    donation_address_id = table.Column<int>(type: "INTEGER", nullable: false),
                    comment_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_donations", x => x.id);
                    table.ForeignKey(
                        name: "fk_donations_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "comments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_donations_donation_addresses_donation_address_id",
                        column: x => x.donation_address_id,
                        principalTable: "donation_addresses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "donation_enotes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    donation_id = table.Column<int>(type: "INTEGER", nullable: false),
                    enote_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_donation_enotes", x => x.id);
                    table.ForeignKey(
                        name: "fk_donation_enotes_donations_donation_id",
                        column: x => x.donation_id,
                        principalTable: "donations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_donation_enotes_enotes_enote_id",
                        column: x => x.enote_id,
                        principalTable: "enotes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_bounties_post_number",
                table: "bounties",
                column: "post_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_comments_comment_id",
                table: "comments",
                column: "comment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_donation_addresses_address",
                table: "donation_addresses",
                column: "address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_donation_addresses_bounty_id",
                table: "donation_addresses",
                column: "bounty_id");

            migrationBuilder.CreateIndex(
                name: "ix_donation_addresses_comment_id",
                table: "donation_addresses",
                column: "comment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_donation_enotes_donation_id",
                table: "donation_enotes",
                column: "donation_id");

            migrationBuilder.CreateIndex(
                name: "ix_donation_enotes_enote_id",
                table: "donation_enotes",
                column: "enote_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_donations_comment_id",
                table: "donations",
                column: "comment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_donations_donation_address_id",
                table: "donations",
                column: "donation_address_id");

            migrationBuilder.CreateIndex(
                name: "ix_enotes_address",
                table: "enotes",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "ix_enotes_pub_key",
                table: "enotes",
                column: "pub_key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "donation_enotes");

            migrationBuilder.DropTable(
                name: "donations");

            migrationBuilder.DropTable(
                name: "enotes");

            migrationBuilder.DropTable(
                name: "donation_addresses");

            migrationBuilder.DropTable(
                name: "bounties");

            migrationBuilder.DropTable(
                name: "comments");
        }
    }
}
