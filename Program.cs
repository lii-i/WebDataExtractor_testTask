using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IValidator<ExtractRequestDTO>, ExtractionRequestDtoValidator>();
builder.Services.AddScoped<DataExtractionService>(sp => {
    string constr = builder.Configuration["ConnectionString"];
    var validator = sp.GetRequiredService<IValidator<ExtractRequestDTO>>();
    return new DataExtractionService(validator, constr);
});

var app = builder.Build();

app.UseSwagger(c => {
    c.RouteTemplate = "api/swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "WebDataExtractor API v1");
    c.RoutePrefix = "api/swagger";
});

app.EndPoints();

app.Run();
