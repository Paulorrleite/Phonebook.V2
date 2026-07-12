using Phonebook.Application.Contacts;

namespace Phonebook.Application.Abstractions;

public interface IContactReadService
{
    Task<ContactEditDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ContactListItemDto>> ListAsync(ListContactsQuery query, CancellationToken cancellationToken);
}
