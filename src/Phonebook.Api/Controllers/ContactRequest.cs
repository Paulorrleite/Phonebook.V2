using Phonebook.Application.Contacts;
using Phonebook.Domain.Contacts;

namespace Phonebook.Api.Controllers;

public sealed record ContactRequest(
    string FirstName,
    string? LastName,
    string? Email,
    IReadOnlyCollection<PhoneNumberRequest> PhoneNumbers,
    AddressRequest? Address,
    PhotoRequest? Photo,
    bool RemovePhoto = false)
{
    public CreateContactCommand ToCreateCommand()
    {
        return new CreateContactCommand(
            FirstName,
            LastName,
            Email,
            PhoneNumbers.Select(phone => phone.ToInput()).ToList(),
            Address?.ToInput(),
            Photo?.ToInput());
    }

    public UpdateContactCommand ToUpdateCommand(Guid id)
    {
        return new UpdateContactCommand(
            id,
            FirstName,
            LastName,
            Email,
            PhoneNumbers.Select(phone => phone.ToInput()).ToList(),
            Address?.ToInput(),
            Photo?.ToInput(),
            RemovePhoto);
    }
}

public sealed record PhoneNumberRequest(
    string CountryCode,
    string AreaCode,
    string Number,
    PhoneType Type,
    bool IsFavorite)
{
    public PhoneNumberInputDto ToInput()
    {
        return new PhoneNumberInputDto(CountryCode, AreaCode, Number, Type, IsFavorite);
    }
}

public sealed record AddressRequest(
    string? Country,
    string? State,
    string? City,
    string? Neighborhood,
    string? PostalCode)
{
    public AddressInputDto ToInput()
    {
        return new AddressInputDto(Country, State, City, Neighborhood, PostalCode);
    }
}

public sealed record PhotoRequest(
    byte[] Content,
    string ContentType,
    string FileName,
    long Size)
{
    public PhotoInputDto ToInput()
    {
        return new PhotoInputDto(Content, ContentType, FileName, Size);
    }
}

public sealed record CreateContactResponse(Guid Id);
