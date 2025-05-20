using System;
using System.Threading.Tasks;
using Plugin.Firebase.Auth; // Nuevo using para Plugin.Firebase.Auth

namespace _1mas.Services
{
    // NO NECESITARÁS FirebaseConstants.FirebaseWebApiKey o FirebaseAuthDomain aquí.
    // Plugin.Firebase se inicializa con google-services.json / GoogleService-Info.plist.
    // Si necesitas la API Key para otras llamadas directas a REST, la defines aparte.

    public class FirebaseAuthService
    {
        private readonly IFirebaseAuth _firebaseAuth; // Usa la interfaz de Plugin.Firebase

        public FirebaseAuthService()
        {
            _firebaseAuth = CrossFirebaseAuth.Current; // Obtiene la instancia actual del cliente de autenticación
            _firebaseAuth.AuthStateChanged += (sender, e) =>
            {
                // El evento AuthStateChanged usa IAuthUser de Plugin.Firebase
                var user = e.AuthUser;
                if (user != null)
                {
                    Console.WriteLine($"Evento AuthStateChanged: Usuario autenticado: {user.Email}");
                }
                else
                {
                    Console.WriteLine("Evento AuthStateChanged: Ningún usuario autenticado.");
                }
            };
        }

        public IAuthUser CurrentUser => _firebaseAuth.CurrentUser; // IAuthUser es el tipo de usuario en Plugin.Firebase

        public bool IsUserAuthenticated => CurrentUser != null;

        public async Task RegisterUserAsync(string email, string password)
        {
            try
            {
                // Usa el método de Plugin.Firebase
                await _firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);
                Console.WriteLine($"Usuario {CurrentUser.Email} registrado.");
            }
            catch (FirebaseAuthException ex) // Las excepciones también son de Plugin.Firebase
            {
                Console.WriteLine($"Error de registro en Firebase: {ex.ErrorCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al registrar usuario: {ex.Message}");
                throw;
            }
        }

        public async Task LoginUserAsync(string email, string password)
        {
            try
            {
                // Usa el método de Plugin.Firebase
                await _firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
                Console.WriteLine($"Usuario {CurrentUser.Email} inició sesión.");
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine($"Error de inicio de sesión en Firebase: {ex.ErrorCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al iniciar sesión: {ex.Message}");
                throw;
            }
        }

        public void SignOut()
        {
            try
            {
                // Usa el método de Plugin.Firebase
                _firebaseAuth.SignOut();
                Console.WriteLine("Sesión cerrada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cerrar sesión: {ex.Message}");
            }
        }
    }
}