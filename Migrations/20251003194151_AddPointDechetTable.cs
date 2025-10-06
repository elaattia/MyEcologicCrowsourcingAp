using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEcologicCrowsourcingApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPointDechetTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organisations_Vehicules_VehiculeId",
                table: "Organisations");

            migrationBuilder.DropForeignKey(
                name: "FK_PointsDechets_Itineraires_ItineraireId",
                table: "PointsDechets");

            migrationBuilder.DropForeignKey(
                name: "FK_PointsDechets_Users_UserId",
                table: "PointsDechets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PointsDechets",
                table: "PointsDechets");

            migrationBuilder.RenameTable(
                name: "PointsDechets",
                newName: "PointDechets");

            migrationBuilder.RenameIndex(
                name: "IX_PointsDechets_UserId",
                table: "PointDechets",
                newName: "IX_PointDechets_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PointsDechets_ItineraireId",
                table: "PointDechets",
                newName: "IX_PointDechets_ItineraireId");

            migrationBuilder.AlterColumn<Guid>(
                name: "VehiculeId",
                table: "Organisations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<double>(
                name: "VolumeEstime",
                table: "PointDechets",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "PointDechets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PointDechets",
                table: "PointDechets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Organisations_Vehicules_VehiculeId",
                table: "Organisations",
                column: "VehiculeId",
                principalTable: "Vehicules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PointDechets_Itineraires_ItineraireId",
                table: "PointDechets",
                column: "ItineraireId",
                principalTable: "Itineraires",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PointDechets_Users_UserId",
                table: "PointDechets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organisations_Vehicules_VehiculeId",
                table: "Organisations");

            migrationBuilder.DropForeignKey(
                name: "FK_PointDechets_Itineraires_ItineraireId",
                table: "PointDechets");

            migrationBuilder.DropForeignKey(
                name: "FK_PointDechets_Users_UserId",
                table: "PointDechets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PointDechets",
                table: "PointDechets");

            migrationBuilder.RenameTable(
                name: "PointDechets",
                newName: "PointsDechets");

            migrationBuilder.RenameIndex(
                name: "IX_PointDechets_UserId",
                table: "PointsDechets",
                newName: "IX_PointsDechets_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PointDechets_ItineraireId",
                table: "PointsDechets",
                newName: "IX_PointsDechets_ItineraireId");

            migrationBuilder.AlterColumn<Guid>(
                name: "VehiculeId",
                table: "Organisations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "VolumeEstime",
                table: "PointsDechets",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "PointsDechets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PointsDechets",
                table: "PointsDechets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Organisations_Vehicules_VehiculeId",
                table: "Organisations",
                column: "VehiculeId",
                principalTable: "Vehicules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PointsDechets_Itineraires_ItineraireId",
                table: "PointsDechets",
                column: "ItineraireId",
                principalTable: "Itineraires",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PointsDechets_Users_UserId",
                table: "PointsDechets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
