var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapPost("/login", (LoginDTO loginDTO) => {
    if(loginDTO.Email =="adm@teste.com" && loginDTO.Senha == "Adm@1234")
    {
        return Results.Ok("Login com sucesso");
    }
    else Results.Unauthorized();
});
public class LoginDTO
{
    public string Email {get; set} = default;
    public string Senha {get; set} = default;

}
app.Run();
