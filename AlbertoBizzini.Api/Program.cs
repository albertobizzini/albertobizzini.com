using AlbertoBizzini.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));

builder.Services.Configure<TurnstileOptions>(
    builder.Configuration.GetSection(TurnstileOptions.SectionName));

builder.Services.AddTransient<IContactService, ContactService>();
builder.Services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>();
builder.Services.AddTransient<IEmailSender, Smtp2GoEmailSender>();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
