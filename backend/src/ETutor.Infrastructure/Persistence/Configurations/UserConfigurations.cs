using ETutor.Domain.Users;
using ETutor.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ETutor.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.DisplayName).HasMaxLength(ColumnLengths.DisplayName);
        builder.Property(profile => profile.PreferredLanguage).HasMaxLength(ColumnLengths.LanguageCode).IsUnicode(false);
        builder.Property(profile => profile.TimeZoneId).HasMaxLength(ColumnLengths.TimeZoneId).IsUnicode(false);
        builder.Property(profile => profile.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(profile => profile.UpdatedAtUtc).HasColumnType("datetime2(0)");

        // 1:1 with the identity account; the profile cannot exist without it.
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<UserProfile>(profile => profile.Id);
        builder.HasOne(profile => profile.Country).WithMany().HasForeignKey(profile => profile.CountryId);
        builder.HasIndex(profile => profile.CountryId);
    }
}

internal sealed class TeacherProfileConfiguration : IEntityTypeConfiguration<TeacherProfile>
{
    public void Configure(EntityTypeBuilder<TeacherProfile> builder)
    {
        builder.ToTable("TeacherProfiles", table => table.HasCheckConstraint(
            "CK_TeacherProfiles_VerificationStatus", "[VerificationStatus] IN (0, 1, 2)"));
        builder.HasKey(teacher => teacher.UserId);
        builder.Property(teacher => teacher.Headline).HasMaxLength(ColumnLengths.Headline);
        builder.Property(teacher => teacher.Bio).HasMaxLength(ColumnLengths.Bio);
        builder.Property(teacher => teacher.VerificationStatus).HasConversion<byte>();
        builder.Property(teacher => teacher.VerifiedAtUtc).HasColumnType("datetime2(0)");
        builder.HasOne(teacher => teacher.User).WithOne(user => user.Teacher).HasForeignKey<TeacherProfile>(teacher => teacher.UserId);

        // Discovery lists verified teachers who accept students.
        builder.HasIndex(teacher => new { teacher.VerificationStatus, teacher.IsAcceptingStudents });
    }
}

internal sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("StudentProfiles");
        builder.HasKey(student => student.UserId);
        builder.HasOne(student => student.User).WithOne(user => user.Student).HasForeignKey<StudentProfile>(student => student.UserId);
        builder.HasOne(student => student.Curriculum).WithMany().HasForeignKey(student => student.CurriculumId);

        // Composite FKs: the grade and stream must belong to the student's curriculum.
        builder.HasOne(student => student.GradeLevel).WithMany()
            .HasForeignKey(student => new { student.GradeLevelId, student.CurriculumId })
            .HasPrincipalKey(grade => new { grade.Id, grade.CurriculumId });
        builder.HasOne(student => student.Stream).WithMany()
            .HasForeignKey(student => new { student.StreamId, student.CurriculumId })
            .HasPrincipalKey(stream => new { stream.Id, stream.CurriculumId });

        // The composite FK on (GradeLevelId, CurriculumId) already provides the index used to find
        // students by grade, so no extra index is declared.
    }
}

internal sealed class StudentSubjectConfiguration : IEntityTypeConfiguration<StudentSubject>
{
    public void Configure(EntityTypeBuilder<StudentSubject> builder)
    {
        builder.ToTable("StudentSubjects");
        builder.HasKey(link => new { link.StudentId, link.SubjectId });
        builder.HasIndex(link => link.SubjectId);
        builder.HasOne(link => link.Student).WithMany(student => student.Subjects).HasForeignKey(link => link.StudentId)
            .OnDelete(DeleteBehavior.Cascade).HasAnnotation(AppDbContext.CascadeAnnotation, true);
        builder.HasOne(link => link.Subject).WithMany().HasForeignKey(link => link.SubjectId);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();
        builder.Property(token => token.TokenHash).HasMaxLength(RefreshToken.HashLength).IsFixedLength();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.FamilyId);
        builder.HasIndex(token => token.UserId);
        builder.Property(token => token.CreatedAtUtc).HasColumnType("datetime2(0)");
        builder.Property(token => token.ExpiresAtUtc).HasColumnType("datetime2(0)");
        builder.Property(token => token.RevokedAtUtc).HasColumnType("datetime2(0)");
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(token => token.UserId);
    }
}

/// <summary>Seeds the roles with fixed IDs and stamps so migrations stay stable.</summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder) => builder.HasData(
        Role("6f9619ff-8b86-d011-b42d-00c04fc96401", RoleNames.Student),
        Role("6f9619ff-8b86-d011-b42d-00c04fc96402", RoleNames.Teacher),
        Role("6f9619ff-8b86-d011-b42d-00c04fc96403", RoleNames.Admin));

    private static IdentityRole<Guid> Role(string id, string name) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id,
    };
}
