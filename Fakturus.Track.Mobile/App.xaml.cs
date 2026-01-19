using Serilog;

namespace Fakturus.Track.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        SetupGlobalExceptionHandling();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Log.Logger.Information("[App] Creating main window");
        return new Window(new MainPage()) { Title = "Fakturus.Track.Mobile" };
    }

    private void SetupGlobalExceptionHandling()
    {
        Log.Logger.Information("[App] Setting up global exception handlers");

        // Handle unhandled exceptions on main thread
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Handle unobserved task exceptions (async/await)
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        Log.Logger.Debug("[App] Global exception handlers registered");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        if (exception != null)
            Log.Logger.Fatal(exception,
                "[App] UNHANDLED EXCEPTION - Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}, IsTerminating: {IsTerminating}",
                exception.GetType().Name,
                exception.Message,
                exception.StackTrace,
                e.IsTerminating);
        else
            Log.Logger.Fatal(
                "[App] UNHANDLED EXCEPTION (non-Exception object) - Object: {ExceptionObject}, Type: {ObjectType}, IsTerminating: {IsTerminating}",
                e.ExceptionObject,
                e.ExceptionObject?.GetType().Name ?? "Unknown",
                e.IsTerminating);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var aggregateException = e.Exception;
        Log.Logger.Error(aggregateException,
            "[App] UNOBSERVED TASK EXCEPTION - InnerExceptions: {InnerExceptionCount}, Message: {Message}",
            aggregateException.InnerExceptions.Count,
            aggregateException.Message);

        // Log each inner exception
        foreach (var innerException in aggregateException.InnerExceptions)
            Log.Logger.Error(innerException, "[App] Inner exception: {ExceptionType} - {Message}",
                innerException.GetType().Name,
                innerException.Message);

        // Mark as observed to prevent app crash
        e.SetObserved();
        Log.Logger.Warning("[App] Task exception marked as observed - app will continue");
    }
}