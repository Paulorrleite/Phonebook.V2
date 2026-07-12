namespace Phonebook.Application.Abstractions;

public interface IPhonebookUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
