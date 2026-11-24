using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Domain.Content;
using ETutor.Domain.Enrollments;
using ETutor.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ETutor.Application.Enrollments;

/// <summary>Student side of enrollment: request access to a course, withdraw, list own enrollments.</summary>
public sealed class EnrollmentService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IRequestLanguage language,
    IClock clock,
    IDatabaseErrors databaseErrors,
    ILogger<EnrollmentService> logger)
{
    public const int MaxMessageLength = 500;

    public async Task<Result<MyEnrollmentDto>> RequestAsync(Guid courseId, EnrollmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
        if (message?.Length > MaxMessageLength)
        {
            return Error.Validation(ErrorCodes.Common.ValidationFailed, "The message can be up to 500 characters.", "message");
        }

        // Only published courses of verified teachers can be requested.
        var course = await db.Courses.AsNoTracking()
            .Where(item => item.Id == courseId && item.Status == ContentStatus.Published
                && item.Teacher!.VerificationStatus == TeacherVerificationStatus.Verified)
            .Select(item => new { item.Teacher!.IsAcceptingStudents })
            .FirstOrDefaultAsync(cancellationToken);
        if (course is null)
        {
            return Error.NotFound(ErrorCodes.Course.NotFound, "Course not found.");
        }

        if (!course.IsAcceptingStudents)
        {
            return Error.BusinessRule(ErrorCodes.Enrollment.NotAcceptingStudents, "This teacher is not accepting new students right now.");
        }

        var studentId = currentUser.RequireUserId();
        var enrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            StudentId = studentId,
            RequestMessage = message,
            RequestedAtUtc = clock.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (databaseErrors.IsUniqueViolation(exception))
        {
            // The filtered unique index guarantees one active request, even for concurrent taps.
            EnrollmentLog.DuplicateRequest(logger, courseId);
            return Error.Conflict(ErrorCodes.Enrollment.AlreadyRequested, "You already have a request or enrollment for this course.");
        }

        EnrollmentLog.Requested(logger, enrollment.Id, courseId);
        return await GetMineAsync(enrollment.Id, cancellationToken);
    }

    public async Task<Result<MyEnrollmentDto>> WithdrawAsync(Guid enrollmentId, CancellationToken cancellationToken)
    {
        var studentId = currentUser.RequireUserId();
        var enrollment = await db.Enrollments.FirstOrDefaultAsync(
            item => item.Id == enrollmentId && item.StudentId == studentId, cancellationToken);
        if (enrollment is null)
        {
            return Error.NotFound(ErrorCodes.Enrollment.NotFound, "Enrollment not found.");
        }

        if (!EnrollmentTransitions.CanMove(enrollment.Status, EnrollmentStatus.Withdrawn, EnrollmentTransitions.Actor.Student))
        {
            return Error.BusinessRule(ErrorCodes.Enrollment.InvalidTransition, "This enrollment can no longer be withdrawn.");
        }

        enrollment.Status = EnrollmentStatus.Withdrawn;
        enrollment.DecidedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await GetMineAsync(enrollmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<MyEnrollmentDto>> ListMineAsync(CancellationToken cancellationToken)
    {
        var studentId = currentUser.RequireUserId();
        // Order on the entity, before projecting: EF cannot translate ordering on constructor-built records.
        var rows = await Project(db.Enrollments.AsNoTracking()
                .Where(item => item.StudentId == studentId)
                .OrderBy(item => item.Status)
                .ThenByDescending(item => item.RequestedAtUtc))
            .ToListAsync(cancellationToken);
        return rows.ConvertAll(ToDto);
    }

    private async Task<Result<MyEnrollmentDto>> GetMineAsync(Guid enrollmentId, CancellationToken cancellationToken)
    {
        var studentId = currentUser.RequireUserId();
        var row = await Project(db.Enrollments.AsNoTracking().Where(item => item.Id == enrollmentId && item.StudentId == studentId))
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? Error.NotFound(ErrorCodes.Enrollment.NotFound, "Enrollment not found.") : ToDto(row);
    }

    private static IQueryable<Row> Project(IQueryable<Enrollment> query) => query.Select(item => new Row(
        item.Id,
        item.CourseId,
        item.Course!.Title,
        item.Course.Subject!.Name,
        item.Course.Teacher!.User!.DisplayName,
        item.Status,
        item.DecisionNote,
        item.RequestedAtUtc,
        item.DecidedAtUtc));

    private MyEnrollmentDto ToDto(Row row) => new(
        row.Id, row.CourseId, row.CourseTitle, row.SubjectName.Resolve(language.Code), row.TeacherName,
        row.Status, row.DecisionNote, row.RequestedAtUtc, row.DecidedAtUtc);

    private sealed record Row(
        Guid Id,
        Guid CourseId,
        string CourseTitle,
        Domain.Common.LocalizedText SubjectName,
        string TeacherName,
        EnrollmentStatus Status,
        string? DecisionNote,
        DateTime RequestedAtUtc,
        DateTime? DecidedAtUtc);
}
