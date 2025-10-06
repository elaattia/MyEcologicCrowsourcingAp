using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEcologicCrowsourcingApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Vehicules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CapaciteMax = table.Column<double>(type: "double precision", nullable: false),
                    VitesseMoyenne = table.Column<double>(type: "double precision", nullable: false),
                    CarburantConsommation = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    VehiculeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NbrVolontaires = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisations", x => x.OrganisationId);
                    table.ForeignKey(
                        name: "FK_Organisations_Vehicules_VehiculeId",
                        column: x => x.VehiculeId,
                        principalTable: "Vehicules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Itineraires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistanceTotale = table.Column<double>(type: "double precision", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DureeEstimee = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CarburantEstime = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itineraires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Itineraires_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "OrganisationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptimisationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListePointsIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    CapaciteVehicule = table.Column<double>(type: "double precision", nullable: false),
                    TempsMaxParTrajet = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ZoneGeographique = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimisationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimisationRequests_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "OrganisationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PointsDechets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Zone = table.Column<string>(type: "text", nullable: false),
                    Pays = table.Column<string>(type: "text", nullable: false),
                    VolumeEstime = table.Column<double>(type: "double precision", nullable: false),
                    ItineraireId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsDechets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointsDechets_Itineraires_ItineraireId",
                        column: x => x.ItineraireId,
                        principalTable: "Itineraires",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PointsDechets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptimisationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OptimisationRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItineraireOptimiseeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScoreEfficacite = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimisationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimisationResults_Itineraires_ItineraireOptimiseeId",
                        column: x => x.ItineraireOptimiseeId,
                        principalTable: "Itineraires",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OptimisationResults_OptimisationRequests_OptimisationReques~",
                        column: x => x.OptimisationRequestId,
                        principalTable: "OptimisationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Itineraires_OrganisationId",
                table: "Itineraires",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimisationRequests_OrganisationId",
                table: "OptimisationRequests",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimisationResults_ItineraireOptimiseeId",
                table: "OptimisationResults",
                column: "ItineraireOptimiseeId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimisationResults_OptimisationRequestId",
                table: "OptimisationResults",
                column: "OptimisationRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_VehiculeId",
                table: "Organisations",
                column: "VehiculeId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsDechets_ItineraireId",
                table: "PointsDechets",
                column: "ItineraireId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsDechets_UserId",
                table: "PointsDechets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OptimisationResults");

            migrationBuilder.DropTable(
                name: "PointsDechets");

            migrationBuilder.DropTable(
                name: "OptimisationRequests");

            migrationBuilder.DropTable(
                name: "Itineraires");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Organisations");

            migrationBuilder.DropTable(
                name: "Vehicules");
        }
    }
}
