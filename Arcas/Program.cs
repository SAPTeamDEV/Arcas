namespace Arcas
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            
            // Launch the setup wizard instead of the main form
            using var setupWizard = new SetupWizard();
            var result = setupWizard.ShowDialog();
            
        }
    }
}