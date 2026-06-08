using PerformanceInsight.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<PerformanceAnalysisService>();
builder.Services.AddScoped<PerformanceDashboardPageService>();
builder.Services.AddScoped<JMeterResultParserService>();
builder.Services.AddSingleton<PerformanceReportStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
