using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Toolkit.Uwp.Notifications; // ДОДАНО ОФІЦІЙНУ БІБЛІОТЕКУ

namespace Tracker.DesktopAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();

    private readonly Dictionary<string, string> _categories = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Code", "Productive" }, { "Figma", "Productive" }, { "devenv", "Productive" },
        { "chrome", "Distracting" }, { "msedge", "Distracting" }, { "Telegram", "Distracting" }, { "Discord", "Distracting" }
    };

    private readonly string[] _motivationalQuotes = new[]
    {
        "Твої цілі самі себе не досягнуть. Закривай вікно і до роботи!",
        "Дисципліна — це вибір між тим, що хочеться зараз, і тим, що справді важливо.",
        "Різниця між тобою і тими, ким ти захоплюєшся — у фокусі.",
        "Час — твій найцінніший ресурс. Не витрачай його на пустий скролінг.",
        "Дофамінова пастка закривається. Повертайся до реальності!"
    };

    private record ActivityPayload(string ActivityName, string Category, DateTime StartTime, int DurationSec);

    public Worker(ILogger<Worker> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string currentProcess = string.Empty;
        DateTime startTime = DateTime.UtcNow;
        var batch = new List<ActivityPayload>();

        while (!stoppingToken.IsCancellationRequested)
        {
            IntPtr handle = Win32Api.GetForegroundWindow();
            if (handle != IntPtr.Zero)
            {
                Win32Api.GetWindowThreadProcessId(handle, out uint processId);
                try 
                {
                    string processName = Process.GetProcessById((int)processId).ProcessName;
                    int idleSeconds = Win32Api.GetIdleTimeSeconds();
                    if (idleSeconds > 60) processName = "Відсутній";

                    if (processName != currentProcess)
                    {
                        if (!string.IsNullOrEmpty(currentProcess))
                        {
                            int duration = (int)(DateTime.UtcNow - startTime).TotalSeconds;
                            if (duration > 0)
                            {
                                string category = currentProcess == "Відсутній" ? "Neutral" : 
                                    (_categories.TryGetValue(currentProcess, out string? mapped) ? mapped : "Neutral");

                                batch.Add(new ActivityPayload(currentProcess, category, startTime, duration));
                            }
                        }
                        currentProcess = processName;
                        startTime = DateTime.UtcNow;
                    }
                }
                catch { }
            }

            if (batch.Count >= 3)
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync("http://localhost:5249/api/telemetry/batch", batch, stoppingToken);
                    if (response.IsSuccessStatusCode)
                    {
                        int distractingSeconds = batch.Where(b => b.Category == "Distracting").Sum(b => b.DurationSec);
                        
                        if (distractingSeconds > 5) 
                        {
                            string quote = _motivationalQuotes[_random.Next(_motivationalQuotes.Length)];
                            ShowWindowsNotification("⚠️ Дофаміновий ліміт", quote);
                        }
                        batch.Clear();
                    }
                }
                catch { }
            }
            await Task.Delay(1000, stoppingToken);
        }
    }

    // НОВИЙ НАТИВНИЙ МЕТОД
    private void ShowWindowsNotification(string title, string message)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Помилка виведення сповіщення: {ex.Message}");
        }
    }
}