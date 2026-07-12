using Phonebook.Domain.Contacts;

namespace Phonebook.Application.Contacts;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record ContactListItemDto(
    Guid Id,
    PhotoSummaryDto? Photo,
    string Name,
    DisplayPhoneDto Phone,
    string? Email);

public sealed record DisplayPhoneDto(
    string CountryCode,
    string AreaCode,
    string Number,
    PhoneType Type,
    bool IsFavorite);

public sealed record ContactEditDto(
    Guid Id,
    string FirstName,
    string? LastName,
    string? Email,
    IReadOnlyCollection<PhoneNumberEditDto> PhoneNumbers,
    AddressEditDto? Address,
    PhotoSummaryDto? Photo);

public sealed record PhoneNumberEditDto(
    Guid Id,
    string CountryCode,
    string AreaCode,
    string Number,
    PhoneType Type,
    bool IsFavorite);

public sealed record AddressEditDto(
    Guid Id,
    string Country,
    string State,
    string City,
    string Neighborhood,
    string PostalCode);

public sealed record PhotoSummaryDto(
    Guid Id,
    string ContentType,
    string FileName,
    long Size);
