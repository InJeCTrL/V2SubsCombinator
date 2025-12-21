using Microsoft.AspNetCore.Mvc;
using V2SubsCombinator.DTOs;

namespace V2SubsCombinator.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    [HttpPost(Name = "Login")]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginRequest loginRequest)
    {
        // Placeholder implementation
        var result = new LoginResult
        {
            Success = loginRequest.Username == "admin" && loginRequest.Password == "password",
            // Token = "dummy-token",
            // ExpireAt = DateTime.UtcNow.AddHours(1)
        };

        return Ok(result);
    }
}
