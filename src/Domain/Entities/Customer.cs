using UnifiedServiceScheduler.Domain.ValueObjects;

namespace UnifiedServiceScheduler.Domain.Entities;

/// <summary>
/// Customer entity — lightweight participant within the Scheduling bounded context.
/// The canonical Customer profile lives in a separate Identity/CRM context
/// and is referenced here by <see cref="CustomerId"/> only.
/// </summary>
public sealed class Customer
{
    // Private constructor required by EF Core for materialization of owned type (ContactInfo).
    private Customer()
    {
        Id      = null!;
        Contact = null!;
    }

    private Customer(CustomerId id, ContactInfo contact)
    {
        Id      = id;
        Contact = contact;
    }

    public CustomerId  Id      { get; }
    public ContactInfo Contact { get; private set; }   // mutable: contact details may change

    public static Customer Create(CustomerId id, ContactInfo contact)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(contact);
        return new Customer(id, contact);
    }

    /// <summary>Replaces contact details (e.g., new phone number or email).</summary>
    public void UpdateContact(ContactInfo newContact)
    {
        ArgumentNullException.ThrowIfNull(newContact);
        Contact = newContact;
    }

    public override string ToString() => $"Customer({Id}) {Contact.FullName}";
}
