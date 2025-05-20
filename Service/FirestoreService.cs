// Nombre del archivo: FirestoreService.cs
// Ubicación recomendada: Carpeta 'Services' dentro de tu proyecto principal

using System;
using System.Net.Http; // Necesario para realizar peticiones HTTP (API REST)
using System.Text; // Necesario para manejar contenido de texto (JSON)
using System.Threading.Tasks; // Necesario para operaciones asíncronas
using Newtonsoft.Json; // Necesitas el paquete NuGet Newtonsoft.Json para manejar JSON
// En la terminal, en la raíz de tu proyecto: dotnet add package Newtonsoft.Json

// Asegúrate de que el namespace coincida con el de tu proyecto y otros servicios
namespace _1mas.Services // <<<< ¡AJUSTA ESTO A TU NAMESPACE REAL!
{
    // --- Configuración de Firestore API ---
    // >>>>>>>> ¡REEMPLAZA CON TU ID DE PROYECTO REAL! <<<<<<<<

    // Tu ID de Proyecto de Firebase (lo encuentras en Configuración del proyecto -> General)
    const string FirebaseProjectId = "mas-a0b4c"; // <<<< ¡REEMPLAZA CON TU ID REAL!

    // URL base para la API REST de Firestore. El formato es fijo, solo necesitas tu Project ID.
    // Usamos interpolación de cadenas ($"...") para incluir la constante FirebaseProjectId.
    const string FirestoreApiBaseUrl = $"https://firestore.googleapis.com/v1/projects/{FirebaseProjectId}/databases/(default)/documents";


    // Clase que provee los servicios de Base de Datos Firestore
    public class FirestoreService
    {
        // HttpClient se usa para hacer las llamadas a la API REST de Firestore por HTTP.
        private readonly HttpClient _httpClient;
        // Necesitamos una referencia al servicio de autenticación para obtener el token del usuario autenticado.
        // El token es necesario para realizar operaciones en Firestore que están protegidas por tus reglas de seguridad.
        private readonly FirebaseAuthService _authService;

        // Constructor: Recibe una instancia de FirebaseAuthService.
        // Esto es un ejemplo de inyección de dependencias (pasar una instancia de un servicio a otro).
        public FirestoreService(FirebaseAuthService authService)
        {
            _httpClient = new HttpClient();
            _authService = authService; // Guardamos la referencia al servicio de autenticación.
        }

        // --- Métodos para interactuar con Firestore ---

        // Método Asíncrono para añadir un nuevo documento a una colección en Firestore.
        // <T> indica que este método es genérico y puede aceptar cualquier tipo de objeto.
        // 'collectionPath' es el nombre de la colección (ej: "partidos", "usuarios").
        // 'data' es el objeto C# que queremos guardar (ej: una instancia de la clase Partido).
        // 'documentId' es opcional. Si no lo proporcionas, Firestore generará uno automáticamente.
        public async Task<string> AddDocumentAsync<T>(string collectionPath, T data, string documentId = null)
        {
            // Para la mayoría de las operaciones de escritura en Firestore, el usuario debe estar autenticado
            // para cumplir con las reglas de seguridad. Obtenemos el token de autenticación del usuario actual.
            var authToken = _authService.CurrentUser?.Credential?.IdToken;

            // Verificamos si hay un token de autenticación válido.
            if (string.IsNullOrEmpty(authToken))
            {
                // Si no hay usuario autenticado, lanzamos un error. Tus reglas de seguridad deberían prevenir escrituras no autenticadas.
                throw new UnauthorizedAccessException("Usuario no autenticado. No se puede escribir en Firestore.");
            }

            // Construimos la URL de la API REST para la operación.
            var requestUrl = $"{FirestoreApiBaseUrl}/{collectionPath}";
            // Si proporcionamos un documentId, la URL y el método HTTP cambian ligeramente.
            if (!string.IsNullOrEmpty(documentId))
            {
                requestUrl += $"/{documentId}"; // La URL incluye el ID del documento
            }

            // CONVERSIÓN CLAVE: La API REST de Firestore espera que los datos estén en un formato JSON específico.
            // No es solo serializar tu objeto T a JSON directamente. Requiere una estructura con "fields"
            // y para cada campo, especificar su tipo de dato de Firestore (stringValue, integerValue, booleanValue, etc.).
            // Aquí es donde la integración con la API REST pura puede ser compleja y requiere mapeo manual o helpers.

            // TODO: IMPLEMENTAR ESTA FUNCIÓN AUXILIAR ConvertObjectToFirestoreJson<T>
            // Esta función debe tomar tu objeto T y convertirlo a una cadena JSON que cumpla con el formato de la API REST de Firestore.
            // EJEMPLO DEL FORMATO QUE ESPERA LA API REST:
            /*
            {
              "fields": {
                "nombreCampo1": { "tipoValor": "valor" }, // ej: "nombre": { "stringValue": "Mi nombre" }
                "nombreCampo2": { "tipoValor": "valor" }  // ej: "edad": { "integerValue": "30" } (los números van como cadena en integerValue/doubleValue)
                // ... otros campos ...
              }
            }
            */
            string firestoreJsonContent = ConvertObjectToFirestoreJson(data); // <<-- ¡NECESITAS IMPLEMENTAR ESTO!

            // Creamos el contenido HTTP con el JSON y especificamos que es JSON.
            var content = new StringContent(firestoreJsonContent, Encoding.UTF8, "application/json");

            // Añadimos el token de autenticación al encabezado de la petición.
            // Esto permite que Firebase sepa qué usuario está haciendo la petición y aplique tus reglas de seguridad.
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            // Realizamos la llamada HTTP a la API REST.
            HttpResponseMessage response;
            if (string.IsNullOrEmpty(documentId))
            {
                 // Usamos POST si queremos que Firebase genere un ID de documento único automáticamente.
                 // La URL es: /documents/collectionPath
                 response = await _httpClient.PostAsync(requestUrl, content);
            }
            else
            {
                 // Usamos PATCH para crear un documento con un ID específico o para actualizar un documento existente.
                 // La URL es: /documents/collectionPath/documentId
                 response = await _httpClient.PatchAsync(requestUrl, content);
            }


            // Verificamos si la llamada fue exitosa (códigos 2xx).
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Documento añadido/actualizado en Firestore: {responseContent}");

                // Si usaste POST (Firebase generó el ID), la respuesta contiene el nombre completo del documento (incluyendo el ID).
                // Puedes necesitar analizar 'responseContent' para extraer el ID si lo necesitas.
                // La respuesta exitosa de POST es un objeto JSON que incluye el campo "name".
                if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                     try
                     {
                          var docResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                          // El campo "name" en la respuesta de POST contiene la ruta completa del documento, ej: projects/.../documents/collection/DOC_ID
                          // Podemos extraer el ID del final de la ruta.
                          string fullDocName = docResponse?.name;
                          if (!string.IsNullOrEmpty(fullDocName))
                          {
                              return fullDocName.Substring(fullDocName.LastIndexOf('/') + 1); // Extrae solo el ID
                          }
                     }
                     catch (Exception ex)
                     {
                          Console.WriteLine($"Advertencia: No se pudo analizar la respuesta para obtener el ID del documento. {ex.Message}");
                     }
                }

                return "Success"; // O devuelve un identificador si lo extrajiste.
            }
            else
            {
                // Si la llamada no fue exitosa, leemos el contenido de error de la respuesta.
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error al añadir/actualizar documento en Firestore: {response.StatusCode} - {errorContent}");
                // Lanzamos una excepción para manejar el error en el código que llamó a este método.
                throw new Exception($"Error de Firestore: {response.StatusCode} - {errorContent}");
            }
        }

        // --- Función Auxiliar Necesaria para Mapeo a Formato JSON de Firestore API REST ---
        // Esta función debe tomar un objeto C# y convertirlo en una cadena JSON
        // que siga el formato específico de "fields": { "campo": { "tipoValor": "valor" }, ... }
        // Implementar esto requiere mapear los tipos de datos de C# a los tipos de datos de Firestore REST API.
        // Esto puede ser complejo para todos los tipos posibles (string, int, double, bool, array, map, null, timestamp, geoPoint, reference).
        private string ConvertObjectToFirestoreJson<T>(T data)
        {
            // Si la librería Firebase.Net tiene utilidades para esto, úsalas aquí.
            // Si no, necesitas implementar la lógica de mapeo manualmente.

            // EJEMPLO MUY BÁSICO Y LIMITADO (Solo para ilustrar el concepto, NO USAR TAL CUAL para todos los tipos):
            // Esto solo funcionaría para un objeto simple con propiedades string, int, bool.
            // Necesitarías manejar DateTime, listas, otros objetos anidados, etc.
            /*
            var fields = new Dictionary<string, object>();
            var properties = typeof(T).GetProperties(); // Obtener propiedades del objeto T

            foreach (var prop in properties)
            {
                var value = prop.GetValue(data);
                if (value is string s) fields[prop.Name] = new { stringValue = s };
                else if (value is int i) fields[prop.Name] = new { integerValue = i.ToString() }; // Números van como cadena en el valor
                else if (value is bool b) fields[prop.Name] = new { booleanValue = b };
                else if (value == null) fields[prop.Name] = new { nullValue = null };
                // TODO: Añadir manejo para double, float, DateTime (timestampValue), List (arrayValue),
                //       otros objetos (mapValue), GeoPoint (geoPointValue), referencias (referenceValue)
                else
                {
                     // Si encuentras un tipo no soportado, podrías lanzar un error o ignorarlo.
                     Console.WriteLine($"Advertencia: Tipo de dato no mapeado para Firestore REST API: {prop.PropertyType.Name}");
                }
            }

            var firestoreFieldsContainer = new { fields = fields };
            return JsonConvert.SerializeObject(firestoreFieldsContainer);
            */
            // Dado que esto es complejo de implementar genéricamente, si no hay utilidades en Firebase.Net,
            // podrías considerar usar una librería cliente diferente (como Plugin.CloudFirestore si es compatible).
             throw new NotImplementedException("Implementación de ConvertObjectToFirestoreJson<T> requerida.");
        }


        // Método Asíncrono para obtener un documento de una colección por su ID de Firestore.
        public async Task<T> GetDocumentAsync<T>(string collectionPath, string documentId)
        {
             // Obtener el token del usuario autenticado (necesario si tus reglas de seguridad requieren autenticación para leer)
            var authToken = _authService.CurrentUser?.Credential?.IdToken;

            // Construir la URL para obtener un documento específico.
            var requestUrl = $"{FirestoreApiBaseUrl}/{collectionPath}/{documentId}";

            // Añadir el token de autenticación al encabezado si está disponible (para reglas de seguridad).
            if (!string.IsNullOrEmpty(authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
            }
            else
            {
                 // Si no hay token (lectura pública), asegúrate de que no se envíe el encabezado de autorización.
                 _httpClient.DefaultRequestHeaders.Authorization = null;
                 // Para algunas configuraciones de reglas, podrías necesitar añadir la API Key como parámetro de query para lecturas no autenticadas.
                 // requestUrl += $"?key={FirebaseWebApiKey}"; // Descomentar si tus reglas lo requieren
            }

            // Realizar la llamada GET a la API REST.
            HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);

            // Verificar si la llamada fue exitosa.
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Documento obtenido de Firestore: {responseContent}");

                // CONVERSIÓN CLAVE: El JSON de respuesta también tiene el formato de "fields": { ... }
                // Necesitas tomar este JSON y convertirlo de vuelta a tu objeto C# de tipo T.

                // TODO: IMPLEMENTAR ESTA FUNCIÓN AUXILIAR ConvertFirestoreJsonToObject<T>
                // Esta función debe tomar la cadena JSON de respuesta de Firestore API REST
                // y mapear los campos (stringValue, integerValue, etc.) a las propiedades de tu objeto T.
                // return ConvertFirestoreJsonToObject<T>(responseContent); // <<-- ¡NECESITAS IMPLEMENTAR ESTO!

                 // EJEMPLO MUY BÁSICO Y LIMITADO de deserialización:
                 /*
                 try
                 {
                     var firestoreData = JsonConvert.DeserializeObject<dynamic>(responseContent);
                     var fields = firestoreData?.fields;
                     if (fields != null)
                     {
                         // Necesitas mapear dinámicamente los campos de Firestore a las propiedades de T.
                         // Esto es lo inverso de ConvertObjectToFirestoreJson.
                         // Ejemplo: Si T es la clase Persona { string Nombre; int Edad; }
                         // var nombre = fields.Nombre?.stringValue?.Value;
                         // var edadStr = fields.Edad?.integerValue?.Value;
                         // int edad = int.TryParse(edadStr, out int parsedEdad) ? parsedEdad : 0;
                         // return (T)(object)new Persona { Nombre = nombre, Edad = edad };

                          throw new NotImplementedException("Implementación de deserialización de campos Firestore requerida.");
                     }
                     // Si el documento existe pero no tiene campos (posible aunque raro)
                     return default(T);
                 }
                 catch (Exception ex)
                 {
                      Console.WriteLine($"Error al deserializar JSON de Firestore: {ex.Message}");
                      throw new Exception($"Error al procesar datos de Firestore: {ex.Message}");
                 }
                 */
                 throw new NotImplementedException("Implementación de ConvertFirestoreJsonToObject<T> requerida.");

            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Si el código de estado es 404 Not Found, el documento no existe.
                Console.WriteLine($"Documento no encontrado: {collectionPath}/{documentId}");
                return default(T); // Devuelve el valor predeterminado para T (null para clases)
            }
            else
            {
                // Si hay otro error HTTP.
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error al obtener documento de Firestore: {response.StatusCode} - {errorContent}");
                throw new Exception($"Error de Firestore: {response.StatusCode} - {errorContent}");
            }
        }

        // --- TODO: Implementar otros métodos comunes de Firestore ---
        // - UpdateDocumentAsync<T>(string collectionPath, string documentId, T data): Para actualizar un documento existente. Usa PATCH.
        // - DeleteDocumentAsync(string collectionPath, string documentId): Para eliminar un documento. Usa DELETE.
        // - GetCollectionAsync<T>(string collectionPath): Para obtener todos los documentos de una colección (puede ser limitado y requerir paginación). Usa GET en /documents/collectionPath.
        // - QueryCollectionAsync<T>(string collectionPath, object query): Para realizar consultas más avanzadas (filtrado, ordenación, etc.). Usa POST en /documents/collectionPath:runQuery con un cuerpo JSON que define la consulta. Este es el más complejo con la API REST pura.

        // --- Implementación de las funciones auxiliares de mapeo (NECESARIAS) ---
        private string ConvertObjectToFirestoreJson<T>(T data)
        {
            // Implementación real para mapear de T a JSON con formato de Firestore fields.
            // Esto es lo que Plugin.CloudFirestore haría por ti automáticamente.
             throw new NotImplementedException("La función ConvertObjectToFirestoreJson no está implementada.");
        }

         private T ConvertFirestoreJsonToObject<T>(string jsonContent)
         {
             // Implementación real para mapear de JSON con formato de Firestore fields a T.
              // Esto es lo que Plugin.CloudFirestore haría por ti automáticamente.
              throw new NotImplementedException("La función ConvertFirestoreJsonToObject no está implementada.");
         }
    }
}