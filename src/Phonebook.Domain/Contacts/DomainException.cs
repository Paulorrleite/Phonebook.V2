namespace Phonebook.Domain.Contacts;

public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
