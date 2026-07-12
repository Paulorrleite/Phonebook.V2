using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Phonebook.Application.Contacts;
using Phonebook.Domain.Contacts;
using Xunit;

namespace Phonebook.IntegrationTests;

public sealed class ContactReadServiceTests : IntegrationTestBase
{
    [Fact]
    public async Task GetContactForEditQuery_returns_all_editable_fields()
    {
        var sender = Services.GetRequiredService<ISender>();
        var id = await sender.Send(new CreateContactCommand(
            "Grace",
            "Hopper",
            "grace@example.com",
            [new PhoneNumberInputDto("+1", "212", "555-0101", PhoneType.Mobile, true)],
            new AddressInputDto("United States", "NY", "New York", "Manhattan", "10001"),
            new PhotoInputDto([1], "image/png", "grace.png", 1)));

        var contact = await sender.Send(new GetContactForEditQuery(id));

        Assert.NotNull(contact);
        Assert.Equal("Grace", contact.FirstName);
        Assert.Equal("Hopper", contact.LastName);
        Assert.Equal("grace@example.com", contact.Email);
        Assert.Single(contact.PhoneNumbers);
        Assert.NotNull(contact.Address);
        Assert.NotNull(contact.Photo);
    }

    [Fact]
    public async Task ListContactsQuery_returns_empty_page_when_no_contacts_match()
    {
        var sender = Services.GetRequiredService<ISender>();

        var result = await sender.Send(new ListContactsQuery("missing", 1, 20));

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task ListContactsQuery_searches_text_case_insensitively_and_phone_digits()
    {
        var sender = Services.GetRequiredService<ISender>();
        await sender.Send(new CreateContactCommand(
            "Alan",
            "Turing",
            "alan@example.com",
            [new PhoneNumberInputDto("+55", "11", "(90000) 1234", PhoneType.Mobile, false)],
            null,
            null));

        var byName = await sender.Send(new ListContactsQuery("turING", 1, 20));
        var byEmail = await sender.Send(new ListContactsQuery("ALAN@EXAMPLE.COM", 1, 20));
        var byPhone = await sender.Send(new ListContactsQuery("55 11 90000", 1, 20));

        Assert.Single(byName.Items);
        Assert.Single(byEmail.Items);
        Assert.Single(byPhone.Items);
    }

    [Fact]
    public async Task ListContactsQuery_paginates_results()
    {
        var sender = Services.GetRequiredService<ISender>();
        await sender.Send(CreateCommand("First", "first@example.com"));
        await sender.Send(CreateCommand("Second", "second@example.com"));
        await sender.Send(CreateCommand("Third", "third@example.com"));

        var page = await sender.Send(new ListContactsQuery(null, 2, 2));

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Single(page.Items);
        Assert.Equal("Third", page.Items.Single().Name);
    }

    [Fact]
    public async Task ListContactsQuery_uses_favorite_phone_before_type_priority()
    {
        var sender = Services.GetRequiredService<ISender>();
        await sender.Send(new CreateContactCommand(
            "Favorite",
            null,
            "favorite@example.com",
            [
                new PhoneNumberInputDto("+55", "11", "1111-1111", PhoneType.Mobile, false),
                new PhoneNumberInputDto("+55", "11", "2222-2222", PhoneType.Commercial, true)
            ],
            null,
            null));

        var result = await sender.Send(new ListContactsQuery(null, 1, 20));

        var item = Assert.Single(result.Items);
        Assert.Equal(PhoneType.Commercial, item.Phone.Type);
        Assert.True(item.Phone.IsFavorite);
    }

    [Fact]
    public async Task ListContactsQuery_uses_phone_type_priority_when_no_favorite_exists()
    {
        var sender = Services.GetRequiredService<ISender>();
        await sender.Send(new CreateContactCommand(
            "Priority",
            null,
            "priority@example.com",
            [
                new PhoneNumberInputDto("+55", "11", "3333-3333", PhoneType.Commercial, false),
                new PhoneNumberInputDto("+55", "11", "4444-4444", PhoneType.Residential, false),
                new PhoneNumberInputDto("+55", "11", "5555-5555", PhoneType.Mobile, false)
            ],
            null,
            null));

        var result = await sender.Send(new ListContactsQuery(null, 1, 20));

        var item = Assert.Single(result.Items);
        Assert.Equal(PhoneType.Mobile, item.Phone.Type);
    }

    private static CreateContactCommand CreateCommand(string firstName, string email)
    {
        return new CreateContactCommand(
            firstName,
            null,
            email,
            [new PhoneNumberInputDto("+55", "11", $"90000-{firstName.Length:0000}", PhoneType.Mobile, false)],
            null,
            null);
    }
}
