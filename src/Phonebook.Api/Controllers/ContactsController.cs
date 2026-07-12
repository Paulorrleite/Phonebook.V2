using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Phonebook.Application.Contacts;
using Phonebook.Domain.Contacts;

namespace Phonebook.Api.Controllers;

[ApiController]
[Route("api/contacts")]
public sealed class ContactsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ContactListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(
            new ListContactsQuery(search, page, pageSize),
            static result => new OkObjectResult(result),
            cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ContactEditDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(
            new GetContactForEditQuery(id),
            result => result is null ? NotFound() : Ok(result),
            cancellationToken).ConfigureAwait(false);
    }

    [HttpPost]
    [ProducesResponseType<CreateContactResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] ContactRequest request,
        CancellationToken cancellationToken)
    {
        return await SendAsync(
            request.ToCreateCommand(),
            id => CreatedAtAction(nameof(Get), new { id }, new CreateContactResponse(id)),
            cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ContactRequest request,
        CancellationToken cancellationToken)
    {
        return await SendAsync(
            request.ToUpdateCommand(id),
            static () => new NoContentResult(),
            cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await SendAsync(
            new DeleteContactCommand(id),
            static () => new NoContentResult(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<IActionResult> SendAsync<TResponse>(
        IRequest<TResponse> request,
        Func<TResponse, IActionResult> onSuccess,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await sender.Send(request, cancellationToken).ConfigureAwait(false);
            return onSuccess(response);
        }
        catch (ValidationException exception)
        {
            return ToValidationProblem(exception);
        }
        catch (DomainException exception)
        {
            return ToValidationProblem(new Dictionary<string, string[]> { ["contact"] = [exception.Message] });
        }
        catch (ContactNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<IActionResult> SendAsync(
        IRequest request,
        Func<IActionResult> onSuccess,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(request, cancellationToken).ConfigureAwait(false);
            return onSuccess();
        }
        catch (ValidationException exception)
        {
            return ToValidationProblem(exception);
        }
        catch (DomainException exception)
        {
            return ToValidationProblem(new Dictionary<string, string[]> { ["contact"] = [exception.Message] });
        }
        catch (ContactNotFoundException)
        {
            return NotFound();
        }
    }

    private IActionResult ToValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors.Any()
            ? exception.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => string.IsNullOrWhiteSpace(group.Key) ? "request" : group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray())
            : new Dictionary<string, string[]> { ["request"] = [exception.Message] };

        return ToValidationProblem(errors);
    }

    private BadRequestObjectResult ToValidationProblem(IDictionary<string, string[]> errors)
    {
        return BadRequest(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest
        });
    }
}
