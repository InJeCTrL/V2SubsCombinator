using Microsoft.AspNetCore.Mvc;
using V2SubsCombinator.DTOs;
using V2SubsCombinator.IServices;

namespace V2SubsCombinator.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class UserController(IAuthentication authenticationService) : ControllerBase
{
    private readonly IAuthentication _authenticationService = authenticationService;

    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginRequest request)
    {
        var result = await _authenticationService.AuthenticateAsync(request);
        
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResult>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authenticationService.RegisterAsync(request);
        
        return Ok(result);
    }
}
