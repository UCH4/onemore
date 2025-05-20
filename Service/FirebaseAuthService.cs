// Nombre del archivo: FirebaseAuthService.cs
// Ubicación recomendada: Carpeta 'Services' dentro de tu proyecto principal
using System; // Para excepciones y Console.WriteLine
using System.Threading.Tasks; // Necesario para operaciones asíncronas (que se comunican con Firebase)
using Firebase.Auth; // Necesario para usar FirebaseAuthClient y otros tipos de la librería
// Namespace: Reemplaza 'TuProyecto' con el nombre real de tu proyecto MAUI
// Puedes verificar el namespace principal en las propiedades de tu proyecto o en App.xaml.cs
namespace _1mas.Services // <<<< ¡AJUSTA ESTO A TU NAMESPACE REAL!
{
    // --- Configuración de Firebase API ---
    // >>>>>>>> ¡REEMPLAZA LAS CADENAS CON TUS VALORES REALES DE FIREBASE! <<<<<<<<

    // 1. Tu Clave Web API de Firebase: La obtuviste de la Configuración del proyecto en la consola de Firebase.
    //    Es una cadena larga de letras y números (ej: AIzaSyC...).
    //    ¡Es CRUCIAL para que la librería de autenticación sepa a qué proyecto conectarse!
    //    ADVERTENCIA DE SEGURIDAD: Guardar la API Key directamente aquí no es lo más seguro en una app de producción
    //    que vayas a publicar. Para empezar está bien y que funcione, pero considera usar métodos más seguros
    //    (como MAUI Essentials Secure Storage) más adelante para proteger tus claves sensibles.
    const string FirebaseWebApiKey = "AIzaSyCOb0r9r7-yo6091ZjCQVglAqM-nTOvCX4"; // <<<< ¡REEMPLAZA ESTA CADENA CON TU CLAVE REAL!

    // 2. Tu Dominio de Autenticación de Firebase: Suele ser el ID de tu proyecto + ".firebaseapp.com".
    //    El ID de tu proyecto también está en la Configuración del proyecto en la consola de Firebase.
    //    Ejemplo: Si el ID de tu proyecto es "mi-app-futbol-12345", el dominio sería "mi-app-futbol-12345.firebaseapp.com".
    const string FirebaseAuthDomain = "mas-a0b4c.firebaseapp.com"; // <<<< ¡REEMPLAZA ESTA CADENA CON TU DOMINIO REAL!


    // Clase que provee los servicios de Autenticación de Firebase
    public class FirebaseAuthService
    {
        // _authClient es la instancia principal de la librería FirebaseAuthentication.net
        // Usaremos esta instancia para realizar operaciones como crear usuario, iniciar sesión, etc.
        private readonly FirebaseAuthClient _authClient;

        // Constructor de la clase: Se ejecuta cuando creas una nueva instancia de FirebaseAuthService.
        // Aquí es donde configuramos el cliente de autenticación.
        public FirebaseAuthService()
        {
            // Inicializamos el cliente de autenticación con la configuración específica de tu proyecto Firebase.
            _authClient = new FirebaseAuthClient(new FirebaseAuthConfig
            {
                // Le pasamos la clave API y el dominio de autenticación para que sepa a qué proyecto de Firebase conectarse.
                ApiKey = FirebaseWebApiKey, // Usa la constante que definimos arriba con tu clave
                AuthDomain = FirebaseAuthDomain, // Usa la constante que definimos arriba con tu dominio

                // Especificamos qué métodos de inicio de sesión vamos a utilizar.
                // EmailAndPasswordProvider() permite iniciar sesión/registrar con email y contraseña.
                // Debe estar habilitado este método en la consola de Firebase.
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailAndPasswordProvider()
                    // Si habilitas y quieres usar otros proveedores (Google Sign-In, Facebook Login, etc.)
                    // con esta librería, tendrías que añadir los proveedores correspondientes aquí.
                    // A menudo, estos requieren paquetes NuGet adicionales específicos para cada proveedor.
                }
            });

            // Opcional pero MUY útil: Suscribirse a un evento que se dispara cada vez que el estado de autenticación del usuario cambia.
            // Esto ocurre cuando un usuario inicia sesión, cierra sesión, o su token de autenticación se refresca.
            // Puedes usar este evento para actualizar la UI de tu aplicación o el estado general del usuario.
            _authClient.AuthStateChanged += AuthClient_AuthStateChanged;
        }

        // Método manejador para el evento AuthStateChanged.
        // Se ejecuta automáticamente cuando el estado de autenticación cambia.
        private void AuthClient_AuthStateChanged(object sender, UserEventArgs e)
        {
            // 'e.User' contiene la información del usuario actualmente autenticado.
            // Si es 'null', significa que no hay ningún usuario loggeado en este momento.
            // Si no es 'null', contiene información útil como Email, UID (User ID), etc.
            var user = e.User;
            if (user != null)
            {
                // Si un usuario se autenticó (inició sesión o se registró exitosamente)
                Console.WriteLine($"Evento AuthStateChanged: Usuario autenticado: {user.Email}");
                // Aquí podrías, por ejemplo, navegar a la página principal, actualizar un icono de perfil, etc.
                // (La lógica de actualización de UI normalmente iría en un ViewModel que esté suscrito a este evento, no directamente aquí).
            }
            else
            {
                // Si no hay ningún usuario autenticado (por ejemplo, después de cerrar sesión o al iniciar la app por primera vez)
                Console.WriteLine("Evento AuthStateChanged: Ningún usuario autenticado.");
                // Aquí podrías, por ejemplo, navegar de regreso a la página de login.
            }
        }

        // --- Métodos para realizar operaciones de Autenticación ---

        // Método Asíncrono para Registrar un nuevo usuario con Email y Contraseña en Firebase.
        // Es 'async' porque se comunica con Firebase por internet, lo que toma tiempo y no queremos bloquear la app.
        // Devuelve un 'Task<UserCredential>', que representa una operación que completará en el futuro y devolverá un UserCredential.
        public async Task<UserCredential> RegisterUserAsync(string email, string password)
        {
            try
            {
                // Llamamos al método de la librería '_authClient' para crear el usuario.
                // 'await' espera a que la operación asíncrona termine sin bloquear el hilo principal.
                var userCredential = await _authClient.CreateUserWithEmailAndPasswordAsync(email, password);

                // Si la operación fue exitosa, imprimimos un mensaje y devolvemos las credenciales del nuevo usuario.
                Console.WriteLine($"Usuario {userCredential.User.Email} registrado.");
                return userCredential; // Contiene el objeto User, tokens, etc.
            }
            // Capturamos una excepción específica de Firebase Authentication.
            // Esto es útil para manejar errores como "email ya existe", "contraseña débil", etc.
            catch (FirebaseAuthException ex)
            {
                // 'ex.Reason' a menudo proporciona un código de error útil (ej: EmailExists, WeakPassword).
                // 'ex.Message' proporciona más detalles.
                Console.WriteLine($"Error de registro en Firebase: {ex.Reason} - {ex.Message}");
                // Relanzamos la excepción ('throw') para que el código que llamó a este método (por ejemplo, un ViewModel)
                // pueda capturarla y mostrar un mensaje de error adecuado al usuario en la UI.
                throw;
            }
            // Capturamos cualquier otra excepción general que pudiera ocurrir (problemas de red, etc.).
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al registrar usuario: {ex.Message}");
                throw; // Relanzar la excepción
            }
        }

        // Método Asíncrono para Iniciar Sesión con Email y Contraseña en Firebase.
        // Similar a RegisterUserAsync, pero para usuarios existentes.
        public async Task<UserCredential> LoginUserAsync(string email, string password)
        {
            try
            {
                // Llama al método de la librería para iniciar sesión.
                var userCredential = await _authClient.SignInWithEmailAndPasswordAsync(email, password);

                // Si la operación fue exitosa.
                Console.WriteLine($"Usuario {userCredential.User.Email} inició sesión.");
                return userCredential; // Contiene el objeto User, tokens, etc.
            }
             // Captura una excepción específica de Firebase Authentication.
             // Útil para errores como "usuario no encontrado", "contraseña incorrecta", etc.
             catch (FirebaseAuthException ex)
            {
                 // 'ex.Reason' puede ser EmailNotFound, WrongPassword, InvalidEmailAddress, etc.
                Console.WriteLine($"Error de inicio de sesión en Firebase: {ex.Reason} - {ex.Message}");
                 // Relanzar la excepción para que se maneje en el ViewModel.
                throw;
            }
            // Captura cualquier otra excepción general.
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al iniciar sesión: {ex.Message}");
                throw; // Relanzar la excepción
            }
        }

        // Método para Cerrar Sesión.
        public void SignOut()
        {
            try
            {
                // Llama al método de la librería para cerrar la sesión del usuario actual.
                _authClient.SignOut();
                Console.WriteLine("Sesión cerrada.");
                // El evento AuthStateChanged se disparará después de esto con e.User siendo null.
                // Aquí podrías añadir lógica para limpiar datos locales del usuario si es necesario.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cerrar sesión: {ex.Message}");
                // Manejar el error al cerrar sesión si ocurre.
            }
        }

        // Propiedad conveniente para obtener el objeto del usuario actualmente autenticado.
        // Si no hay nadie loggeado, devuelve 'null'. Si hay un usuario, devuelve un objeto 'User'.
        public User CurrentUser => _authClient.User;

        // Propiedad booleana para verificar rápidamente si hay algún usuario autenticado.
        public bool IsUserAuthenticated => _authClient.User != null;
    }
}