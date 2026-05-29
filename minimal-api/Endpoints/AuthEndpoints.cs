using ParkingControl.Services;

namespace ParkingControl.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest req, AuthService auth) =>
        {
            var token = await auth.LoginAsync(req.Email, req.Password);
            return token is null
                ? Results.Unauthorized()
                : Results.Ok(new { token });
        });

        app.MapPost("/api/auth/register", async (RegisterRequest req, AuthService auth) =>
        {
            try
            {
                var user = await auth.CreateUserAsync(req.Name, req.Email, req.Password, req.Role ?? "Operator");
                return Results.Ok(new { user.Id, user.Name, user.Email, user.Role });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization("AdminOnly");
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Name, string Email, string Password, string? Role);
