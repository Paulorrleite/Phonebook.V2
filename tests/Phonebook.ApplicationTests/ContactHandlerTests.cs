using FluentValidation;
using Phonebook.Application.Abstractions;
using Phonebook.Application.Contacts;
using Phonebook.Application.Contacts.Validation;
using Phonebook.Domain.Contacts;
using Xunit;

namespace Phonebook.ApplicationTests;

public sealed class ContactHandlerTests
{
    [Fact]
    public async Task CreateContactCommandHandler_PersistsValidContact()
    {
        var repository = new FakeContactRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new CreateContactCommandHandler(new CreateContactCommandValidator(), repository, unitOfWork);

        var id = await handler.Handle(ValidCreateCommand("ada@example.com"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        Assert.Single(repository.Contacts);
        Assert.True(unitOfWork.Saved);
    }

    [Fact]
    public async Task CreateContactCommandHandler_RejectsDuplicateEmail()
    {
        var repository = new FakeContactRepository();
        await repository.AddAsync(CreateContact("existing@example.com"), CancellationToken.None);
        var handler = new CreateContactCommandHandler(new CreateContactCommandValidator(), repository, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(ValidCreateCommand("existing@example.com"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateContactCommandHandler_UpdatesContactAndSaves()
    {
        var repository = new FakeContactRepository();
        var contact = CreateContact("before@example.com");
        await repository.AddAsync(contact, CancellationToken.None);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateContactCommandHandler(new UpdateContactCommandValidator(), repository, unitOfWork);

        await handler.Handle(new UpdateContactCommand(
            contact.Id,
            "After",
            null,
            "after@example.com",
            [Phone("88888-7777", PhoneType.Residential)],
            null,
            null,
            true), CancellationToken.None);

        Assert.Equal("After", contact.FirstName);
        Assert.Equal("after@example.com", contact.Email);
        Assert.Single(contact.PhoneNumbers);
        Assert.Null(contact.Photo);
        Assert.True(unitOfWork.Saved);
    }

    [Fact]
    public async Task UpdateContactCommandHandler_PreservesExistingPhotoWhenNoPhotoChangeIsRequested()
    {
        var repository = new FakeContactRepository();
        var contact = CreateContact("before@example.com");
        await repository.AddAsync(contact, CancellationToken.None);
        var existingPhoto = contact.Photo;
        var handler = new UpdateContactCommandHandler(
            new UpdateContactCommandValidator(),
            repository,
            new FakeUnitOfWork());

        await handler.Handle(new UpdateContactCommand(
            contact.Id,
            "After",
            null,
            "after@example.com",
            [Phone("88888-7777", PhoneType.Residential)],
            null,
            null,
            false), CancellationToken.None);

        Assert.Same(existingPhoto, contact.Photo);
    }

    [Fact]
    public async Task UpdateContactCommandHandler_RejectsMissingContact()
    {
        var handler = new UpdateContactCommandHandler(
            new UpdateContactCommandValidator(),
            new FakeContactRepository(),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<ContactNotFoundException>(
            () => handler.Handle(new UpdateContactCommand(
                Guid.NewGuid(),
                "Missing",
                null,
                null,
                [Phone()],
                null,
                null,
                false), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteContactCommandHandler_RemovesContactAndSaves()
    {
        var repository = new FakeContactRepository();
        var contact = CreateContact("delete@example.com");
        await repository.AddAsync(contact, CancellationToken.None);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new DeleteContactCommandHandler(new DeleteContactCommandValidator(), repository, unitOfWork);

        await handler.Handle(new DeleteContactCommand(contact.Id), CancellationToken.None);

        Assert.Empty(repository.Contacts);
        Assert.True(unitOfWork.Saved);
    }

    [Fact]
    public async Task QueryHandlers_ValidateAndDelegateToReadService()
    {
        var expectedContact = new ContactEditDto(Guid.NewGuid(), "Ada", null, null, [], null, null);
        var expectedPage = new PagedResult<ContactListItemDto>([], 1, 20, 0);
        var readService = new FakeContactReadService(expectedContact, expectedPage);
        var getHandler = new GetContactForEditQueryHandler(new GetContactForEditQueryValidator(), readService);
        var listHandler = new ListContactsQueryHandler(new ListContactsQueryValidator(), readService);

        var contact = await getHandler.Handle(new GetContactForEditQuery(expectedContact.Id), CancellationToken.None);
        var page = await listHandler.Handle(new ListContactsQuery(null), CancellationToken.None);

        Assert.Same(expectedContact, contact);
        Assert.Same(expectedPage, page);
        Assert.Equal(expectedContact.Id, readService.RequestedContactId);
        Assert.NotNull(readService.RequestedListQuery);
    }

    private static CreateContactCommand ValidCreateCommand(string email) =>
        new(
            "Ada",
            null,
            email,
            [Phone()],
            null,
            null);

    private static Contact CreateContact(string email) =>
        new(
            "Existing",
            null,
            email,
            [new PhoneNumber("+55", "11", "99999-1111", PhoneType.Mobile)],
            null,
            new ContactPhoto([1], "image/png", "avatar.png", 1));

    private static PhoneNumberInputDto Phone(
        string number = "99999-1111",
        PhoneType type = PhoneType.Mobile,
        bool isFavorite = false) =>
        new("+55", "11", number, type, isFavorite);

    private sealed class FakeContactRepository : IContactRepository
    {
        private readonly List<Contact> _contacts = [];

        public IReadOnlyCollection<Contact> Contacts => _contacts;

        public Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_contacts.FirstOrDefault(contact => contact.Id == id));
        }

        public Task<bool> EmailExistsAsync(string email, Guid? excludingContactId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_contacts.Any(contact =>
                contact.Email == email &&
                (!excludingContactId.HasValue || contact.Id != excludingContactId.Value)));
        }

        public Task AddAsync(Contact contact, CancellationToken cancellationToken)
        {
            _contacts.Add(contact);
            return Task.CompletedTask;
        }

        public void Delete(Contact contact)
        {
            _contacts.Remove(contact);
        }
    }

    private sealed class FakeUnitOfWork : IPhonebookUnitOfWork
    {
        public bool Saved { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeContactReadService(
        ContactEditDto? contact,
        PagedResult<ContactListItemDto> page)
        : IContactReadService
    {
        public Guid? RequestedContactId { get; private set; }

        public ListContactsQuery? RequestedListQuery { get; private set; }

        public Task<ContactEditDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken)
        {
            RequestedContactId = id;
            return Task.FromResult(contact);
        }

        public Task<PagedResult<ContactListItemDto>> ListAsync(ListContactsQuery query, CancellationToken cancellationToken)
        {
            RequestedListQuery = query;
            return Task.FromResult(page);
        }
    }
}
