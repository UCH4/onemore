using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Plugin.Firebase.Auth; // Necesario para obtener el token del usuario
using Newtonsoft.Json; // Puedes mantenerlo si lo usas para tus propios objetos, Plugin.Firebase maneja su propio JSON.

// Nuevos usings para Plugin.Firebase.Firestore
using Plugin.Firebase.Firestore;



namespace _1mas.Services
{
    // La clase FirebaseConstants ya no es necesaria aquí si solo usas Firestore a través de Plugin.Firebase.
    // Plugin.Firebase maneja la conexión directamente.
    // Si aún necesitas tu FirebaseProjectId o FirestoreApiBaseUrl para *otras* llamadas REST directas,
    // puedes mantener tu clase FirebaseConstants aparte.
    // Para la comunicación directa con Firestore a través de Plugin.Firebase, ya no es necesaria.

    public class FirestoreService
    {
        private readonly IFirebaseFirestore _firestore; // Usa la interfaz de Plugin.Firebase
        private readonly FirebaseAuthService _authService; // Necesario para acceder al usuario autenticado

        public FirestoreService(FirebaseAuthService authService)
        {
            _firestore = CrossFirebaseFirestore.Current; // Obtiene la instancia actual del cliente de Firestore
            _authService = authService;
        }

        // Método Asíncrono para añadir un nuevo documento a una colección en Firestore.
        public async Task<string> AddDocumentAsync<T>(string collectionPath, T data, string documentId = null)
        {
            // Plugin.Firebase maneja la autenticación internamente para las operaciones de Firestore
            // solo asegurándote de que el usuario esté logueado. No necesitas pasar el token explícitamente.

            try
            {
                if (string.IsNullOrEmpty(documentId))
                {
                    // Añadir un nuevo documento con un ID generado automáticamente
                    var documentReference = await _firestore.Collection(collectionPath).AddDocumentAsync(data);
                    Console.WriteLine($"Documento añadido en Firestore con ID: {documentReference.Id}");
                    return documentReference.Id;
                }
                else
                {
                    // Actualizar/establecer un documento con un ID específico
                    await _firestore.Collection(collectionPath).Document(documentId).SetDataAsync(data);
                    Console.WriteLine($"Documento actualizado/establecido en Firestore con ID: {documentId}");
                    return documentId;
                }
            }
            catch (FirebaseFirestoreException ex)
            {
                Console.WriteLine($"Error de Firestore al añadir/actualizar documento: {ex.ErrorCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al añadir/actualizar documento: {ex.Message}");
                throw;
            }
        }

        // Método Asíncrono para obtener un documento de una colección por su ID.
        public async Task<T> GetDocumentAsync<T>(string collectionPath, string documentId)
        {
            try
            {
                // Obtener el documento por su ID
                var documentSnapshot = await _firestore.Collection(collectionPath).Document(documentId).GetDocumentAsync();

                if (documentSnapshot.Exists)
                {
                    // Convertir el documento a tu tipo T
                    var data = documentSnapshot.GetData<T>();
                    Console.WriteLine($"Documento obtenido de Firestore: {documentId}");
                    return data;
                }
                else
                {
                    Console.WriteLine($"Documento no encontrado: {collectionPath}/{documentId}");
                    return default(T)!; 
                }
            }
            catch (FirebaseFirestoreException ex)
            {
                Console.WriteLine($"Error de Firestore al obtener documento: {ex.ErrorCode} - {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al obtener documento: {ex.Message}");
                throw;
            }
        }

        // Puedes añadir métodos para eliminar, obtener colecciones, hacer queries, etc.
        // Ej: public async Task DeleteDocumentAsync(string collectionPath, string documentId) { /* ... */ }
        // Ej: public async Task<IEnumerable<T>> GetCollectionAsync<T>(string collectionPath) { /* ... */ }
        // Ej: public async Task<IEnumerable<T>> QueryCollectionAsync<T>(string collectionPath, Query query) { /* ... */ }
    }
}