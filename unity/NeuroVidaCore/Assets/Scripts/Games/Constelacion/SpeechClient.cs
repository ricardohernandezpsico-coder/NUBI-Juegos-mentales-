using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeuroVida.Games.Constelacion
{
    /// <summary>Un evento del reconocedor de voz (ver <c>com.example.bridge.SpeechBridge</c> en la app).</summary>
    [Serializable]
    public sealed class SpeechEvent
    {
        /// <summary>"ready", "partial", "final", "restart", "error" o "end".</summary>
        public string type;
        public string text;
        public int code;
        /// <summary>Milisegundos desde que empezó a escuchar.</summary>
        public long t;
        /// <summary>Otras lecturas posibles (solo en "final").</summary>
        public string[] alts;
    }

    /// <summary>
    /// La voz, desde Unity: llama a la clase Kotlin <c>com.example.bridge.SpeechBridge</c> de la app (misma APK, corre
    /// en el proceso de Unity) con <see cref="AndroidJavaClass"/> y lee sus eventos con <see cref="Poll"/> en cada
    /// cuadro (sin callbacks entre Java y C#). Pide el permiso de micrófono si falta. En el editor no hay voz:
    /// <see cref="Available"/> es falso y el juego usa el teclado.
    /// Probado antes con la prueba de voz de la app en el Motorola de Ricardo (28-sep): con y sin internet, sin "bip",
    /// sin errores, al instante.
    /// </summary>
    public static class SpeechClient
    {
        private const string BridgeClass = "com.example.bridge.SpeechBridge";

        [Serializable]
        private sealed class Batch { public SpeechEvent[] items; }

        /// <summary>¿Hay reconocimiento de voz en este teléfono?</summary>
        public static bool Available
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using (var bridge = new AndroidJavaClass(BridgeClass))
                        return bridge.CallStatic<bool>("isAvailable", Activity());
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SpeechClient] Sin reconocimiento de voz: " + e.Message);
                    return false;
                }
#else
                return false;
#endif
            }
        }

        /// <summary>¿Ya dio permiso de micrófono?</summary>
        public static bool HasPermission
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
#else
                return false;
#endif
            }
        }

        /// <summary>Pide el permiso de micrófono; <paramref name="done"/> recibe si quedó concedido.</summary>
        public static void RequestPermission(Action<bool> done)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (HasPermission) { done(true); return; }
            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += _ => done(true);
            callbacks.PermissionDenied += _ => done(false);
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone, callbacks);
#else
            done(false);
#endif
        }

        /// <summary>
        /// Empieza a escuchar sin parar. <paramref name="offline"/>: reconocer en el propio teléfono si se puede (la voz no
        /// sale del teléfono); si el teléfono no tiene el español, la app pasa sola al reconocedor con internet.
        /// </summary>
        public static void Start(bool offline)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass(BridgeClass))
                {
                    string lang = bridge.CallStatic<string>("defaultLanguage");
                    bridge.CallStatic("start", Activity(), lang, offline);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[SpeechClient] No se pudo empezar a escuchar: " + e.Message);
            }
#endif
        }

        public static void Stop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var bridge = new AndroidJavaClass(BridgeClass)) bridge.CallStatic("stop");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SpeechClient] stop: " + e.Message);
            }
#endif
        }

        /// <summary>¿Está reconociendo en el teléfono (sin internet)?</summary>
        public static bool Offline
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using (var bridge = new AndroidJavaClass(BridgeClass)) return bridge.CallStatic<bool>("isOffline");
                }
                catch (Exception) { return false; }
#else
                return false;
#endif
            }
        }

        /// <summary>Eventos nuevos desde la última lectura (vacío si no hay voz).</summary>
        public static List<SpeechEvent> Poll()
        {
            var list = new List<SpeechEvent>();
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                string json;
                using (var bridge = new AndroidJavaClass(BridgeClass)) json = bridge.CallStatic<string>("poll");
                list.AddRange(Parse(json));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SpeechClient] poll: " + e.Message);
            }
#endif
            return list;
        }

        /// <summary>Lee el arreglo JSON de eventos (JsonUtility no lee arreglos sueltos: se envuelve).</summary>
        public static SpeechEvent[] Parse(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "[]") return new SpeechEvent[0];
            var batch = JsonUtility.FromJson<Batch>("{\"items\":" + json + "}");
            return batch?.items ?? new SpeechEvent[0];
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject Activity()
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                return player.GetStatic<AndroidJavaObject>("currentActivity");
        }
#endif
    }
}
