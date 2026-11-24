using ETutor.Domain.Catalog;
using ETutor.Domain.Content;
using ETutor.Domain.Enrollments;
using ETutor.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETutor.Infrastructure.Persistence.Configurations;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses", table =>
        {
            table.HasCheckConstraint("CK_Courses_Status", "[Status] IN (0, 1, 2)");
            table.HasCheckConstraint("CK_Courses_ContentLanguage", "[ContentLanguage] IN ('en', 'ar')");
        });
        builder.HasKey(course => course.Id);
        builder.Property(course => course.Title).HasMaxLength(ColumnLengths.Title);
        builder.Property(course => course.Description).HasMaxLength(ColumnLengths.LongDescription);
        builder.Property(course => course.ContentLanguage).HasMaxLength(ColumnLengths.LanguageCode).IsUnicode(false);
        builder.Property(course => course.Status).HasConversion<byte>();
        builder.Property(course => course.PublishedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(course => course.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(course => course.UpdatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(course => course.RowVersion).IsRowVersion();

        builder.HasOne(course => course.Teacher).WithMany().HasForeignKey(course => course.TeacherId);
        builder.HasOne(course => course.Curriculum).WithMany().HasForeignKey(course => course.CurriculumId);
        builder.HasOne(course => course.Subject).WithMany().HasForeignKey(course => course.SubjectId);

        // Composite FKs: grade and academic year belong to the course's curriculum; the subject is taught at that grade.
        builder.HasOne(course => course.GradeLevel).WithMany()
            .HasForeignKey(course => new { course.GradeLevelId, course.CurriculumId })
            .HasPrincipalKey(grade => new { grade.Id, grade.CurriculumId });
        builder.HasOne(course => course.AcademicYear).WithMany()
            .HasForeignKey(course => new { course.AcademicYearId, course.CurriculumId })
            .HasPrincipalKey(year => new { year.Id, year.CurriculumId });
        builder.HasOne<GradeSubject>().WithMany()
            .HasForeignKey(course => new { course.GradeLevelId, course.SubjectId });

        builder.HasIndex(course => new { course.TeacherId, course.Status });
        builder.HasIndex(course => new { course.Status, course.GradeLevelId, course.SubjectId })
            .IncludeProperties(course => new { course.TeacherId, course.Title });
    }
}

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units", table => table.HasCheckConstraint("CK_Units_Status", "[Status] IN (0, 1, 2)"));
        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Title).HasMaxLength(ColumnLengths.Title);
        builder.Property(unit => unit.Description).HasMaxLength(ColumnLengths.ShortDescription);
        builder.Property(unit => unit.Status).HasConversion<byte>();
        builder.Property(unit => unit.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(unit => unit.UpdatedAtUtc).HasColumnType("datetime2(0)");

        // Alternate key so lessons can prove they belong to the same course as their unit.
        builder.HasAlternateKey(unit => new { unit.Id, unit.CourseId });
        builder.HasOne(unit => unit.Course).WithMany(course => course.Units).HasForeignKey(unit => unit.CourseId)
            .OnDelete(DeleteBehavior.Cascade).HasAnnotation(AppDbContext.CascadeAnnotation, true);

        // Not unique on purpose: reordering swaps values and SQL Server cannot defer uniqueness.
        builder.HasIndex(unit => new { unit.CourseId, unit.SortOrder });
    }
}

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons", table => table.HasCheckConstraint("CK_Lessons_Status", "[Status] IN (0, 1, 2)"));
        builder.HasKey(lesson => lesson.Id);
        builder.Property(lesson => lesson.Title).HasMaxLength(ColumnLengths.Title);
        builder.Property(lesson => lesson.Summary).HasMaxLength(ColumnLengths.ShortDescription);
        builder.Property(lesson => lesson.Notes);
        builder.Property(lesson => lesson.Status).HasConversion<byte>();
        builder.Property(lesson => lesson.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(lesson => lesson.UpdatedAtUtc).HasColumnType("datetime2(0)");

        builder.HasOne(lesson => lesson.Unit).WithMany(unit => unit.Lessons)
            .HasForeignKey(lesson => new { lesson.UnitId, lesson.CourseId })
            .HasPrincipalKey(unit => new { unit.Id, unit.CourseId })
            .OnDelete(DeleteBehavior.Cascade).HasAnnotation(AppDbContext.CascadeAnnotation, true);

        builder.HasIndex(lesson => new { lesson.UnitId, lesson.SortOrder });
        builder.HasIndex(lesson => lesson.CourseId);
    }
}

internal sealed class LessonResourceConfiguration : IEntityTypeConfiguration<LessonResource>
{
    public void Configure(EntityTypeBuilder<LessonResource> builder)
    {
        builder.ToTable("LessonResources", table => table.HasCheckConstraint(
            "CK_LessonResources_TypeMatchesSource",
            "([Type] = 1 AND [FileId] IS NOT NULL AND [ExternalUrl] IS NULL) OR " +
            "([Type] = 2 AND [ExternalUrl] IS NOT NULL AND [FileId] IS NULL)"));
        builder.HasKey(resource => resource.Id);
        builder.Property(resource => resource.Type).HasConversion<byte>();
        builder.Property(resource => resource.Title).HasMaxLength(ColumnLengths.Title);
        builder.Property(resource => resource.ExternalUrl).HasMaxLength(ColumnLengths.Url);
        builder.Property(resource => resource.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(resource => resource.UpdatedAtUtc).HasColumnType("datetime2(0)");

        builder.HasOne(resource => resource.Lesson).WithMany(lesson => lesson.Resources)
            .HasForeignKey(resource => resource.LessonId)
            .OnDelete(DeleteBehavior.Cascade).HasAnnotation(AppDbContext.CascadeAnnotation, true);
        builder.HasOne(resource => resource.File).WithMany().HasForeignKey(resource => resource.FileId);

        builder.HasIndex(resource => new { resource.LessonId, resource.SortOrder });
    }
}

internal sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");
        builder.HasKey(file => file.Id);
        builder.Property(file => file.Purpose).HasConversion<byte>();
        builder.Property(file => file.StorageProvider).HasMaxLength(20).IsUnicode(false);
        builder.Property(file => file.StorageKey).HasMaxLength(400).IsUnicode(false);
        builder.HasIndex(file => file.StorageKey).IsUnique();
        builder.Property(file => file.OriginalFileName).HasMaxLength(255);
        builder.Property(file => file.ContentType).HasMaxLength(100).IsUnicode(false);
        builder.Property(file => file.Sha256).HasMaxLength(StoredFile.Sha256Length).IsFixedLength();
        builder.Property(file => file.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.HasOne<Identity.ApplicationUser>().WithMany().HasForeignKey(file => file.OwnerUserId);
        builder.HasIndex(file => file.OwnerUserId);
    }
}

internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments", table => table.HasCheckConstraint("CK_Enrollments_Status", "[Status] IN (0, 1, 2, 3, 4)"));
        builder.HasKey(enrollment => enrollment.Id);
        builder.Property(enrollment => enrollment.Status).HasConversion<byte>();
        builder.Property(enrollment => enrollment.RequestMessage).HasMaxLength(ColumnLengths.Note);
        builder.Property(enrollment => enrollment.DecisionNote).HasMaxLength(ColumnLengths.Note);
        builder.Property(enrollment => enrollment.RequestedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(enrollment => enrollment.DecidedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(enrollment => enrollment.LastActivityAtUtc).HasColumnType("datetime2(0)");
        builder.Property(enrollment => enrollment.RowVersion).IsRowVersion();

        // Attempts and submissions (Phase 5) reference (Id, StudentId) to stay consistent with the enrollment.
        builder.HasAlternateKey(enrollment => new { enrollment.Id, enrollment.StudentId });
        builder.HasOne(enrollment => enrollment.Course).WithMany().HasForeignKey(enrollment => enrollment.CourseId);
        builder.HasOne(enrollment => enrollment.Student).WithMany().HasForeignKey(enrollment => enrollment.StudentId);

        // At most one active (Pending or Approved) enrollment per student and course, even under concurrent requests.
        builder.HasIndex(enrollment => new { enrollment.CourseId, enrollment.StudentId })
            .IsUnique()
            .HasFilter("[Status] IN (0, 1)")
            .HasDatabaseName("UX_Enrollments_Active");
        builder.HasIndex(enrollment => new { enrollment.CourseId, enrollment.Status, enrollment.RequestedAtUtc });
        builder.HasIndex(enrollment => new { enrollment.StudentId, enrollment.Status });
        builder.HasIndex(enrollment => new { enrollment.CourseId, enrollment.Status, enrollment.LastActivityAtUtc });
    }
}
