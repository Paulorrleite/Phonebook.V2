using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phonebook.Application.Contacts;
using Phonebook.Domain.Contacts;
using Xunit;

namespace Phonebook.IntegrationTests;

public sealed class ContactHandlerTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateContactCommand_persists_contact_and_children()
    {
        var sender = Services.GetRequiredService<ISender>();
        var command = new CreateContactCommand(
            "Ada",
            "Lovelace",
            "ada@example.com",
            [new PhoneNumberInputDto("+55", "11", "98888-7777", PhoneType.Mobile, true)],
            new AddressInputDto("Brazil", "SP", "Sao Paulo", "Centro", "01000-000"),
            new PhotoInputDto([1, 2, 3], "image/png", "ada.png", 3));

        var id = await sender.Send(command);

        var contact = await DbContext.Contacts
            .Include(entity => entity.PhoneNumbers)
            .Include(entity => entity.Address)
            .Include(entity => entity.Photo)
            .SingleAsync(entity => entity.Id == id);
        Assert.Equal("Ada", contact.FirstName);
        Assert.Single(contact.PhoneNumbers);
        Assert.NotNull(contact.Address);
        Assert.NotNull(contact.Photo);
    }

    [Fact]
    public async Task CreateContactCommand_rejects_duplicate_non_null_email_before_persistence()
    {
        var sender = Services.GetRequiredService<ISender>();
        var command = CreateCommand("first@example.com", "First");
        await sender.Send(command);

        var duplicate = CreateCommand("first@example.com", "Second");

        await Assert.ThrowsAsync<ValidationException>(() => sender.Send(duplicate));
    }

    [Fact]
    public async Task UpdateContactCommand_replaces_editable_state_and_removes_photo_when_requested()
    {
        var sender = Services.GetRequiredService<ISender>();
        var id = await sender.Send(new CreateContactCommand(
            "Before",
            "Person",
            "before@example.com",
            [new PhoneNumberInputDto("+55", "11", "91111-1111", PhoneType.Mobile, true)],
            new AddressInputDto("Brazil", "SP", "Sao Paulo", "Centro", "01000-000"),
            new PhotoInputDto([1, 2, 3], "image/png", "before.png", 3)));

        await sender.Send(new UpdateContactCommand(
            id,
            "After",
            null,
            "after@example.com",
            [new PhoneNumberInputDto("+1", "212", "555-0100", PhoneType.Commercial, false)],
            null,
            null,
            true));

        var contact = await DbContext.Contacts
            .AsNoTracking()
            .Include(entity => entity.PhoneNumbers)
            .Include(entity => entity.Address)
            .Include(entity => entity.Photo)
            .SingleAsync(entity => entity.Id == id);

        Assert.Equal("After", contact.FirstName);
        Assert.Null(contact.LastName);
        Assert.Equal("after@example.com", contact.Email);
        Assert.Single(contact.PhoneNumbers);
        Assert.Null(contact.Address);
        Assert.Null(contact.Photo);
    }

    [Fact]
    public async Task UpdateContactCommand_preserves_photo_when_no_photo_change_is_requested()
    {
        var sender = Services.GetRequiredService<ISender>();
        var id = await sender.Send(new CreateContactCommand(
            "Before",
            "Person",
            "before.keep@example.com",
            [new PhoneNumberInputDto("+55", "11", "91111-1111", PhoneType.Mobile, true)],
            null,
            new PhotoInputDto([1, 2, 3], "image/png", "before.png", 3)));

        await sender.Send(new UpdateContactCommand(
            id,
            "After",
            null,
            "after.keep@example.com",
            [new PhoneNumberInputDto("+1", "212", "555-0100", PhoneType.Commercial, false)],
            null,
            null,
            false));

        var contact = await DbContext.Contacts
            .AsNoTracking()
            .Include(entity => entity.Photo)
            .SingleAsync(entity => entity.Id == id);

        Assert.NotNull(contact.Photo);
        Assert.Equal("before.png", contact.Photo.FileName);
        Assert.Equal("image/png", contact.Photo.ContentType);
        Assert.Equal(3, contact.Photo.Size);
    }

    [Fact]
    public async Task UpdateContactCommand_rejects_missing_contact()
    {
        var sender = Services.GetRequiredService<ISender>();
        var command = new UpdateContactCommand(
            Guid.NewGuid(),
            "Missing",
            null,
            null,
            [new PhoneNumberInputDto("+55", "11", "90000-0000", PhoneType.Mobile, false)],
            null,
            null,
            false);

        await Assert.ThrowsAsync<ContactNotFoundException>(() => sender.Send(command));
    }

    [Fact]
    public async Task DeleteContactCommand_hard_deletes_contact()
    {
        var sender = Services.GetRequiredService<ISender>();
        var id = await sender.Send(CreateCommand("delete@example.com", "Delete"));

        await sender.Send(new DeleteContactCommand(id));

        Assert.False(await DbContext.Contacts.AnyAsync(entity => entity.Id == id));
    }

    private static CreateContactCommand CreateCommand(string email, string firstName)
    {
        return new CreateContactCommand(
            firstName,
            null,
            email,
            [new PhoneNumberInputDto("+55", "11", "90000-0000", PhoneType.Mobile, false)],
            null,
            null);
    }
}
