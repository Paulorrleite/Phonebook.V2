using System.Text;

namespace Phonebook.Domain.Contacts;

public sealed class PhoneNumber
{
    private PhoneNumber()
    {
        CountryCode = string.Empty;
        AreaCode = string.Empty;
        Number = string.Empty;
        NormalizedNumber = string.Empty;
    }

    public PhoneNumber(
        string countryCode,
        string areaCode,
        string number,
        PhoneType type,
        bool isFavorite = false)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new DomainException("Country code is required.");
        }

        if (string.IsNullOrWhiteSpace(areaCode))
        {
            throw new DomainException("Area code is required.");
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            throw new DomainException("Phone number is required.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Phone type is invalid.");
        }

        var normalizedNumber = Normalize(countryCode, areaCode, number);
        if (normalizedNumber.Length == 0)
        {
            throw new DomainException("Phone number must contain digits.");
        }

        Id = Guid.NewGuid();
        CountryCode = countryCode.Trim();
        AreaCode = areaCode.Trim();
        Number = number.Trim();
        NormalizedNumber = normalizedNumber;
        Type = type;
        IsFavorite = isFavorite;
    }

    public Guid Id { get; private set; }

    public string CountryCode { get; private set; }

    public string AreaCode { get; private set; }

    public string Number { get; private set; }

    public string NormalizedNumber { get; private set; }

    public PhoneType Type { get; private set; }

    public bool IsFavorite { get; private set; }

    public void MarkFavorite()
    {
        IsFavorite = true;
    }

    public void ClearFavorite()
    {
        IsFavorite = false;
    }

    public static string Normalize(string countryCode, string areaCode, string number)
    {
        var normalized = new StringBuilder();
        AppendDigits(countryCode, normalized);
        AppendDigits(areaCode, normalized);
        AppendDigits(number, normalized);

        return normalized.ToString();
    }

    private static void AppendDigits(string value, StringBuilder builder)
    {
        foreach (var character in value)
        {
            if (char.IsDigit(character))
            {
                builder.Append(character);
            }
        }
    }
}
