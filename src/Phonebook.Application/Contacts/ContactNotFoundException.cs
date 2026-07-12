namespace Phonebook.Application.Contacts;

public sealed class ContactNotFoundException(Guid contactId)
    : Exception($"Contact '{contactId}' was not found.")
{
    public Guid ContactId { get; } = contactId;
}
