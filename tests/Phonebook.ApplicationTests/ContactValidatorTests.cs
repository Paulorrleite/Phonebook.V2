using FluentValidation;
using Phonebook.Application.Contacts;
using Phonebook.Application.Contacts.Validation;
using Phonebook.Domain.Contacts;
using Xunit;

namespace Phonebook.ApplicationTests;

public sealed class ContactValidatorTests
{
    private readonly CreateContactCommandValidator _createValidator = new();
    private readonly UpdateContactCommandValidator _updateValidator = new();
    private readonly ListContactsQueryValidator _listValidator = new();

    [Fact]
    public void CreateContactCommand_WithValidRequiredFields_IsValid()
    {
        var result = _createValidator.Validate(ValidCreateCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateContactCommand_WithoutFirstName_IsInvalid()
    {
        var command = ValidCreateCommand() with { FirstName = "" };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "FirstName");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    public void CreateContactCommand_WithInvalidEmail_IsInvalid(string email)
    {
        var command = ValidCreateCommand() with { Email = email };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Email");
    }

    [Fact]
    public void CreateContactCommand_WithNullEmail_IsValid()
    {
        var command = ValidCreateCommand() with { Email = null };

        var result = _createValidator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateContactCommand_WithoutPhones_IsInvalid()
    {
        var command = ValidCreateCommand() with { PhoneNumbers = [] };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "PhoneNumbers");
    }

    [Fact]
    public void CreateContactCommand_WithNullPhones_IsInvalid()
    {
        var command = ValidCreateCommand() with { PhoneNumbers = null! };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "PhoneNumbers");
    }

    [Fact]
    public void CreateContactCommand_WithMoreThanTenPhones_IsInvalid()
    {
        var phones = Enumerable
            .Range(1, 11)
            .Select(index => Phone(number: $"99999{index:0000}"))
            .ToArray();
        var command = ValidCreateCommand() with { PhoneNumbers = phones };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "PhoneNumbers");
    }

    [Fact]
    public void CreateContactCommand_WithDuplicatePhones_IsInvalid()
    {
        var command = ValidCreateCommand() with
        {
            PhoneNumbers = [Phone(), Phone()]
        };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.ErrorMessage == "Phone numbers must be unique within a contact.");
    }

    [Fact]
    public void CreateContactCommand_WithMultipleFavoritePhones_IsInvalid()
    {
        var command = ValidCreateCommand() with
        {
            PhoneNumbers = [Phone(isFavorite: true), Phone(number: "888887777", isFavorite: true)]
        };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.ErrorMessage == "Only one phone number can be favorite.");
    }

    [Fact]
    public void CreateContactCommand_WithInvalidPhoneType_IsInvalid()
    {
        var command = ValidCreateCommand() with
        {
            PhoneNumbers = [Phone(type: (PhoneType)999)]
        };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName.EndsWith(".Type", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateContactCommand_WithInvalidCountryCode_IsInvalid()
    {
        var command = ValidCreateCommand() with
        {
            PhoneNumbers = [Phone(countryCode: "Brazil")]
        };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName.EndsWith(".CountryCode", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateContactCommand_WithPartialAddress_IsInvalid()
    {
        var command = ValidCreateCommand() with
        {
            Address = new AddressInputDto("Brazil", "SP", null, "Centro", "01001-000")
        };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Address.City");
    }

    [Fact]
    public void CreateContactCommand_WithOversizedPhoto_IsInvalid()
    {
        var size = ContactPhoto.MaxSizeInBytes + 1;
        var command = ValidCreateCommand() with
        {
            Photo = new PhotoInputDto(new byte[size], "image/png", "large.png", size)
        };

        var result = _createValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Photo.Size");
    }

    [Fact]
    public void UpdateContactCommand_WithoutId_IsInvalid()
    {
        var command = new UpdateContactCommand(
            Guid.Empty,
            "Ada",
            null,
            null,
            [Phone()],
            null,
            null,
            false);

        var result = _updateValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Id");
    }

    [Fact]
    public void UpdateContactCommand_WithPhotoAndRemovePhoto_IsInvalid()
    {
        var command = new UpdateContactCommand(
            Guid.NewGuid(),
            "Ada",
            null,
            null,
            [Phone()],
            null,
            new PhotoInputDto([1], "image/png", "ada.png", 1),
            true);

        var result = _updateValidator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.ErrorMessage == "Cannot upload and remove a photo in the same request.");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void ListContactsQuery_WithInvalidPagination_IsInvalid(int page, int pageSize)
    {
        var result = _listValidator.Validate(new ListContactsQuery(null, page, pageSize));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ListContactsQuery_WithLongSearch_IsInvalid()
    {
        var result = _listValidator.Validate(new ListContactsQuery(new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Search");
    }

    [Fact]
    public void ListContactsQuery_WithDefaults_IsValid()
    {
        var result = _listValidator.Validate(new ListContactsQuery(null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Brazil", "01001-000")]
    [InlineData("BR", "01001000")]
    [InlineData("United States", "12345")]
    [InlineData("USA", "12345-6789")]
    [InlineData("Canada", "A1A 1A1")]
    public void PostalCodeValidator_WithValidPostalCode_ReturnsTrue(string country, string postalCode)
    {
        var validator = new PostalCodeValidator();

        Assert.True(validator.IsValid(country, postalCode));
    }

    [Theory]
    [InlineData("Brazil", "1234")]
    [InlineData("United States", "1234")]
    [InlineData("Canada", " ")]
    public void PostalCodeValidator_WithInvalidPostalCode_ReturnsFalse(string country, string postalCode)
    {
        var validator = new PostalCodeValidator();

        Assert.False(validator.IsValid(country, postalCode));
    }

    private static CreateContactCommand ValidCreateCommand() =>
        new(
            "Ada",
            null,
            "ada@example.com",
            [Phone()],
            null,
            null);

    private static PhoneNumberInputDto Phone(
        string countryCode = "+55",
        string areaCode = "11",
        string number = "999991111",
        PhoneType type = PhoneType.Mobile,
        bool isFavorite = false) =>
        new(countryCode, areaCode, number, type, isFavorite);
}
