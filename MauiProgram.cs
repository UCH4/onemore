// MauiProgram.cs
using Microsoft.Extensions.Logging;
using Plugin.Firebase.Auth;        // Necesario para AddFirebaseAuth()
using Plugin.Firebase.Firestore;    // Necesario para AddFirestore()
//using Plugin.Firebase.Shared;       // Necesario para ConfigureFirebase()

using _1mas.Services; // Asegúrate de que este namespace apunte a donde están tus servicios

namespace _1mas
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                // AÑADE ESTO: Inicialización de Plugin.Firebase y registro de servicios de Firebase
                .ConfigureFirebase(firebaseBuilder =>
                {
                    firebaseBuilder.AddFirebaseAuth();      // Habilita Firebase Authentication
                    firebaseBuilder.AddFirestore();         // Habilita Firebase Firestore
                    // Puedes añadir otros servicios si los necesitas (ej. AddCloudMessaging(), AddAnalytics())
                });

            // REGISTRA TUS SERVICIOS PERSONALIZADOS
            builder.Services.AddSingleton<FirebaseAuthService>();
            builder.Services.AddSingleton<FirestoreService>();

            #if DEBUG
                builder.Logging.AddDebug();
            #endif

            return builder.Build();
        }
    }
}