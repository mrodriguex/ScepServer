using Microsoft.EntityFrameworkCore;
using ScepAdmin.Data;
using ScepAdmin.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<ICertificateService, CertificateService>();
builder.Services.AddScoped<IChallengeValidationService, ChallengeValidationService>();
builder.Services.AddScoped<ICertificateIssuanceService, CertificateIssuanceService>();
builder.Services.AddScoped<ICrlService, CrlService>();
builder.Services.AddScoped<IBootstrapService, BootstrapService>();
builder.Services.AddSingleton<IScepRequestDecoder, ScepRequestDecoder>();
builder.Services.AddSingleton<IScepCertificateFactory, ScepCertificateFactory>();
builder.Services.AddSingleton<IScepResponseBuilder, ScepResponseBuilder>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
