using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Phonebook.Api.Controllers;
using Phonebook.Application.Contacts;
using Phonebook.Domain.Contacts;
using Xunit;

namespace Phonebook.IntegrationTests;

public sealed class ContactEndpointTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Create_returns_created_and_location()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/contacts", CreateRequest()).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<CreateContactResponse>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Contains($"/api/contacts/{body.Id}", response.Headers.Location!.OriginalString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_returns_bad_request_for_validation_errors()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var request = CreateRequest(firstName: string.Empty);

        var response = await client.PostAsJsonAsync("/api/contacts", request).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(problem);
        Assert.Contains(nameof(ContactRequest.FirstName), problem.Errors.Keys);
    }

    [Fact]
    public async Task List_returns_default_paged_contacts()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await CreateContactAsync(client, CreateRequest(firstName: "Ada", lastName: "Lovelace", email: "ada@example.test")).ConfigureAwait(false);

        var response = await client.GetAsync("/api/contacts").ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<ContactListItemDto>>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
        Assert.Equal(1, body.TotalCount);
        Assert.Contains(body.Items, contact => contact.Name == "Ada Lovelace" && contact.Email == "ada@example.test");
    }

    [Fact]
    public async Task List_returns_bad_request_for_invalid_pagination()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/contacts?page=0&pageSize=101").ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_returns_contact_for_edit()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var id = await CreateContactAsync(client, CreateRequest(firstName: "Grace", lastName: "Hopper")).ConfigureAwait(false);

        var response = await client.GetAsync($"/api/contacts/{id}").ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ContactEditDto>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(body);
        Assert.Equal(id, body.Id);
        Assert.Equal("Grace", body.FirstName);
        Assert.Single(body.PhoneNumbers);
    }

    [Fact]
    public async Task Get_returns_not_found_for_missing_contact()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/contacts/{Guid.NewGuid()}").ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_returns_no_content_and_persists_changes()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var id = await CreateContactAsync(client, CreateRequest(firstName: "Alan", email: "alan@example.test")).ConfigureAwait(false);
        var update = CreateRequest(firstName: "Alan", lastName: "Turing", email: "alan.turing@example.test");

        var response = await client.PutAsJsonAsync($"/api/contacts/{id}", update).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = await client.GetFromJsonAsync<ContactEditDto>($"/api/contacts/{id}", JsonOptions).ConfigureAwait(false);
        Assert.NotNull(saved);
        Assert.Equal("Turing", saved.LastName);
        Assert.Equal("alan.turing@example.test", saved.Email);
    }

    [Fact]
    public async Task Update_preserves_existing_photo_when_remove_photo_is_false()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var photo = new PhotoRequest([1, 2], "image/png", "alan.png", 2);
        var id = await CreateContactAsync(client, CreateRequest(firstName: "Alan", email: "alan.photo@example.test", photo: photo)).ConfigureAwait(false);
        var update = CreateRequest(firstName: "Alan", lastName: "Turing", email: "alan.photo.updated@example.test");

        var response = await client.PutAsJsonAsync($"/api/contacts/{id}", update).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = await client.GetFromJsonAsync<ContactEditDto>($"/api/contacts/{id}", JsonOptions).ConfigureAwait(false);
        Assert.NotNull(saved?.Photo);
        Assert.Equal("alan.png", saved.Photo.FileName);
        Assert.Equal("image/png", saved.Photo.ContentType);
        Assert.Equal(2, saved.Photo.Size);
    }

    [Fact]
    public async Task Update_removes_existing_photo_when_requested()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var photo = new PhotoRequest([1, 2], "image/png", "remove.png", 2);
        var id = await CreateContactAsync(client, CreateRequest(firstName: "Remove", email: "remove.photo@example.test", photo: photo)).ConfigureAwait(false);
        var update = CreateRequest(firstName: "Remove", email: "remove.photo.updated@example.test", removePhoto: true);

        var response = await client.PutAsJsonAsync($"/api/contacts/{id}", update).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var saved = await client.GetFromJsonAsync<ContactEditDto>($"/api/contacts/{id}", JsonOptions).ConfigureAwait(false);
        Assert.NotNull(saved);
        Assert.Null(saved.Photo);
    }

    [Fact]
    public async Task Update_returns_not_found_for_missing_contact()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/contacts/{Guid.NewGuid()}", CreateRequest()).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_returns_no_content_and_removes_contact()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var id = await CreateContactAsync(client, CreateRequest(firstName: "Katherine")).ConfigureAwait(false);

        var response = await client.DeleteAsync($"/api/contacts/{id}").ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/contacts/{id}").ConfigureAwait(false)).StatusCode);
    }

    [Fact]
    public async Task Delete_returns_not_found_for_missing_contact()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/contacts/{Guid.NewGuid()}").ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var connectionString = Environment.GetEnvironmentVariable("PHONEBOOK_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("PHONEBOOK_TEST_CONNECTION_STRING is required for API integration tests.");
        Environment.SetEnvironmentVariable("PHONEBOOK_CONNECTION_STRING", connectionString);

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Phonebook"] = connectionString
                    });
                });
            });
    }

    private static async Task<Guid> CreateContactAsync(HttpClient client, ContactRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/contacts", request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CreateContactResponse>(JsonOptions).ConfigureAwait(false);
        return body?.Id ?? throw new InvalidOperationException("Create contact response did not include an ID.");
    }

    private static ContactRequest CreateRequest(
        string firstName = "Marie",
        string? lastName = null,
        string? email = null,
        PhotoRequest? photo = null,
        bool removePhoto = false)
    {
        return new ContactRequest(
            firstName,
            lastName,
            email,
            [
                new PhoneNumberRequest(
                    "+55",
                    "11",
                    "99999-0000",
                    PhoneType.Mobile,
                    true)
            ],
            null,
            photo,
            removePhoto);
    }
}
