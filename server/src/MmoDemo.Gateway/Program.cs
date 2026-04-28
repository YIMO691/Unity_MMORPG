var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "OK",
    Service = "MmoDemo.Gateway",
    Phase = "Phase 1 Task 1"
}));

app.Run();

public partial class Program;
