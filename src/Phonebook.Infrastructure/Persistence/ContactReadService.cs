using Microsoft.EntityFrameworkCore;
using Phonebook.Application.Abstractions;
using Phonebook.Application.Contacts;
using Phonebook.Domain.Contacts;

namespace Phonebook.Infrastructure.Persistence;

public sealed class ContactReadService(PhonebookDbContext dbContext) : IContactReadService
{
    public Task<ContactEditDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Contacts
            .AsNoTracking()
            .Where(contact => contact.Id == id)
            .Select(contact => new ContactEditDto(
                contact.Id,
                contact.FirstName,
                contact.LastName,
                contact.Email,
                contact.PhoneNumbers
                    .OrderBy(phone => phone.Type == PhoneType.Mobile ? 1 : phone.Type == PhoneType.Residential ? 2 : 3)
                    .ThenBy(phone => phone.Id)
                    .Select(phone => new PhoneNumberEditDto(
                        phone.Id,
                        phone.CountryCode,
                        phone.AreaCode,
                        phone.Number,
                        phone.Type,
                        phone.IsFavorite))
                    .ToList(),
                contact.Address == null
                    ? null
                    : new AddressEditDto(
                        contact.Address.Id,
                        contact.Address.Country,
                        contact.Address.State,
                        contact.Address.City,
                        contact.Address.Neighborhood,
                        contact.Address.PostalCode),
                contact.Photo == null
                    ? null
                    : new PhotoSummaryDto(
                        contact.Photo.Id,
                        contact.Photo.ContentType,
                        contact.Photo.FileName,
                        contact.Photo.Size)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<ContactListItemDto>> ListAsync(ListContactsQuery query, CancellationToken cancellationToken)
    {
        var contactsQuery = dbContext.Contacts.AsNoTracking();

        var search = query.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var phoneDigits = NormalizeDigits(search);
            var pattern = $"%{search}%";

            contactsQuery = contactsQuery.Where(contact =>
                EF.Functions.ILike(contact.FirstName, pattern) ||
                (contact.LastName != null && EF.Functions.ILike(contact.LastName, pattern)) ||
                (contact.Email != null && EF.Functions.ILike(contact.Email, pattern)) ||
                (phoneDigits.Length > 0 && contact.PhoneNumbers.Any(phone => phone.NormalizedNumber.Contains(phoneDigits))));
        }

        var totalCount = await contactsQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var skip = (query.Page - 1) * query.PageSize;

        var items = await contactsQuery
            .OrderBy(contact => contact.FirstName)
            .ThenBy(contact => contact.LastName)
            .ThenBy(contact => contact.Id)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(contact => new ContactListItemDto(
                contact.Id,
                contact.Photo == null
                    ? null
                    : new PhotoSummaryDto(
                        contact.Photo.Id,
                        contact.Photo.ContentType,
                        contact.Photo.FileName,
                        contact.Photo.Size),
                contact.LastName == null ? contact.FirstName : contact.FirstName + " " + contact.LastName,
                contact.PhoneNumbers
                    .OrderByDescending(phone => phone.IsFavorite)
                    .ThenBy(phone => phone.Type == PhoneType.Mobile ? 1 : phone.Type == PhoneType.Residential ? 2 : 3)
                    .ThenBy(phone => phone.Id)
                    .Select(phone => new DisplayPhoneDto(
                        phone.CountryCode,
                        phone.AreaCode,
                        phone.Number,
                        phone.Type,
                        phone.IsFavorite))
                    .First(),
                contact.Email))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<ContactListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    private static string NormalizeDigits(string value)
    {
        Span<char> digits = stackalloc char[value.Length];
        var position = 0;

        foreach (var character in value)
        {
            if (char.IsDigit(character))
            {
                digits[position] = character;
                position++;
            }
        }

        return new string(digits[..position]);
    }
}
