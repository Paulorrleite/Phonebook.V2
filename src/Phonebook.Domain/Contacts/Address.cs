namespace Phonebook.Domain.Contacts;

public sealed class Address
{
    private Address()
    {
        Country = string.Empty;
        State = string.Empty;
        City = string.Empty;
        Neighborhood = string.Empty;
        PostalCode = string.Empty;
    }

    public Address(string country, string state, string city, string neighborhood, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(country) ||
            string.IsNullOrWhiteSpace(state) ||
            string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(neighborhood) ||
            string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainException("All address fields are required.");
        }

        Id = Guid.NewGuid();
        Country = country.Trim();
        State = state.Trim();
        City = city.Trim();
        Neighborhood = neighborhood.Trim();
        PostalCode = postalCode.Trim();
    }

    public Guid Id { get; private set; }

    public string Country { get; private set; }

    public string State { get; private set; }

    public string City { get; private set; }

    public string Neighborhood { get; private set; }

    public string PostalCode { get; private set; }
}
