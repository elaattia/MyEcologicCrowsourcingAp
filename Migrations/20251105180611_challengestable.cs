using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyEcologicCrowsourcingApp.Migrations
{
    /// <inheritdoc />
    public partial class challengestable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    PointsRequired = table.Column<int>(type: "integer", nullable: false),
                    ChallengesRequired = table.Column<int>(type: "integer", nullable: true),
                    SpecificType = table.Column<int>(type: "integer", nullable: true),
                    Criteria = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    BonusPoints = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    IsAIGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    AIPromptUsed = table.Column<string>(type: "text", nullable: true),
                    AIGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequiredProofType = table.Column<string>(type: "text", nullable: false),
                    VerificationCriteria = table.Column<string>(type: "text", nullable: true),
                    Tips = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: true),
                    CurrentParticipants = table.Column<int>(type: "integer", nullable: false),
                    MaxSubmissionsPerUser = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationMethod = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Challenges_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AIPromptTemplate = table.Column<string>(type: "text", nullable: false),
                    VerificationCriteriaTemplate = table.Column<string>(type: "text", nullable: true),
                    MinPoints = table.Column<int>(type: "integer", nullable: false),
                    MaxPoints = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false),
                    ChallengesCompleted = table.Column<int>(type: "integer", nullable: false),
                    ChallengesInProgress = table.Column<int>(type: "integer", nullable: false),
                    SubmissionsApproved = table.Column<int>(type: "integer", nullable: false),
                    SubmissionsRejected = table.Column<int>(type: "integer", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    LongestStreak = table.Column<int>(type: "integer", nullable: false),
                    LastChallengeCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecyclingChallenges = table.Column<int>(type: "integer", nullable: false),
                    LitterPickupChallenges = table.Column<int>(type: "integer", nullable: false),
                    PlantingChallenges = table.Column<int>(type: "integer", nullable: false),
                    IdentificationChallenges = table.Column<int>(type: "integer", nullable: false),
                    GlobalRank = table.Column<int>(type: "integer", nullable: false),
                    WeeklyRank = table.Column<int>(type: "integer", nullable: false),
                    MonthlyRank = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAchievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProofType = table.Column<string>(type: "text", nullable: false),
                    ProofUrl = table.Column<string>(type: "text", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<double>(type: "double precision", precision: 10, scale: 7, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    IsAIVerified = table.Column<bool>(type: "boolean", nullable: false),
                    AIConfidenceScore = table.Column<double>(type: "double precision", nullable: true),
                    AIVerificationResult = table.Column<string>(type: "text", nullable: true),
                    AIVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeSubmissions_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChallengeSubmissions_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChallengeSubmissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    SubmissionCount = table.Column<int>(type: "integer", nullable: false),
                    PointsEarned = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChallenges_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionVotes_ChallengeSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "ChallengeSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_IsActive",
                table: "Achievements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_CreatedByUserId",
                table: "Challenges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_IsActive",
                table: "Challenges",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_IsActive_Type_StartDate",
                table: "Challenges",
                columns: new[] { "IsActive", "Type", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_StartDate",
                table: "Challenges",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_Type",
                table: "Challenges",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_ChallengeId",
                table: "ChallengeSubmissions",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_ReviewedByUserId",
                table: "ChallengeSubmissions",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_Status",
                table: "ChallengeSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_SubmittedAt",
                table: "ChallengeSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_UserId_ChallengeId",
                table: "ChallengeSubmissions",
                columns: new[] { "UserId", "ChallengeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeTemplates_IsActive",
                table: "ChallengeTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeTemplates_Type",
                table: "ChallengeTemplates",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionVotes_SubmissionId_UserId",
                table: "SubmissionVotes",
                columns: new[] { "SubmissionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionVotes_UserId",
                table: "SubmissionVotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_AchievementId",
                table: "UserAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_UserId_AchievementId",
                table: "UserAchievements",
                columns: new[] { "UserId", "AchievementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserChallenges_ChallengeId",
                table: "UserChallenges",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChallenges_IsCompleted",
                table: "UserChallenges",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserChallenges_UserId_ChallengeId",
                table: "UserChallenges",
                columns: new[] { "UserId", "ChallengeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_GlobalRank",
                table: "UserStats",
                column: "GlobalRank");

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_TotalPoints",
                table: "UserStats",
                column: "TotalPoints");

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_UserId",
                table: "UserStats",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeTemplates");

            migrationBuilder.DropTable(
                name: "SubmissionVotes");

            migrationBuilder.DropTable(
                name: "UserAchievements");

            migrationBuilder.DropTable(
                name: "UserChallenges");

            migrationBuilder.DropTable(
                name: "UserStats");

            migrationBuilder.DropTable(
                name: "ChallengeSubmissions");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "Challenges");
        }
    }
}
