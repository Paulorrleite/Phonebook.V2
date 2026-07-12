namespace Phonebook.Domain.Contacts;

public sealed class Contact
{
    public const int MinimumPhoneCount = 1;
    public const int MaximumPhoneCount = 10;

    private readonly List<PhoneNumber> _phoneNumbers = [];

    private Contact()
    {
        FirstName = string.Empty;
    }

    public Contact(
        string firstName,
        string? lastName,
        string? email,
        IEnumerable<PhoneNumber> phoneNumbers,
        Address? address = null,
        ContactPhoto? photo = null)
    {
        Id = Guid.NewGuid();
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;

        UpdateDetails(firstName, lastName, email);
        ReplacePhones(phoneNumbers);
        Address = address;
        Photo = photo;
    }

    public Guid Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string? LastName { get; private set; }

    public string? Email { get; private set; }

    public IReadOnlyCollection<PhoneNumber> PhoneNumbers => _phoneNumbers.AsReadOnly();

    public Address? Address { get; private set; }

    public ContactPhoto? Photo { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public PhoneNumber DisplayPhone =>
        _phoneNumbers.FirstOrDefault(phoneNumber => phoneNumber.IsFavorite) ??
        _phoneNumbers
            .OrderBy(phoneNumber => GetDisplayPriority(phoneNumber.Type))
            .First();

    public void UpdateDetails(string firstName, string? lastName, string? email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("First name is required.");
        }

        FirstName = firstName.Trim();
        LastName = NormalizeOptional(lastName);
        Email = NormalizeOptional(email);
        Touch();
    }

    public void ReplacePhones(IEnumerable<PhoneNumber> phoneNumbers)
    {
        var replacementPhones = phoneNumbers.ToList();

        if (replacementPhones.Count is < MinimumPhoneCount or > MaximumPhoneCount)
        {
            throw new DomainException("A contact must have between 1 and 10 phone numbers.");
        }

        if (replacementPhones.Select(phoneNumber => phoneNumber.NormalizedNumber).Distinct().Count() != replacementPhones.Count)
        {
            throw new DomainException("Phone numbers must be unique within a contact.");
        }

        if (replacementPhones.Count(phoneNumber => phoneNumber.IsFavorite) > 1)
        {
            throw new DomainException("Only one phone number can be favorite.");
        }

        _phoneNumbers.Clear();
        _phoneNumbers.AddRange(replacementPhones);
        Touch();
    }

    public void SetAddress(Address address)
    {
        Address = address;
        Touch();
    }

    public void RemoveAddress()
    {
        Address = null;
        Touch();
    }

    public void SetPhoto(ContactPhoto photo)
    {
        Photo = photo;
        Touch();
    }

    public void RemovePhoto()
    {
        Photo = null;
        Touch();
    }

    private static int GetDisplayPriority(PhoneType phoneType) =>
        phoneType switch
        {
            PhoneType.Mobile => 1,
            PhoneType.Residential => 2,
            PhoneType.Commercial => 3,
            _ => 4
        };

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
