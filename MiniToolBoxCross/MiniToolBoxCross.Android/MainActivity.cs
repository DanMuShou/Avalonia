using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Serilog;

namespace MiniToolBoxCross.Android;

[Activity(
    Label = "MiniToolBoxCross.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation
        | ConfigChanges.ScreenSize
        | ConfigChanges.UiMode
)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder).WithInterFont();
    }

    protected override void OnResume()
    {
        base.OnResume();
        Log.Information("Android application resumed");
    }

    protected override void OnPause()
    {
        base.OnPause();
        Log.Information("Android application paused");
    }
}
