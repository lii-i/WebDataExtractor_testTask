var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<IValidator<ExtractionRequestDto>, ExtractionRequestDtoValidator>();
builder.Services.AddScopedService<DataExtractionService>(sp => {
    string constr = bulder.Configuration["ConnectionString"];
    var validator = sc.GetRequiredService<IValidator<ExtractRequestDTO>>();

    return new DataExtractionService(validator, constr);
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();



app.Run();
