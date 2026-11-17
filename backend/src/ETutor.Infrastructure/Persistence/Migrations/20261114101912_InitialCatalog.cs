using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ETutor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    IsoCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DefaultTimeZoneId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                    table.CheckConstraint("CK_Countries_IsoCode_Upper", "[IsoCode] = UPPER([IsoCode]) COLLATE Latin1_General_CS_AS");
                });

            migrationBuilder.CreateTable(
                name: "Curricula",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curricula", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.Id);
                    table.UniqueConstraint("AK_AcademicYears_Id_CurriculumId", x => new { x.Id, x.CurriculumId });
                    table.CheckConstraint("CK_AcademicYears_StartBeforeEnd", "[StartDate] < [EndDate]");
                    table.ForeignKey(
                        name: "FK_AcademicYears_Curricula_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curricula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumAvailabilities",
                columns: table => new
                {
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumAvailabilities", x => new { x.CountryId, x.CurriculumId });
                    table.ForeignKey(
                        name: "FK_CurriculumAvailabilities_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumAvailabilities_Curricula_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curricula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumStreams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumStreams", x => x.Id);
                    table.UniqueConstraint("AK_CurriculumStreams_Id_CurriculumId", x => new { x.Id, x.CurriculumId });
                    table.ForeignKey(
                        name: "FK_CurriculumStreams_Curricula_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curricula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CurriculumId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Ordinal = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeLevels", x => x.Id);
                    table.UniqueConstraint("AK_GradeLevels_Id_CurriculumId", x => new { x.Id, x.CurriculumId });
                    table.ForeignKey(
                        name: "FK_GradeLevels_Curricula_CurriculumId",
                        column: x => x.CurriculumId,
                        principalTable: "Curricula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeSubjects",
                columns: table => new
                {
                    GradeLevelId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeSubjects", x => new { x.GradeLevelId, x.SubjectId });
                    table.ForeignKey(
                        name: "FK_GradeSubjects_GradeLevels_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "Id", "DefaultTimeZoneId", "IsActive", "IsoCode", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Asia/Amman", true, "JO", "{\"en\":\"Jordan\",\"ar\":\"الأردن\"}", 1 },
                    { 2, "Asia/Riyadh", true, "SA", "{\"en\":\"Saudi Arabia\",\"ar\":\"السعودية\"}", 2 },
                    { 3, "Asia/Dubai", true, "AE", "{\"en\":\"United Arab Emirates\",\"ar\":\"الإمارات العربية المتحدة\"}", 3 },
                    { 4, "Asia/Kuwait", true, "KW", "{\"en\":\"Kuwait\",\"ar\":\"الكويت\"}", 4 },
                    { 5, "Asia/Qatar", true, "QA", "{\"en\":\"Qatar\",\"ar\":\"قطر\"}", 5 },
                    { 6, "Asia/Bahrain", true, "BH", "{\"en\":\"Bahrain\",\"ar\":\"البحرين\"}", 6 },
                    { 7, "Asia/Muscat", true, "OM", "{\"en\":\"Oman\",\"ar\":\"عُمان\"}", 7 }
                });

            migrationBuilder.InsertData(
                table: "Curricula",
                columns: new[] { "Id", "Code", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "jo-tawjihi", true, "{\"en\":\"Jordanian Tawjihi\",\"ar\":\"التوجيهي الأردني\"}", 1 },
                    { 2, "sa-national", true, "{\"en\":\"Saudi National Curriculum\",\"ar\":\"المنهج الوطني السعودي\"}", 2 },
                    { 3, "ae-moe", true, "{\"en\":\"UAE Ministry of Education\",\"ar\":\"منهاج وزارة التربية والتعليم الإماراتية\"}", 3 },
                    { 4, "kw-national", true, "{\"en\":\"Kuwaiti National Curriculum\",\"ar\":\"المنهج الوطني الكويتي\"}", 4 },
                    { 5, "qa-national", true, "{\"en\":\"Qatari National Curriculum\",\"ar\":\"المنهج الوطني القطري\"}", 5 },
                    { 6, "bh-national", true, "{\"en\":\"Bahraini National Curriculum\",\"ar\":\"المنهج الوطني البحريني\"}", 6 },
                    { 7, "om-national", true, "{\"en\":\"Omani National Curriculum\",\"ar\":\"المنهج الوطني العُماني\"}", 7 },
                    { 8, "cambridge", true, "{\"en\":\"Cambridge IGCSE / A Level\",\"ar\":\"كامبريدج IGCSE / A Level\"}", 8 },
                    { 9, "ib-dp", true, "{\"en\":\"International Baccalaureate (IB DP)\",\"ar\":\"البكالوريا الدولية (IB)\"}", 9 },
                    { 10, "american", true, "{\"en\":\"American High School\",\"ar\":\"المنهج الأمريكي\"}", 10 }
                });

            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "mathematics", true, "{\"en\":\"Mathematics\",\"ar\":\"الرياضيات\"}" },
                    { 2, "physics", true, "{\"en\":\"Physics\",\"ar\":\"الفيزياء\"}" },
                    { 3, "chemistry", true, "{\"en\":\"Chemistry\",\"ar\":\"الكيمياء\"}" },
                    { 4, "biology", true, "{\"en\":\"Biology\",\"ar\":\"الأحياء\"}" },
                    { 5, "arabic", true, "{\"en\":\"Arabic Language\",\"ar\":\"اللغة العربية\"}" },
                    { 6, "english", true, "{\"en\":\"English Language\",\"ar\":\"اللغة الإنجليزية\"}" },
                    { 7, "islamic-education", true, "{\"en\":\"Islamic Education\",\"ar\":\"التربية الإسلامية\"}" },
                    { 8, "history", true, "{\"en\":\"History\",\"ar\":\"التاريخ\"}" },
                    { 9, "geography", true, "{\"en\":\"Geography\",\"ar\":\"الجغرافيا\"}" },
                    { 10, "computer-science", true, "{\"en\":\"Computer Science\",\"ar\":\"علوم الحاسوب\"}" },
                    { 11, "economics", true, "{\"en\":\"Economics\",\"ar\":\"الاقتصاد\"}" },
                    { 12, "business", true, "{\"en\":\"Business Studies\",\"ar\":\"إدارة الأعمال\"}" }
                });

            migrationBuilder.InsertData(
                table: "AcademicYears",
                columns: new[] { "Id", "CurriculumId", "EndDate", "Label", "StartDate" },
                values: new object[,]
                {
                    { 1, 1, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 8, 23) },
                    { 2, 2, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 8, 23) },
                    { 3, 3, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 8, 23) },
                    { 4, 4, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 8, 23) },
                    { 5, 5, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 8, 23) },
                    { 6, 6, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 8, 23) },
                    { 7, 7, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 8, 23) },
                    { 8, 8, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 9, 1) },
                    { 9, 9, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 9, 1) },
                    { 10, 10, new DateOnly(2027, 6, 30), "2026/2027", new DateOnly(2026, 9, 1) }
                });

            migrationBuilder.InsertData(
                table: "CurriculumAvailabilities",
                columns: new[] { "CountryId", "CurriculumId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 8 },
                    { 1, 9 },
                    { 1, 10 },
                    { 2, 2 },
                    { 2, 8 },
                    { 2, 9 },
                    { 2, 10 },
                    { 3, 3 },
                    { 3, 8 },
                    { 3, 9 },
                    { 3, 10 },
                    { 4, 4 },
                    { 4, 8 },
                    { 4, 9 },
                    { 4, 10 },
                    { 5, 5 },
                    { 5, 8 },
                    { 5, 9 },
                    { 5, 10 },
                    { 6, 6 },
                    { 6, 8 },
                    { 6, 9 },
                    { 6, 10 },
                    { 7, 7 },
                    { 7, 8 },
                    { 7, 9 },
                    { 7, 10 }
                });

            migrationBuilder.InsertData(
                table: "CurriculumStreams",
                columns: new[] { "Id", "Code", "CurriculumId", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "scientific", 1, "{\"en\":\"Scientific\",\"ar\":\"العلمي\"}", 1 },
                    { 2, "literary", 1, "{\"en\":\"Literary\",\"ar\":\"الأدبي\"}", 2 }
                });

            migrationBuilder.InsertData(
                table: "GradeLevels",
                columns: new[] { "Id", "Code", "CurriculumId", "Name", "Ordinal" },
                values: new object[,]
                {
                    { 110, "g10", 1, "{\"en\":\"Grade 10\",\"ar\":\"الصف العاشر\"}", (byte)10 },
                    { 111, "g11", 1, "{\"en\":\"Grade 11\",\"ar\":\"الصف الحادي عشر\"}", (byte)11 },
                    { 112, "g12", 1, "{\"en\":\"Grade 12\",\"ar\":\"الصف الثاني عشر\"}", (byte)12 },
                    { 210, "g10", 2, "{\"en\":\"Grade 10\",\"ar\":\"الصف العاشر\"}", (byte)10 },
                    { 211, "g11", 2, "{\"en\":\"Grade 11\",\"ar\":\"الصف الحادي عشر\"}", (byte)11 },
                    { 212, "g12", 2, "{\"en\":\"Grade 12\",\"ar\":\"الصف الثاني عشر\"}", (byte)12 },
                    { 310, "g10", 3, "{\"en\":\"Grade 10\",\"ar\":\"الصف العاشر\"}", (byte)10 },
                    { 311, "g11", 3, "{\"en\":\"Grade 11\",\"ar\":\"الصف الحادي عشر\"}", (byte)11 },
                    { 312, "g12", 3, "{\"en\":\"Grade 12\",\"ar\":\"الصف الثاني عشر\"}", (byte)12 },
                    { 410, "g10", 4, "{\"en\":\"Grade 10\",\"ar\":\"الصف العاشر\"}", (byte)10 },
                    { 411, "g11", 4, "{\"en\":\"Grade 11\",\"ar\":\"الصف الحادي عشر\"}", (byte)11 },
                    { 412, "g12", 4, "{\"en\":\"Grade 12\",\"ar\":\"الصف الثاني عشر\"}", (byte)12 },
                    { 510, "g10", 5, "{\"en\":\"Grade 10\",\"ar\":\"الصف العاشر\"}", (byte)10 },
                    { 511, "g11", 5, "{\"en\":\"Grade 11\",\"ar\":\"الصف الحادي عشر\"}", (byte)11 },
                    { 512, "g12", 5, "{\"en\":\"Grade 12\",\"ar\":\"الصف الثاني عشر\"}", (byte)12 },
                    { 610, "g10", 6, "{\"en\":\"Grade 10\",\"ar\":\"الصف العاشر\"}", (byte)10 },
                    { 611, "g11", 6, "{\"en\":\"Grade 11\",\"ar\":\"الصف الحادي عشر\"}", (byte)11 },
                    { 612, "g12", 6, "{\"en\":\"Grade 12\",\"ar\":\"الصف الثاني عشر\"}", (byte)12 },
                    { 710, "g10", 7, "{\"en\":\"Grade 10\",\"ar\":\"الصف العاشر\"}", (byte)10 },
                    { 711, "g11", 7, "{\"en\":\"Grade 11\",\"ar\":\"الصف الحادي عشر\"}", (byte)11 },
                    { 712, "g12", 7, "{\"en\":\"Grade 12\",\"ar\":\"الصف الثاني عشر\"}", (byte)12 },
                    { 810, "y10", 8, "{\"en\":\"Year 10 (IGCSE)\",\"ar\":\"السنة العاشرة (IGCSE)\"}", (byte)10 },
                    { 811, "y11", 8, "{\"en\":\"Year 11 (IGCSE)\",\"ar\":\"السنة الحادية عشرة (IGCSE)\"}", (byte)11 },
                    { 812, "y12", 8, "{\"en\":\"Year 12 (AS Level)\",\"ar\":\"السنة الثانية عشرة (AS Level)\"}", (byte)12 },
                    { 813, "y13", 8, "{\"en\":\"Year 13 (A Level)\",\"ar\":\"السنة الثالثة عشرة (A Level)\"}", (byte)13 },
                    { 911, "dp1", 9, "{\"en\":\"DP Year 1\",\"ar\":\"السنة الأولى من الدبلوما\"}", (byte)11 },
                    { 912, "dp2", 9, "{\"en\":\"DP Year 2\",\"ar\":\"السنة الثانية من الدبلوما\"}", (byte)12 },
                    { 1010, "g10", 10, "{\"en\":\"Grade 10\",\"ar\":\"الصف العاشر\"}", (byte)10 },
                    { 1011, "g11", 10, "{\"en\":\"Grade 11\",\"ar\":\"الصف الحادي عشر\"}", (byte)11 },
                    { 1012, "g12", 10, "{\"en\":\"Grade 12\",\"ar\":\"الصف الثاني عشر\"}", (byte)12 }
                });

            migrationBuilder.InsertData(
                table: "GradeSubjects",
                columns: new[] { "GradeLevelId", "SubjectId" },
                values: new object[,]
                {
                    { 110, 1 },
                    { 110, 2 },
                    { 110, 3 },
                    { 110, 4 },
                    { 110, 5 },
                    { 110, 6 },
                    { 110, 7 },
                    { 110, 8 },
                    { 110, 9 },
                    { 110, 10 },
                    { 110, 11 },
                    { 110, 12 },
                    { 111, 1 },
                    { 111, 2 },
                    { 111, 3 },
                    { 111, 4 },
                    { 111, 5 },
                    { 111, 6 },
                    { 111, 7 },
                    { 111, 8 },
                    { 111, 9 },
                    { 111, 10 },
                    { 111, 11 },
                    { 111, 12 },
                    { 112, 1 },
                    { 112, 2 },
                    { 112, 3 },
                    { 112, 4 },
                    { 112, 5 },
                    { 112, 6 },
                    { 112, 7 },
                    { 112, 8 },
                    { 112, 9 },
                    { 112, 10 },
                    { 112, 11 },
                    { 112, 12 },
                    { 210, 1 },
                    { 210, 2 },
                    { 210, 3 },
                    { 210, 4 },
                    { 210, 5 },
                    { 210, 6 },
                    { 210, 7 },
                    { 210, 8 },
                    { 210, 9 },
                    { 210, 10 },
                    { 210, 11 },
                    { 210, 12 },
                    { 211, 1 },
                    { 211, 2 },
                    { 211, 3 },
                    { 211, 4 },
                    { 211, 5 },
                    { 211, 6 },
                    { 211, 7 },
                    { 211, 8 },
                    { 211, 9 },
                    { 211, 10 },
                    { 211, 11 },
                    { 211, 12 },
                    { 212, 1 },
                    { 212, 2 },
                    { 212, 3 },
                    { 212, 4 },
                    { 212, 5 },
                    { 212, 6 },
                    { 212, 7 },
                    { 212, 8 },
                    { 212, 9 },
                    { 212, 10 },
                    { 212, 11 },
                    { 212, 12 },
                    { 310, 1 },
                    { 310, 2 },
                    { 310, 3 },
                    { 310, 4 },
                    { 310, 5 },
                    { 310, 6 },
                    { 310, 7 },
                    { 310, 8 },
                    { 310, 9 },
                    { 310, 10 },
                    { 310, 11 },
                    { 310, 12 },
                    { 311, 1 },
                    { 311, 2 },
                    { 311, 3 },
                    { 311, 4 },
                    { 311, 5 },
                    { 311, 6 },
                    { 311, 7 },
                    { 311, 8 },
                    { 311, 9 },
                    { 311, 10 },
                    { 311, 11 },
                    { 311, 12 },
                    { 312, 1 },
                    { 312, 2 },
                    { 312, 3 },
                    { 312, 4 },
                    { 312, 5 },
                    { 312, 6 },
                    { 312, 7 },
                    { 312, 8 },
                    { 312, 9 },
                    { 312, 10 },
                    { 312, 11 },
                    { 312, 12 },
                    { 410, 1 },
                    { 410, 2 },
                    { 410, 3 },
                    { 410, 4 },
                    { 410, 5 },
                    { 410, 6 },
                    { 410, 7 },
                    { 410, 8 },
                    { 410, 9 },
                    { 410, 10 },
                    { 410, 11 },
                    { 410, 12 },
                    { 411, 1 },
                    { 411, 2 },
                    { 411, 3 },
                    { 411, 4 },
                    { 411, 5 },
                    { 411, 6 },
                    { 411, 7 },
                    { 411, 8 },
                    { 411, 9 },
                    { 411, 10 },
                    { 411, 11 },
                    { 411, 12 },
                    { 412, 1 },
                    { 412, 2 },
                    { 412, 3 },
                    { 412, 4 },
                    { 412, 5 },
                    { 412, 6 },
                    { 412, 7 },
                    { 412, 8 },
                    { 412, 9 },
                    { 412, 10 },
                    { 412, 11 },
                    { 412, 12 },
                    { 510, 1 },
                    { 510, 2 },
                    { 510, 3 },
                    { 510, 4 },
                    { 510, 5 },
                    { 510, 6 },
                    { 510, 7 },
                    { 510, 8 },
                    { 510, 9 },
                    { 510, 10 },
                    { 510, 11 },
                    { 510, 12 },
                    { 511, 1 },
                    { 511, 2 },
                    { 511, 3 },
                    { 511, 4 },
                    { 511, 5 },
                    { 511, 6 },
                    { 511, 7 },
                    { 511, 8 },
                    { 511, 9 },
                    { 511, 10 },
                    { 511, 11 },
                    { 511, 12 },
                    { 512, 1 },
                    { 512, 2 },
                    { 512, 3 },
                    { 512, 4 },
                    { 512, 5 },
                    { 512, 6 },
                    { 512, 7 },
                    { 512, 8 },
                    { 512, 9 },
                    { 512, 10 },
                    { 512, 11 },
                    { 512, 12 },
                    { 610, 1 },
                    { 610, 2 },
                    { 610, 3 },
                    { 610, 4 },
                    { 610, 5 },
                    { 610, 6 },
                    { 610, 7 },
                    { 610, 8 },
                    { 610, 9 },
                    { 610, 10 },
                    { 610, 11 },
                    { 610, 12 },
                    { 611, 1 },
                    { 611, 2 },
                    { 611, 3 },
                    { 611, 4 },
                    { 611, 5 },
                    { 611, 6 },
                    { 611, 7 },
                    { 611, 8 },
                    { 611, 9 },
                    { 611, 10 },
                    { 611, 11 },
                    { 611, 12 },
                    { 612, 1 },
                    { 612, 2 },
                    { 612, 3 },
                    { 612, 4 },
                    { 612, 5 },
                    { 612, 6 },
                    { 612, 7 },
                    { 612, 8 },
                    { 612, 9 },
                    { 612, 10 },
                    { 612, 11 },
                    { 612, 12 },
                    { 710, 1 },
                    { 710, 2 },
                    { 710, 3 },
                    { 710, 4 },
                    { 710, 5 },
                    { 710, 6 },
                    { 710, 7 },
                    { 710, 8 },
                    { 710, 9 },
                    { 710, 10 },
                    { 710, 11 },
                    { 710, 12 },
                    { 711, 1 },
                    { 711, 2 },
                    { 711, 3 },
                    { 711, 4 },
                    { 711, 5 },
                    { 711, 6 },
                    { 711, 7 },
                    { 711, 8 },
                    { 711, 9 },
                    { 711, 10 },
                    { 711, 11 },
                    { 711, 12 },
                    { 712, 1 },
                    { 712, 2 },
                    { 712, 3 },
                    { 712, 4 },
                    { 712, 5 },
                    { 712, 6 },
                    { 712, 7 },
                    { 712, 8 },
                    { 712, 9 },
                    { 712, 10 },
                    { 712, 11 },
                    { 712, 12 },
                    { 810, 1 },
                    { 810, 2 },
                    { 810, 3 },
                    { 810, 4 },
                    { 810, 5 },
                    { 810, 6 },
                    { 810, 8 },
                    { 810, 9 },
                    { 810, 10 },
                    { 810, 11 },
                    { 810, 12 },
                    { 811, 1 },
                    { 811, 2 },
                    { 811, 3 },
                    { 811, 4 },
                    { 811, 5 },
                    { 811, 6 },
                    { 811, 8 },
                    { 811, 9 },
                    { 811, 10 },
                    { 811, 11 },
                    { 811, 12 },
                    { 812, 1 },
                    { 812, 2 },
                    { 812, 3 },
                    { 812, 4 },
                    { 812, 5 },
                    { 812, 6 },
                    { 812, 8 },
                    { 812, 9 },
                    { 812, 10 },
                    { 812, 11 },
                    { 812, 12 },
                    { 813, 1 },
                    { 813, 2 },
                    { 813, 3 },
                    { 813, 4 },
                    { 813, 5 },
                    { 813, 6 },
                    { 813, 8 },
                    { 813, 9 },
                    { 813, 10 },
                    { 813, 11 },
                    { 813, 12 },
                    { 911, 1 },
                    { 911, 2 },
                    { 911, 3 },
                    { 911, 4 },
                    { 911, 5 },
                    { 911, 6 },
                    { 911, 8 },
                    { 911, 9 },
                    { 911, 10 },
                    { 911, 11 },
                    { 911, 12 },
                    { 912, 1 },
                    { 912, 2 },
                    { 912, 3 },
                    { 912, 4 },
                    { 912, 5 },
                    { 912, 6 },
                    { 912, 8 },
                    { 912, 9 },
                    { 912, 10 },
                    { 912, 11 },
                    { 912, 12 },
                    { 1010, 1 },
                    { 1010, 2 },
                    { 1010, 3 },
                    { 1010, 4 },
                    { 1010, 5 },
                    { 1010, 6 },
                    { 1010, 8 },
                    { 1010, 9 },
                    { 1010, 10 },
                    { 1010, 11 },
                    { 1010, 12 },
                    { 1011, 1 },
                    { 1011, 2 },
                    { 1011, 3 },
                    { 1011, 4 },
                    { 1011, 5 },
                    { 1011, 6 },
                    { 1011, 8 },
                    { 1011, 9 },
                    { 1011, 10 },
                    { 1011, 11 },
                    { 1011, 12 },
                    { 1012, 1 },
                    { 1012, 2 },
                    { 1012, 3 },
                    { 1012, 4 },
                    { 1012, 5 },
                    { 1012, 6 },
                    { 1012, 8 },
                    { 1012, 9 },
                    { 1012, 10 },
                    { 1012, 11 },
                    { 1012, 12 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_CurriculumId_Label",
                table: "AcademicYears",
                columns: new[] { "CurriculumId", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_IsoCode",
                table: "Countries",
                column: "IsoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Curricula_Code",
                table: "Curricula",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumAvailabilities_CurriculumId",
                table: "CurriculumAvailabilities",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumStreams_CurriculumId_Code",
                table: "CurriculumStreams",
                columns: new[] { "CurriculumId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeLevels_CurriculumId_Code",
                table: "GradeLevels",
                columns: new[] { "CurriculumId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeLevels_CurriculumId_Ordinal",
                table: "GradeLevels",
                columns: new[] { "CurriculumId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeSubjects_SubjectId",
                table: "GradeSubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropTable(
                name: "CurriculumAvailabilities");

            migrationBuilder.DropTable(
                name: "CurriculumStreams");

            migrationBuilder.DropTable(
                name: "GradeSubjects");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "GradeLevels");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Curricula");
        }
    }
}
