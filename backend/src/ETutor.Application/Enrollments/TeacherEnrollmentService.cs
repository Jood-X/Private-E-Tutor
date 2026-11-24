using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ETutor.Application.Enrollments;

/// <summary>
/// Teacher side of enrollment: the request queue, approve/reject, revoke, and the student list.
/// Only enrollments in the caller's own courses are visible (others are 404).
/// </summary>
public sealed class TeacherEnrollmentService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IRequestLanguage language,
    IClock clock,
    ILogger<TeacherEnrollmentService> logger)
{
    public const int MaxNoteLength = 500;

    public async Task<PagedResult<TeacherEnrollmentDto>> ListAsync(EnrollmentQuery query, PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var enrollments = Owned().AsNoTracking();
        if (query.Status is { } status)
        {
            enrollments = enrollments.Where(item => item.Status == status);
        }

        if (query.CourseId is { } courseId)
        {
            enrollments = enrollments.Where(item => item.CourseId == courseId);
        }

        var rows = await Project(enrollments.OrderBy(item => item.Status).ThenBy(item => item.RequestedAtUtc))
            .ToPagedAsync(page, cancellationToken);
        return rows.Map(ToDto);
    }

    public Task<Result<TeacherEnrollmentDto>> ApproveAsync(Guid enrollmentId, DecisionRequest request, CancellationToken cancellationToken) =>
        DecideAsync(enrollmentId, EnrollmentStatus.Approved, request, cancellationToken);

    public Task<Result<TeacherEnrollmentDto>> RejectAsync(Guid enrollmentId, DecisionRequest request, CancellationToken cancellationToken) =>
        DecideAsync(enrollmentId, EnrollmentStatus.Rejected, request, cancellationToken);

    public Task<Result<TeacherEnrollmentDto>> RevokeAsync(Guid enrollmentId, DecisionRequest request, CancellationToken cancellationToken) =>
        DecideAsync(enrollmentId, EnrollmentStatus.Revoked, request, cancellationToken);

    private async Task<Result<TeacherEnrollmentDto>> DecideAsync(
        Guid enrollmentId, EnrollmentStatus target, DecisionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note?.Length > MaxNoteLength)
        {
            return Error.Validation(ErrorCodes.Common.ValidationFailed, "The note can be up to 500 characters.", "note");
        }

        var enrollment = await Owned().FirstOrDefaultAsync(item => item.Id == enrollmentId, cancellationToken);
        if (enrollment is null)
        {
            return Error.NotFound(ErrorCodes.Enrollment.NotFound, "Enrollment not found.");
        }

        if (!EnrollmentTransitions.CanMove(enrollment.Status, target, EnrollmentTransitions.Actor.Teacher))
        {
            return Error.BusinessRule(ErrorCodes.Enrollment.InvalidTransition, $"A {enrollment.Status} enrollment cannot become {target}.");
        }

        enrollment.Status = target;
        enrollment.DecisionNote = note;
        enrollment.DecidedAtUtc = clock.UtcNow;

        // RowVersion makes a concurrent decision (e.g. student withdrew meanwhile) fail with 409, not overwrite.
        await db.SaveChangesAsync(cancellationToken);
        EnrollmentLog.Decided(logger, enrollmentId, target);

        var row = await Project(Owned().AsNoTracking().Where(item => item.Id == enrollmentId)).FirstAsync(cancellationToken);
        return ToDto(row);
    }

    private IQueryable<Enrollment> Owned()
    {
        var teacherId = currentUser.RequireUserId();
        return db.Enrollments.Where(item => item.Course!.TeacherId == teacherId);
    }

    private static IQueryable<Row> Project(IQueryable<Enrollment> query) => query.Select(item => new Row(
        item.Id,
        item.CourseId,
        item.Course!.Title,
        item.StudentId,
        item.Student!.User!.DisplayName,
        item.Student.GradeLevel!.Name,
        item.Status,
        item.RequestMessage,
        item.RequestedAtUtc,
        item.DecidedAtUtc,
        item.LastActivityAtUtc));

    private TeacherEnrollmentDto ToDto(Row row) => new(
        row.Id, row.CourseId, row.CourseTitle, row.StudentId, row.StudentName, row.GradeName.Resolve(language.Code),
        row.Status, row.RequestMessage, row.RequestedAtUtc, row.DecidedAtUtc, row.LastActivityAtUtc);

    private sealed record Row(
        Guid Id,
        Guid CourseId,
        string CourseTitle,
        Guid StudentId,
        string StudentName,
        Domain.Common.LocalizedText GradeName,
        EnrollmentStatus Status,
        string? RequestMessage,
        DateTime RequestedAtUtc,
        DateTime? DecidedAtUtc,
        DateTime? LastActivityAtUtc);
}
