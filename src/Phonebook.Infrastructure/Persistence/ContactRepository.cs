using Microsoft.EntityFrameworkCore;
using Phonebook.Application.Abstractions;
using Phonebook.Domain.Contacts;

namespace Phonebook.Infrastructure.Persistence;

public sealed class ContactRepository(PhonebookDbContext dbContext) : IContactRepository
{
    public Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Contacts
            .Include(contact => contact.PhoneNumbers)
            .Include(contact => contact.Address)
            .Include(contact => contact.Photo)
            .FirstOrDefaultAsync(contact => contact.Id == id, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludingContactId, CancellationToken cancellationToken)
    {
        return dbContext.Contacts
            .AnyAsync(contact =>
                contact.Email == email &&
                (!excludingContactId.HasValue || contact.Id != excludingContactId.Value),
                cancellationToken);
    }

    public Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        return dbContext.Contacts.AddAsync(contact, cancellationToken).AsTask();
    }

    public void Delete(Contact contact)
    {
        dbContext.Contacts.Remove(contact);
    }
}
