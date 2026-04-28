namespace NICE.Platform.Collaboration.API.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NICE.Platform.Collaboration.Contracts.Requests;

[ApiController]
[Route("api/applications")]
public class ApplicationsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;
    
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterApplicationRequest request, CancellationToken ct)
    {
        // TODO: map to RegisterApplicationCommand and dispatch
        throw new NotImplementedException();
    }
}
