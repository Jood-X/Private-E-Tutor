using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETutor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VideoAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LessonResources_TypeMatchesSource",
                table: "LessonResources");

            migrationBuilder.AddColumn<Guid>(
                name: "VideoAssetId",
                table: "LessonResources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VideoAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerTeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    ProviderAssetId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IsEmbeddable = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoAssets", x => x.Id);
                    table.CheckConstraint("CK_VideoAssets_Status", "[Status] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_VideoAssets_TeacherProfiles_OwnerTeacherId",
                        column: x => x.OwnerTeacherId,
                        principalTable: "TeacherProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonResources_VideoAssetId",
                table: "LessonResources",
                column: "VideoAssetId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LessonResources_TypeMatchesSource",
                table: "LessonResources",
                sql: "([Type] = 1 AND [FileId] IS NOT NULL AND [ExternalUrl] IS NULL AND [VideoAssetId] IS NULL) OR ([Type] = 2 AND [ExternalUrl] IS NOT NULL AND [FileId] IS NULL AND [VideoAssetId] IS NULL) OR ([Type] = 3 AND [VideoAssetId] IS NOT NULL AND [FileId] IS NULL AND [ExternalUrl] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_VideoAssets_OwnerTeacherId_Provider_ProviderAssetId",
                table: "VideoAssets",
                columns: new[] { "OwnerTeacherId", "Provider", "ProviderAssetId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonResources_VideoAssets_VideoAssetId",
                table: "LessonResources",
                column: "VideoAssetId",
                principalTable: "VideoAssets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonResources_VideoAssets_VideoAssetId",
                table: "LessonResources");

            migrationBuilder.DropTable(
                name: "VideoAssets");

            migrationBuilder.DropIndex(
                name: "IX_LessonResources_VideoAssetId",
                table: "LessonResources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LessonResources_TypeMatchesSource",
                table: "LessonResources");

            migrationBuilder.DropColumn(
                name: "VideoAssetId",
                table: "LessonResources");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LessonResources_TypeMatchesSource",
                table: "LessonResources",
                sql: "([Type] = 1 AND [FileId] IS NOT NULL AND [ExternalUrl] IS NULL) OR ([Type] = 2 AND [ExternalUrl] IS NOT NULL AND [FileId] IS NULL)");
        }
    }
}
