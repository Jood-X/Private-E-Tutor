using ETutor.Application.Users;

namespace ETutor.UnitTests.Application;

public sealed class UserValidatorTests
{
    private static RegisterStudentRequest Valid() => new(
        "lina@etutor.test", "Correct-horse-42", "Lina", "en", "Asia/Amman", "JO", 1, 112, 1, [1, 2]);

    [Fact]
    public void ValidStudentRequest_Passes()
    {
        Assert.True(new RegisterStudentRequestValidator().Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("not-an-email", "Email")]
    [InlineData("", "Email")]
    public void InvalidEmail_IsRejected(string email, string property)
    {
        var result = new RegisterStudentRequestValidator().Validate(Valid() with { Email = email });

        Assert.Contains(result.Errors, error => error.PropertyName == property);
    }

    [Fact]
    public void DuplicateSubjects_AreRejected()
    {
        var result = new RegisterStudentRequestValidator().Validate(Valid() with { SubjectIds = [1, 1] });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterStudentRequest.SubjectIds));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("   ")]
    public void TooShortDisplayName_IsRejected(string name)
    {
        var result = new RegisterStudentRequestValidator().Validate(Valid() with { DisplayName = name });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterStudentRequest.DisplayName));
    }

    [Theory]
    [InlineData("Asia/Amman", true)]
    [InlineData("Asia/Riyadh", true)]
    [InlineData("Not/AZone", false)]
    public void TimeZone_MustBeIana(string zone, bool valid)
    {
        var result = new RegisterStudentRequestValidator().Validate(Valid() with { TimeZoneId = zone });

        Assert.Equal(valid, !result.Errors.Exists(error => error.PropertyName == nameof(RegisterStudentRequest.TimeZoneId)));
    }

    [Fact]
    public void TeacherHeadline_IsRequired()
    {
        var request = new RegisterTeacherRequest("t@etutor.test", "Correct-horse-42", "Khaled", "ar", "Asia/Amman", "JO", "");

        Assert.Contains(new RegisterTeacherRequestValidator().Validate(request).Errors,
            error => error.PropertyName == nameof(RegisterTeacherRequest.Headline));
    }
}
