using AlbertoBizzini.Shared;
using Microsoft.AspNetCore.Mvc;
using WebApi;

namespace AlbertoBizzini.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ContactController : ControllerBase
{
    [HttpPost]
    public ContactResponse Post([FromBody] ContactRequest request)
    {
        return new ContactResponse { Success = true, ErrorCode = request.Email };
    }
}