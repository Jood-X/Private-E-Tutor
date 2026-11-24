using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETutor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CoursesAndEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: false),
                    GradeLevelId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    AcademicYearId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ContentLanguage = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.CheckConstraint("CK_Courses_ContentLanguage", "[ContentLanguage] IN ('en', 'ar')");
                    table.CheckConstraint("CK_Courses_Status", "[Status] IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_Courses_AcademicYears_AcademicYearId_CurriculumId",
                        columns: x => new { x.AcademicYearId, x.CurriculumId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "Id", "CurriculumId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_Curricula_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curricula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_GradeLevels_GradeLevelId_CurriculumId",
                        columns: x => new { x.GradeLevelId, x.CurriculumId },
                        principalTable: "GradeLevels",
                        principalColumns: new[] { "Id", "CurriculumId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_GradeSubjects_GradeLevelId_SubjectId",
                        columns: x => new { x.GradeLevelId, x.SubjectId },
                        principalTable: "GradeSubjects",
                        principalColumns: new[] { "GradeLevelId", "SubjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_TeacherProfiles_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "TeacherProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    StorageProvider = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(400)", unicode: false, maxLength: 400, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<byte[]>(type: "binary(32)", fixedLength: true, maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoredFiles_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RequestMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.UniqueConstraint("AK_Enrollments_Id_StudentId", x => new { x.Id, x.StudentId });
                    table.CheckConstraint("CK_Enrollments_Status", "[Status] IN (0, 1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_Enrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_StudentProfiles_StudentId",
                        column: x => x.StudentId,
                        principalTable: "StudentProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.UniqueConstraint("AK_Units_Id_CourseId", x => new { x.Id, x.CourseId });
                    table.CheckConstraint("CK_Units_Status", "[Status] IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_Units_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedMinutes = table.Column<short>(type: "smallint", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.CheckConstraint("CK_Lessons_Status", "[Status] IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_Lessons_Units_UnitId_CourseId",
                        columns: x => new { x.UnitId, x.CourseId },
                        principalTable: "Units",
                        principalColumns: new[] { "Id", "CourseId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonResources", x => x.Id);
                    table.CheckConstraint("CK_LessonResources_TypeMatchesSource", "([Type] = 1 AND [FileId] IS NOT NULL AND [ExternalUrl] IS NULL) OR ([Type] = 2 AND [ExternalUrl] IS NOT NULL AND [FileId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_LessonResources_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonResources_StoredFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "StoredFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_AcademicYearId_CurriculumId",
                table: "Courses",
                columns: new[] { "AcademicYearId", "CurriculumId" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CurriculumId",
                table: "Courses",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_GradeLevelId_CurriculumId",
                table: "Courses",
                columns: new[] { "GradeLevelId", "CurriculumId" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_GradeLevelId_SubjectId",
                table: "Courses",
                columns: new[] { "GradeLevelId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Status_GradeLevelId_SubjectId",
                table: "Courses",
                columns: new[] { "Status", "GradeLevelId", "SubjectId" })
                .Annotation("SqlServer:Include", new[] { "TeacherId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SubjectId",
                table: "Courses",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TeacherId_Status",
                table: "Courses",
                columns: new[] { "TeacherId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId_Status_LastActivityAtUtc",
                table: "Enrollments",
                columns: new[] { "CourseId", "Status", "LastActivityAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId_Status_RequestedAtUtc",
                table: "Enrollments",
                columns: new[] { "CourseId", "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_Status",
                table: "Enrollments",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_Enrollments_Active",
                table: "Enrollments",
                columns: new[] { "CourseId", "StudentId" },
                unique: true,
                filter: "[Status] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_LessonResources_FileId",
                table: "LessonResources",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonResources_LessonId_SortOrder",
                table: "LessonResources",
                columns: new[] { "LessonId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CourseId",
                table: "Lessons",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UnitId_CourseId",
                table: "Lessons",
                columns: new[] { "UnitId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UnitId_SortOrder",
                table: "Lessons",
                columns: new[] { "UnitId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_OwnerUserId",
                table: "StoredFiles",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_StorageKey",
                table: "StoredFiles",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_CourseId_SortOrder",
                table: "Units",
                columns: new[] { "CourseId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "LessonResources");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "StoredFiles");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Courses");
        }
    }
}
