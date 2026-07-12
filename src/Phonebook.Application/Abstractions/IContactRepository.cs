using Phonebook.Domain.Contacts;

namespace Phonebook.Application.Abstractions;

public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, Guid? excludingContactId, CancellationToken cancellationToken);

    Task AddAsync(Contact contact, CancellationToken cancellationToken);

    void Delete(Contact contact);
}
