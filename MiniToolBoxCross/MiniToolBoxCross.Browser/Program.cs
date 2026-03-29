using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using MiniToolBoxCross;
using Serilog;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            Log.Information("Starting MiniToolBoxCross Browser application");
            await BuildAvaloniaApp().WithInterFont().StartBrowserAppAsync("out");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
