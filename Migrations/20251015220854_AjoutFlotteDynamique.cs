using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEcologicCrowsourcingApp.Migrations
{
    /// <inheritdoc />
    public partial class AjoutFlotteDynamique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Itineraires_Organisations_OrganisationId",
                table: "Itineraires");

            migrationBuilder.DropForeignKey(
                name: "FK_Organisations_Vehicules_VehiculeId",
                table: "Organisations");

            migrationBuilder.DropForeignKey(
                name: "FK_PointDechets_Users_UserId",
                table: "PointDechets");

            migrationBuilder.DropIndex(
                name: "IX_Organisations_VehiculeId",
                table: "Organisations");

            migrationBuilder.DropIndex(
                name: "IX_Itineraires_OrganisationId",
                table: "Itineraires");

            migrationBuilder.AddColumn<DateTime>(
                name: "DerniereUtilisation",
                table: "Vehicules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstDisponible",
                table: "Vehicules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Immatriculation",
                table: "Vehicules",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganisationId",
                table: "Vehicules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "PointDechets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Pays",
                table: "PointDechets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateNettoyage",
                table: "PointDechets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NettoyeParOrganisationId",
                table: "PointDechets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nom",
                table: "Organisations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreation",
                table: "Itineraires",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateDebut",
                table: "Itineraires",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFin",
                table: "Itineraires",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Statut",
                table: "Itineraires",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Depots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: false),
                    Longitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: false),
                    Adresse = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstActif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Depots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Depots_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "OrganisationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_OrganisationId_EstDisponible",
                table: "Vehicules",
                columns: new[] { "OrganisationId", "EstDisponible" });

            migrationBuilder.CreateIndex(
                name: "IX_PointDechets_NettoyeParOrganisationId",
                table: "PointDechets",
                column: "NettoyeParOrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_PointDechets_Statut",
                table: "PointDechets",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_PointDechets_Statut_Zone",
                table: "PointDechets",
                columns: new[] { "Statut", "Zone" });

            migrationBuilder.CreateIndex(
                name: "IX_PointDechets_Zone",
                table: "PointDechets",
                column: "Zone");

            migrationBuilder.CreateIndex(
                name: "IX_Itineraires_DateCreation",
                table: "Itineraires",
                column: "DateCreation");

            migrationBuilder.CreateIndex(
                name: "IX_Itineraires_OrganisationId_Statut",
                table: "Itineraires",
                columns: new[] { "OrganisationId", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_Depots_OrganisationId_EstActif",
                table: "Depots",
                columns: new[] { "OrganisationId", "EstActif" });

            migrationBuilder.AddForeignKey(
                name: "FK_Itineraires_Organisations_OrganisationId",
                table: "Itineraires",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "OrganisationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PointDechets_Organisations_NettoyeParOrganisationId",
                table: "PointDechets",
                column: "NettoyeParOrganisationId",
                principalTable: "Organisations",
                principalColumn: "OrganisationId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PointDechets_Users_UserId",
                table: "PointDechets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicules_Organisations_OrganisationId",
                table: "Vehicules",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "OrganisationId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Itineraires_Organisations_OrganisationId",
                table: "Itineraires");

            migrationBuilder.DropForeignKey(
                name: "FK_PointDechets_Organisations_NettoyeParOrganisationId",
                table: "PointDechets");

            migrationBuilder.DropForeignKey(
                name: "FK_PointDechets_Users_UserId",
                table: "PointDechets");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicules_Organisations_OrganisationId",
                table: "Vehicules");

            migrationBuilder.DropTable(
                name: "Depots");

            migrationBuilder.DropIndex(
                name: "IX_Vehicules_OrganisationId_EstDisponible",
                table: "Vehicules");

            migrationBuilder.DropIndex(
                name: "IX_PointDechets_NettoyeParOrganisationId",
                table: "PointDechets");

            migrationBuilder.DropIndex(
                name: "IX_PointDechets_Statut",
                table: "PointDechets");

            migrationBuilder.DropIndex(
                name: "IX_PointDechets_Statut_Zone",
                table: "PointDechets");

            migrationBuilder.DropIndex(
                name: "IX_PointDechets_Zone",
                table: "PointDechets");

            migrationBuilder.DropIndex(
                name: "IX_Itineraires_DateCreation",
                table: "Itineraires");

            migrationBuilder.DropIndex(
                name: "IX_Itineraires_OrganisationId_Statut",
                table: "Itineraires");

            migrationBuilder.DropColumn(
                name: "DerniereUtilisation",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "EstDisponible",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "Immatriculation",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "OrganisationId",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "DateNettoyage",
                table: "PointDechets");

            migrationBuilder.DropColumn(
                name: "NettoyeParOrganisationId",
                table: "PointDechets");

            migrationBuilder.DropColumn(
                name: "DateCreation",
                table: "Itineraires");

            migrationBuilder.DropColumn(
                name: "DateDebut",
                table: "Itineraires");

            migrationBuilder.DropColumn(
                name: "DateFin",
                table: "Itineraires");

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "Itineraires");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Zone",
                table: "PointDechets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Pays",
                table: "PointDechets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Nom",
                table: "Organisations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_VehiculeId",
                table: "Organisations",
                column: "VehiculeId");

            migrationBuilder.CreateIndex(
                name: "IX_Itineraires_OrganisationId",
                table: "Itineraires",
                column: "OrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Itineraires_Organisations_OrganisationId",
                table: "Itineraires",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "OrganisationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Organisations_Vehicules_VehiculeId",
                table: "Organisations",
                column: "VehiculeId",
                principalTable: "Vehicules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PointDechets_Users_UserId",
                table: "PointDechets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
