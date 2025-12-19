var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // This enables Swagger generation

var app = builder.Build();

// 2. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // This enables the interactive UI at /swagger
}

app.UseDefaultFiles(); // Important: Looks for login.html or index.html
app.UseStaticFiles();  // Important: Serves files from wwwroot

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();