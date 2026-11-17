using ETutor.Domain.Catalog;
using ETutor.Infrastructure.Persistence.Conversions;
using ETutor.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETutor.Infrastructure.Persistence.Configurations;

// Catalog tables are small, closely related reference data, so their configurations share one file.
// Tables for business aggregates get one configuration file each.

internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");
        builder.HasKey(country => country.Id);
        builder.Property(country => country.Id).ValueGeneratedNever();
        builder.Property(country => country.IsoCode)
            .HasMaxLength(ColumnLengths.IsoCountryCode).IsFixedLength().IsUnicode(false);
        builder.HasIndex(country => country.IsoCode).IsUnique();
        builder.Property(country => country.Name).IsLocalizedText();
        builder.Property(country => country.DefaultTimeZoneId).HasMaxLength(ColumnLengths.TimeZoneId).IsUnicode(false);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Countries_IsoCode_Upper", "[IsoCode] = UPPER([IsoCode]) COLLATE Latin1_General_CS_AS"));
        builder.HasData(CatalogSeedData.Countries);
    }
}

internal sealed class CurriculumConfiguration : IEntityTypeConfiguration<Curriculum>
{
    public void Configure(EntityTypeBuilder<Curriculum> builder)
    {
        builder.ToTable("Curricula");
        builder.HasKey(curriculum => curriculum.Id);
        builder.Property(curriculum => curriculum.Id).ValueGeneratedNever();
        builder.Property(curriculum => curriculum.Code).HasMaxLength(ColumnLengths.Code).IsUnicode(false);
        builder.HasIndex(curriculum => curriculum.Code).IsUnique();
        builder.Property(curriculum => curriculum.Name).IsLocalizedText();
        builder.HasData(CatalogSeedData.Curricula);
    }
}

internal sealed class CurriculumAvailabilityConfiguration : IEntityTypeConfiguration<CurriculumAvailability>
{
    public void Configure(EntityTypeBuilder<CurriculumAvailability> builder)
    {
        builder.ToTable("CurriculumAvailabilities");
        builder.HasKey(link => new { link.CountryId, link.CurriculumId });
        builder.HasIndex(link => link.CurriculumId);
        builder.HasOne(link => link.Country).WithMany(country => country.CurriculumAvailabilities)
            .HasForeignKey(link => link.CountryId);
        builder.HasOne(link => link.Curriculum).WithMany(curriculum => curriculum.Availabilities)
            .HasForeignKey(link => link.CurriculumId);
        builder.HasData(CatalogSeedData.CurriculumAvailabilities);
    }
}

internal sealed class CurriculumStreamConfiguration : IEntityTypeConfiguration<CurriculumStream>
{
    public void Configure(EntityTypeBuilder<CurriculumStream> builder)
    {
        builder.ToTable("CurriculumStreams");
        builder.HasKey(stream => stream.Id);
        builder.Property(stream => stream.Id).ValueGeneratedNever();
        builder.Property(stream => stream.Code).HasMaxLength(ColumnLengths.Code).IsUnicode(false);
        builder.Property(stream => stream.Name).IsLocalizedText();
        builder.HasIndex(stream => new { stream.CurriculumId, stream.Code }).IsUnique();

        // Alternate key so students' (StreamId, CurriculumId) can be enforced with a composite FK.
        builder.HasAlternateKey(stream => new { stream.Id, stream.CurriculumId });
        builder.HasOne(stream => stream.Curriculum).WithMany(curriculum => curriculum.Streams)
            .HasForeignKey(stream => stream.CurriculumId);
        builder.HasData(CatalogSeedData.Streams);
    }
}

internal sealed class GradeLevelConfiguration : IEntityTypeConfiguration<GradeLevel>
{
    public void Configure(EntityTypeBuilder<GradeLevel> builder)
    {
        builder.ToTable("GradeLevels");
        builder.HasKey(grade => grade.Id);
        builder.Property(grade => grade.Id).ValueGeneratedNever();
        builder.Property(grade => grade.Code).HasMaxLength(ColumnLengths.Code).IsUnicode(false);
        builder.Property(grade => grade.Name).IsLocalizedText();
        builder.HasIndex(grade => new { grade.CurriculumId, grade.Code }).IsUnique();
        builder.HasIndex(grade => new { grade.CurriculumId, grade.Ordinal }).IsUnique();

        // Alternate key used by composite FKs from courses and student profiles (later phases).
        builder.HasAlternateKey(grade => new { grade.Id, grade.CurriculumId });
        builder.HasOne(grade => grade.Curriculum).WithMany(curriculum => curriculum.GradeLevels)
            .HasForeignKey(grade => grade.CurriculumId);
        builder.HasData(CatalogSeedData.GradeLevels);
    }
}

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");
        builder.HasKey(subject => subject.Id);
        builder.Property(subject => subject.Id).ValueGeneratedNever();
        builder.Property(subject => subject.Code).HasMaxLength(ColumnLengths.Code).IsUnicode(false);
        builder.HasIndex(subject => subject.Code).IsUnique();
        builder.Property(subject => subject.Name).IsLocalizedText();
        builder.HasData(CatalogSeedData.Subjects);
    }
}

internal sealed class GradeSubjectConfiguration : IEntityTypeConfiguration<GradeSubject>
{
    public void Configure(EntityTypeBuilder<GradeSubject> builder)
    {
        builder.ToTable("GradeSubjects");
        builder.HasKey(link => new { link.GradeLevelId, link.SubjectId });
        builder.HasIndex(link => link.SubjectId);
        builder.HasOne(link => link.GradeLevel).WithMany(grade => grade.Subjects)
            .HasForeignKey(link => link.GradeLevelId);
        builder.HasOne(link => link.Subject).WithMany(subject => subject.GradeLevels)
            .HasForeignKey(link => link.SubjectId);
        builder.HasData(CatalogSeedData.GradeSubjects);
    }
}

internal sealed class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> builder)
    {
        builder.ToTable("AcademicYears", table => table.HasCheckConstraint(
            "CK_AcademicYears_StartBeforeEnd", "[StartDate] < [EndDate]"));
        builder.HasKey(year => year.Id);
        builder.Property(year => year.Id).ValueGeneratedNever();
        builder.Property(year => year.Label).HasMaxLength(ColumnLengths.AcademicYearLabel);
        builder.HasIndex(year => new { year.CurriculumId, year.Label }).IsUnique();
        builder.HasAlternateKey(year => new { year.Id, year.CurriculumId });
        builder.HasOne(year => year.Curriculum).WithMany(curriculum => curriculum.AcademicYears)
            .HasForeignKey(year => year.CurriculumId);
        builder.HasData(CatalogSeedData.AcademicYears);
    }
}
