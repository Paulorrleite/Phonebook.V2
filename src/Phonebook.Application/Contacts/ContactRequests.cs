using MediatR;
using Phonebook.Domain.Contacts;

namespace Phonebook.Application.Contacts;

public sealed record CreateContactCommand(
    string FirstName,
    string? LastName,
    string? Email,
    IReadOnlyCollection<PhoneNumberInputDto> PhoneNumbers,
    AddressInputDto? Address,
    PhotoInputDto? Photo) : IRequest<Guid>;

public sealed record UpdateContactCommand(
    Guid Id,
    string FirstName,
    string? LastName,
    string? Email,
    IReadOnlyCollection<PhoneNumberInputDto> PhoneNumbers,
    AddressInputDto? Address,
    PhotoInputDto? Photo,
    bool RemovePhoto) : IRequest;

public sealed record DeleteContactCommand(Guid Id) : IRequest;

public sealed record GetContactForEditQuery(Guid Id) : IRequest<ContactEditDto?>;

public sealed record ListContactsQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ContactListItemDto>>;

public sealed record PhoneNumberInputDto(
    string CountryCode,
    string AreaCode,
    string Number,
    PhoneType Type,
    bool IsFavorite);

public sealed record AddressInputDto(
    string? Country,
    string? State,
    string? City,
    string? Neighborhood,
    string? PostalCode);

public sealed record PhotoInputDto(
    byte[] Content,
    string ContentType,
    string FileName,
    long Size);
