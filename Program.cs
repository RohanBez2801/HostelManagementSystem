using HostelManagementSystem.Helpers;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Encoding Provider for Access DB (Vital for OLEDB)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var app = builder.Build();

// 2. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles(); // Serves index.html by default
app.UseStaticFiles();  // Serves wwwroot content

app.MapControllers();

// 3. Initialize Database (Run the "Doctor")
DbHelper.InitializeDatabase();

app.Run();