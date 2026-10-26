using Tracker.DesktopAgent;

var builder = Host.CreateApplicationBuilder(args);

// Реєструємо HttpClient із базовою адресою нашого API (заміни порт на свій, якщо він інший)
builder.Services.AddHttpClient<Worker>(client => 
{
    client.BaseAddress = new Uri("http://localhost:5249"); // Адреса локального бекенду
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
