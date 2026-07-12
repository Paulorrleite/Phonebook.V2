using Microsoft.EntityFrameworkCore;
using Phonebook.Domain.Contacts;
using Xunit;

namespace Phonebook.IntegrationTests;

public sealed class PhonebookPersistenceTests : IntegrationTestBase
{
    [Fact]
    public async Task Migration_creates_expected_tables_and_indexes()
    {
        var appliedMigrations = await DbContext.Database.GetAppliedMigrationsAsync();

        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
        Assert.False(DbContext.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Email_unique_index_allows_multiple_nulls_but_rejects_duplicate_non_null_email()
    {
        DbContext.Contacts.Add(CreateContact("No", null));
        DbContext.Contacts.Add(CreateContact("AlsoNo", null));
        await DbContext.SaveChangesAsync();

        DbContext.Contacts.Add(CreateContact("One", "same@example.com"));
        await DbContext.SaveChangesAsync();

        DbContext.Contacts.Add(CreateContact("Two", "same@example.com"));
        await Assert.ThrowsAsync<DbUpdateException>(() => DbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Phone_unique_index_rejects_duplicate_normalized_number_for_same_contact()
    {
        var contact = CreateContact("Owner", "owner@example.com");
        DbContext.Contacts.Add(contact);
        await DbContext.SaveChangesAsync();

        var duplicatePhone = new PhoneNumber("+55", "11", "9999-0000", PhoneType.Residential);
        DbContext.PhoneNumbers.Add(duplicatePhone);
        DbContext.Entry(duplicatePhone).Property("contact_id").CurrentValue = contact.Id;

        await Assert.ThrowsAsync<DbUpdateException>(() => DbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_contact_cascades_to_phone_address_and_photo()
    {
        var contact = new Contact(
            "Cascade",
            "Target",
            "cascade@example.com",
            [new PhoneNumber("+55", "11", "90000-0000", PhoneType.Mobile)],
            new Address("Brazil", "SP", "Sao Paulo", "Centro", "01000-000"),
            new ContactPhoto([1, 2, 3], "image/png", "avatar.png", 3));

        DbContext.Contacts.Add(contact);
        await DbContext.SaveChangesAsync();

        DbContext.Contacts.Remove(contact);
        await DbContext.SaveChangesAsync();

        Assert.Empty(await DbContext.Contacts.ToListAsync());
        Assert.Empty(await DbContext.PhoneNumbers.ToListAsync());
        Assert.Empty(await DbContext.Addresses.ToListAsync());
        Assert.Empty(await DbContext.ContactPhotos.ToListAsync());
    }

    private static Contact CreateContact(string firstName, string? email)
    {
        return new Contact(
            firstName,
            null,
            email,
            [new PhoneNumber("+55", "11", "9999-0000", PhoneType.Mobile)]);
    }
}
