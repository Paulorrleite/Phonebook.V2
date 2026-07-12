using Phonebook.Domain.Contacts;
using Xunit;

namespace Phonebook.DomainTests;

public sealed class ContactTests
{
    [Fact]
    public void Constructor_WithValidRequiredData_CreatesContactWithoutOptionalFields()
    {
        var contact = new Contact("Ada", null, null, [Mobile()]);

        Assert.Equal("Ada", contact.FirstName);
        Assert.Null(contact.LastName);
        Assert.Null(contact.Email);
        Assert.Null(contact.Address);
        Assert.Null(contact.Photo);
        Assert.Single(contact.PhoneNumbers);
    }

    [Fact]
    public void Constructor_WithoutFirstName_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => new Contact(" ", null, null, [Mobile()]));

        Assert.Equal("First name is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithoutPhones_Throws()
    {
        var exception = Assert.Throws<DomainException>(() => new Contact("Ada", null, null, []));

        Assert.Equal("A contact must have between 1 and 10 phone numbers.", exception.Message);
    }

    [Fact]
    public void Constructor_WithMoreThanTenPhones_Throws()
    {
        var phones = Enumerable
            .Range(1, 11)
            .Select(index => Mobile(number: $"99999{index:0000}"));

        var exception = Assert.Throws<DomainException>(() => new Contact("Ada", null, null, phones));

        Assert.Equal("A contact must have between 1 and 10 phone numbers.", exception.Message);
    }

    [Fact]
    public void Constructor_WithDuplicatePhoneNumbers_Throws()
    {
        var phones = new[]
        {
            Mobile(),
            Mobile()
        };

        var exception = Assert.Throws<DomainException>(() => new Contact("Ada", null, null, phones));

        Assert.Equal("Phone numbers must be unique within a contact.", exception.Message);
    }

    [Fact]
    public void Constructor_WithMultipleFavoritePhones_Throws()
    {
        var phones = new[]
        {
            Mobile(number: "999991111", isFavorite: true),
            Residential(number: "33334444", isFavorite: true)
        };

        var exception = Assert.Throws<DomainException>(() => new Contact("Ada", null, null, phones));

        Assert.Equal("Only one phone number can be favorite.", exception.Message);
    }

    [Fact]
    public void DisplayPhone_WhenFavoriteExists_ReturnsFavoritePhone()
    {
        var commercial = Commercial(number: "44445555", isFavorite: true);
        var contact = new Contact("Ada", null, null, [Mobile(), commercial]);

        Assert.Same(commercial, contact.DisplayPhone);
    }

    [Fact]
    public void DisplayPhone_WithoutFavorite_ReturnsPhoneByTypePriority()
    {
        var mobile = Mobile(number: "999991111");
        var contact = new Contact("Ada", null, null, [Commercial(), Residential(), mobile]);

        Assert.Same(mobile, contact.DisplayPhone);
    }

    [Fact]
    public void SetAddress_StoresOptionalAddress()
    {
        var contact = new Contact("Ada", null, null, [Mobile()]);
        var address = new Address("Brazil", "SP", "Sao Paulo", "Centro", "01001-000");

        contact.SetAddress(address);

        Assert.Same(address, contact.Address);
    }

    [Fact]
    public void SetPhoto_StoresOptionalPhoto()
    {
        var contact = new Contact("Ada", null, null, [Mobile()]);
        var content = new byte[] { 1, 2, 3 };
        var photo = new ContactPhoto(content, "image/png", "ada.png", content.Length);

        contact.SetPhoto(photo);

        Assert.Same(photo, contact.Photo);
    }

    [Fact]
    public void ContactPhoto_WhenOversized_Throws()
    {
        var content = new byte[ContactPhoto.MaxSizeInBytes + 1];

        var exception = Assert.Throws<DomainException>(() =>
            new ContactPhoto(content, "image/png", "large.png", content.Length));

        Assert.Equal("Photo size must be between 1 byte and 750 KB.", exception.Message);
    }

    [Fact]
    public void Address_WithPartialData_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new Address("Brazil", "SP", "", "Centro", "01001-000"));

        Assert.Equal("All address fields are required.", exception.Message);
    }

    [Fact]
    public void UpdateDetails_UpdatesNamesAndEmail()
    {
        var contact = new Contact("Ada", null, null, [Mobile()]);

        contact.UpdateDetails("Grace", "Hopper", "grace@example.com");

        Assert.Equal("Grace", contact.FirstName);
        Assert.Equal("Hopper", contact.LastName);
        Assert.Equal("grace@example.com", contact.Email);
    }

    [Fact]
    public void ReplacePhones_WithValidPhones_ReplacesExistingPhones()
    {
        var contact = new Contact("Ada", null, null, [Mobile()]);
        var commercial = Commercial(number: "44445555");

        contact.ReplacePhones([commercial]);

        Assert.Same(commercial, Assert.Single(contact.PhoneNumbers));
    }

    [Fact]
    public void ReplacePhones_WhenRemovingAllPhones_Throws()
    {
        var contact = new Contact("Ada", null, null, [Mobile()]);

        var exception = Assert.Throws<DomainException>(() => contact.ReplacePhones([]));

        Assert.Equal("A contact must have between 1 and 10 phone numbers.", exception.Message);
    }

    [Fact]
    public void RemoveAddress_ClearsAddress()
    {
        var contact = new Contact(
            "Ada",
            null,
            null,
            [Mobile()],
            new Address("Brazil", "SP", "Sao Paulo", "Centro", "01001-000"));

        contact.RemoveAddress();

        Assert.Null(contact.Address);
    }

    [Fact]
    public void RemovePhoto_ClearsPhoto()
    {
        var content = new byte[] { 1, 2, 3 };
        var contact = new Contact(
            "Ada",
            null,
            null,
            [Mobile()],
            photo: new ContactPhoto(content, "image/png", "ada.png", content.Length));

        contact.RemovePhoto();

        Assert.Null(contact.Photo);
    }

    private static PhoneNumber Mobile(string number = "999991111", bool isFavorite = false) =>
        new("+55", "11", number, PhoneType.Mobile, isFavorite);

    private static PhoneNumber Residential(string number = "33334444", bool isFavorite = false) =>
        new("+55", "11", number, PhoneType.Residential, isFavorite);

    private static PhoneNumber Commercial(string number = "44445555", bool isFavorite = false) =>
        new("+55", "11", number, PhoneType.Commercial, isFavorite);
}
