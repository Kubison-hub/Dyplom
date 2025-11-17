using UnityEngine;
using Firebase.Extensions;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System;

public class FirebaseInitializer : MonoBehaviour
{
    private static bool firebaseInitialized = false;

    // Metoda statyczna do inicjalizacji Firebase i autoryzacji
    public static void InitializeFirebase(MonoBehaviour context, Action<FirebaseFirestore, string> onInitialized)
    {
        if (firebaseInitialized)
        {
            // Jeœli ju¿ zainicjalizowano, po prostu uruchom Auth
            AuthenticateAndInitFirestore(context, onInitialized);
            return;
        }

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"Nie uda³o siê rozwi¹zaæ zale¿noœci Firebase: {task.Exception}");
                return;
            }

            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Inicjalizacja Aplikacji Firebase
                string firebaseConfigJson = typeof(__firebase_config) != null ? __firebase_config.ToString() : "{}";
                FirebaseApp app;

                try
                {
                    // U¿ywamy konfiguracji z globalnej zmiennej __firebase_config
                    AppOptions options = GetAppOptionsFromJson(firebaseConfigJson);
                    app = FirebaseApp.InitializeApp(options);
                    firebaseInitialized = true;
                    Debug.Log("Firebase App zainicjalizowany pomyœlnie.");

                    AuthenticateAndInitFirestore(context, onInitialized);
                }
                catch (Exception e)
                {
                    Debug.LogError($"B³¹d inicjalizacji Firebase App: {e}");
                }
            }
            else
            {
                Debug.LogError($"Zale¿noœci Firebase nie s¹ dostêpne: {dependencyStatus}");
            }
        });
    }

    // Logika autoryzacji i inicjalizacji Firestore
    private static void AuthenticateAndInitFirestore(MonoBehaviour context, Action<FirebaseFirestore, string> onInitialized)
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        string initialAuthToken = typeof(__initial_auth_token) != null ? __initial_auth_token.ToString() : null;

        System.Threading.Tasks.Task authTask;
        if (!string.IsNullOrEmpty(initialAuthToken))
        {
            authTask = auth.SignInWithCustomTokenAsync(initialAuthToken);
        }
        else
        {
            authTask = auth.SignInAnonymouslyAsync();
        }

        authTask.ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError($"B³¹d autoryzacji Firebase: {task.Exception}");
                return;
            }

            FirebaseUser newUser = task.Result.User;
            string userId = newUser.UserId;
            Debug.Log($"U¿ytkownik zalogowany pomyœlnie. UID: {userId}");

            // W³¹czenie logowania debugowania dla Firestore
            FirebaseFirestore.LogLevel = LogLevel.Debug;

            onInitialized?.Invoke(db, userId);
        });
    }

    // Funkcja pomocnicza do parsowania AppOptions z JSON (dla Canvasa)
    private static AppOptions GetAppOptionsFromJson(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "{}") return null;

        // W praktyce, w œrodowisku Canvas, potrzebujemy po prostu zainicjalizowaæ.
        // Jeœli jednak zale¿y na precyzyjnym parsowaniu:
        // Poni¿ej jest bardzo uproszczone parsowanie, w Unity czêsto u¿ywa siê FirebaseApp.InitializeApp() bez opcji, 
        // jeœli JSON jest poprawnie za³adowany w œrodowisku.
        // Dla œrodowiska Canvasa, gdzie konfiguracja jest podana, ta funkcja mo¿e byæ pominiêta.

        return null; // Zwracamy null i polegamy na domyœlnej inicjalizacji.
    }
}