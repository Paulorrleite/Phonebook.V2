using System.Text.RegularExpressions;

namespace Phonebook.Application.Contacts.Validation;

public interface IPostalCodeRule
{
    bool AppliesTo(string country);

    bool IsValid(string postalCode);
}

public sealed class BrazilPostalCodeRule : IPostalCodeRule
{
    private static readonly Regex PostalCodeRegex = new(@"^\d{5}-?\d{3}$", RegexOptions.Compiled);

    public bool AppliesTo(string country) =>
        IsCountry(country, "BR") ||
        IsCountry(country, "BRA") ||
        IsCountry(country, "Brazil") ||
        IsCountry(country, "Brasil");

    public bool IsValid(string postalCode) => PostalCodeRegex.IsMatch(postalCode.Trim());

    private static bool IsCountry(string country, string expected) =>
        string.Equals(country.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}

public sealed class UnitedStatesPostalCodeRule : IPostalCodeRule
{
    private static readonly Regex PostalCodeRegex = new(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled);

    public bool AppliesTo(string country) =>
        IsCountry(country, "US") ||
        IsCountry(country, "USA") ||
        IsCountry(country, "United States") ||
        IsCountry(country, "United States of America");

    public bool IsValid(string postalCode) => PostalCodeRegex.IsMatch(postalCode.Trim());

    private static bool IsCountry(string country, string expected) =>
        string.Equals(country.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}

public sealed class FallbackPostalCodeRule : IPostalCodeRule
{
    public bool AppliesTo(string country) => !string.IsNullOrWhiteSpace(country);

    public bool IsValid(string postalCode) => !string.IsNullOrWhiteSpace(postalCode);
}

public interface IPostalCodeValidator
{
    bool IsValid(string country, string postalCode);
}

public sealed class PostalCodeValidator : IPostalCodeValidator
{
    private readonly IReadOnlyCollection<IPostalCodeRule> _rules;

    public PostalCodeValidator()
        : this([new BrazilPostalCodeRule(), new UnitedStatesPostalCodeRule(), new FallbackPostalCodeRule()])
    {
    }

    public PostalCodeValidator(IReadOnlyCollection<IPostalCodeRule> rules)
    {
        _rules = rules;
    }

    public bool IsValid(string country, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(postalCode))
        {
            return false;
        }

        var rule = _rules.First(rule => rule.AppliesTo(country));
        return rule.IsValid(postalCode);
    }
}
