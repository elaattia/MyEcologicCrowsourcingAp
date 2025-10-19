using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEcologicCrowsourcingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommandation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecommandationsEcologiques",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PointDechetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScorePriorite = table.Column<int>(type: "integer", nullable: false),
                    Urgence = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionRecommandee = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DateGeneration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContexteUtilise = table.Column<string>(type: "text", nullable: true),
                    EstActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommandationsEcologiques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommandationsEcologiques_PointDechets_PointDechetId",
                        column: x => x.PointDechetId,
                        principalTable: "PointDechets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommandationsEcologiques_PointDechetId",
                table: "RecommandationsEcologiques",
                column: "PointDechetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommandationsEcologiques");
        }
    }
}
