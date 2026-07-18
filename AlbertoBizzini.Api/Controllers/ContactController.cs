using AlbertoBizzini.Api;
using AlbertoBizzini.Services;
using AlbertoBizzini.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AlbertoBizzini.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ContactController : ControllerBase
{
    private readonly IContactService _contactService;

    public ContactController(IContactService contactService)
    {
        _contactService = contactService;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimiterPolicies.Contact)]
    public async Task<ActionResult<ContactResponse>> Send(
        ContactRequest request,
        CancellationToken cancellationToken)
    {
        // "Website" is a HoneyPot
        if (!string.IsNullOrWhiteSpace(request.Website))
            return Ok(new ContactResponse { Success = true });

        var context = new ContactContext
        {
            IpAddress = Request.HttpContext.GetClientIp(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        var response = await _contactService.SendAsync(
            request,
            context,
            cancellationToken);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}