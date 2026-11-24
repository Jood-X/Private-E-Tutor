using ETutor.Domain.Content;
using ETutor.Domain.Enrollments;
using Actor = ETutor.Domain.Enrollments.EnrollmentTransitions.Actor;
using Problem = ETutor.Domain.Content.ContentRules.CourseStatusProblem;
using Status = ETutor.Domain.Enrollments.EnrollmentStatus;

namespace ETutor.UnitTests.Domain;

public sealed class ContentAndEnrollmentRulesTests
{
    [Theory]
    [InlineData(Status.Pending, Status.Approved, Actor.Teacher, true)]
    [InlineData(Status.Pending, Status.Rejected, Actor.Teacher, true)]
    [InlineData(Status.Approved, Status.Revoked, Actor.Teacher, true)]
    [InlineData(Status.Pending, Status.Withdrawn, Actor.Student, true)]
    [InlineData(Status.Approved, Status.Withdrawn, Actor.Student, true)]
    [InlineData(Status.Pending, Status.Approved, Actor.Student, false)]   // students can't approve themselves
    [InlineData(Status.Rejected, Status.Approved, Actor.Teacher, false)]  // terminal states stay terminal
    [InlineData(Status.Withdrawn, Status.Approved, Actor.Teacher, false)]
    [InlineData(Status.Approved, Status.Approved, Actor.Teacher, false)]
    [InlineData(Status.Pending, Status.Revoked, Actor.Teacher, false)]
    public void Enrollment_Transitions(Status from, Status to, Actor actor, bool allowed)
    {
        Assert.Equal(allowed, EnrollmentTransitions.CanMove(from, to, actor));
    }

    [Fact]
    public void Only_Pending_And_Approved_Are_Active()
    {
        Assert.True(EnrollmentTransitions.IsActive(Status.Pending));
        Assert.True(EnrollmentTransitions.IsActive(Status.Approved));
        Assert.False(EnrollmentTransitions.IsActive(Status.Rejected));
        Assert.False(EnrollmentTransitions.IsActive(Status.Withdrawn));
        Assert.False(EnrollmentTransitions.IsActive(Status.Revoked));
    }

    [Theory]
    [InlineData(ContentStatus.Draft, ContentStatus.Published, true, false, null)]
    [InlineData(ContentStatus.Draft, ContentStatus.Published, false, false, Problem.NoPublishedLesson)]
    [InlineData(ContentStatus.Published, ContentStatus.Archived, true, true, null)]
    [InlineData(ContentStatus.Archived, ContentStatus.Published, true, true, null)]
    [InlineData(ContentStatus.Published, ContentStatus.Draft, true, true, Problem.HasStudents)]
    [InlineData(ContentStatus.Published, ContentStatus.Draft, true, false, null)]
    [InlineData(ContentStatus.Draft, ContentStatus.Archived, true, false, Problem.NotAllowed)]
    [InlineData(ContentStatus.Published, ContentStatus.Published, false, true, null)]
    public void Course_Status_Changes(ContentStatus from, ContentStatus to, bool hasPublishedLesson, bool hasStudents, Problem? expected)
    {
        Assert.Equal(expected, ContentRules.CheckCourseStatusChange(from, to, hasPublishedLesson, hasStudents));
    }

    [Theory]
    [InlineData("https://example.com/notes", true)]
    [InlineData("http://example.com/notes", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("example.com", false)]
    public void Links_Must_Be_Https(string url, bool allowed)
    {
        Assert.Equal(allowed, ContentRules.IsAllowedLink(url));
    }

    [Fact]
    public void Sort_Orders_Leave_Gaps()
    {
        Assert.Equal(1000, ContentRules.NextSortOrder(null));
        Assert.Equal(4000, ContentRules.NextSortOrder(3000));
        Assert.True(ContentRules.CanDelete(ContentStatus.Draft));
        Assert.False(ContentRules.CanDelete(ContentStatus.Published));
    }
}
