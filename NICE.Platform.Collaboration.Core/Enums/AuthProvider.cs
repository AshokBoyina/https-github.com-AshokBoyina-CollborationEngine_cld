namespace NICE.Platform.Collaboration.Core.Enums;

/// <summary>
/// Identifies which external authentication validator to use.
/// Mapped from the <c>X-Access-Key</c> request header.
/// </summary>
public enum AuthProvider
{
    /// <summary>READI token — engine makes HTTP POST to the READI validate endpoint.</summary>
    READI,

    /// <summary>NICE token — engine makes HTTP POST to the NICE validate endpoint.</summary>
    NICE,

    /// <summary>Anonymous — engine decodes JWT internally, validates SurveyId / FirstName / LastName claims.</summary>
    ANON
}
