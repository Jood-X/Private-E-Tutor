namespace ETutor.Application.Abstractions;

/// <summary>The language negotiated for the current request (e.g. from Accept-Language).</summary>
public interface IRequestLanguage
{
    string Code { get; }
}
