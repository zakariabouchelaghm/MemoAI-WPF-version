using System.Windows;
using System;

namespace MemoAI
{
    public partial class App : Application
    {
        // C'est ICI que l'on crée "AI_Engine"
        // On l'appelle static pour pouvoir y accéder depuis n'importe quelle fenêtre
        public static EmbeddingService AI_Engine { get; } = new EmbeddingService();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // On initialise la base de données et sqlite-vec
                await AI_Engine.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Détails de l'erreur : {ex.Message}\n\nInterne : {ex.InnerException?.Message}");
                Shutdown();
            }
        }
    }
}