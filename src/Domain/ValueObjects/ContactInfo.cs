namespace UnifiedServiceScheduler.Domain.ValueObjects;

/// <summary>
/// A customer's contact information — a value object because
/// two records with identical data are interchangeable.
/// </summary>
public sealed record ContactInfo
{
    public string FirstName   { get; }
    public string LastName    { get; }
    public string Email       { get; }
    public string PhoneNumber { get; }

    public string FullName => $"{FirstName} {LastName}";

    private ContactInfo()
    {
        FirstName   = string.Empty;
        LastName    = string.Empty;
        Email       = string.Empty;
        PhoneNumber = string.Empty;
    }

    private ContactInfo(string firstName, string lastName, string email, string phoneNumber)
    {
        FirstName   = firstName;
        LastName    = lastName;
        Email       = email;
        PhoneNumber = phoneNumber;
    }

    public static ContactInfo Create(
        string firstName, string lastName, string email, string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        // Minimal format guard — a real implementation delegates to a domain service.
        if (!email.Contains('@'))
            throw new ArgumentException("Email address must contain '@'.", nameof(email));

        return new ContactInfo(
            firstName.Trim(), lastName.Trim(),
            email.Trim().ToLowerInvariant(), phoneNumber.Trim());
    }

    public override string ToString() => $"{FullName} <{Email}>";
}
