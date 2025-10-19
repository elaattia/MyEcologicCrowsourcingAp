using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEcologicCrowsourcingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommandation2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RecommandationsEcologiques_DateGeneration",
                table: "RecommandationsEcologiques",
                column: "DateGeneration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecommandationsEcologiques_DateGeneration",
                table: "RecommandationsEcologiques");
        }
    }
}
