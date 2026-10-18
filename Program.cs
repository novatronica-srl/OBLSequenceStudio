namespace OBLSequenceStudio;

/// <summary>Hosts the single-threaded Windows Forms application entry point.</summary>
internal static class Program
{
    /// <summary>Initializes Windows Forms application settings and runs the main editor message loop.</summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
