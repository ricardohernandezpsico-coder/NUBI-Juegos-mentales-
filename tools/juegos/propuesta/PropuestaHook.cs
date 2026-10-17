// TEMPORAL (Tarea 114, parte 2): NO va al juego ni se commitea dentro de Assets. Se copia a Assets/Scripts/Bootstrap/Editor/ SOLO mientras se sacan las capturas de la propuesta de volumen y se borra después (junto con el parche de ScreenshotRunner que lo llama).
// Para un cuadro de Engranajes saca, además de la toma normal («hoy»), las MISMAS tomas por capas, para poder rehacer cada objeto por separado:
//   · «fondo»: el cuadro sin el cohete ni la máquina (queda la sala y el cielo);
//   · por cada GRUPO (cada engranaje, cada correa con sus poleas, el motor y su flecha, cada cartel, cada parte del cohete…): el grupo SOLO (sin las sombras ni los soportes horneados de los engranajes), sobre un telón NEGRO (K) y uno BLANCO (W).
// Con K y W el alfa de cada píxel sale exacto (alfa = 1 − (W − K)) y el color premultiplicado es K; así no queda ningún halo del fondo en los bordes y cada objeto tiene su propia silueta aunque toque a otro (los engranajes que encajan).
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroVida.Bridge.EditorTools
{
    public static class PropuestaHook
    {
        private sealed class Grupo { public string Nombre; public readonly List<GameObject> Objetos = new List<GameObject>(); }

        private static GameObject _telon;
        private static List<Grupo> _grupos = new List<Grupo>();
        private static readonly List<KeyValuePair<GameObject, bool>> _estado = new List<KeyValuePair<GameObject, bool>>();

        private static Type TipoDe(string nombre)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) { var t = asm.GetType(nombre); if (t != null) return t; }
            return null;
        }

        private static object Controller()
        {
            var t = TipoDe("NeuroVida.Games.Engranajes.EngranajesGameController");
            return t == null ? null : UnityEngine.Object.FindObjectOfType(t);
        }

        private static object Campo(object o, string nombre)
        {
            var f = o.GetType().GetField(nombre, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return f == null ? null : f.GetValue(o);
        }

        public static bool Hay() => Controller() != null;

        private static string ClaveDe(string nombre)
        {
            int u = nombre.IndexOf('_');
            if (u > 0) return nombre.Substring(0, u);                                           // Belt0_line00 → Belt0
            if (nombre.StartsWith("WindowGlow")) return "WindowGlow";
            if (nombre.StartsWith("Window")) return "Window";
            if (nombre.StartsWith("Pulse")) return "Pulse";
            return nombre;
        }

        /// <summary>Arma la lista de grupos (de abajo hacia arriba: primero las partes del cohete, después la máquina) con lo que está encendido ahora; devuelve sus nombres.</summary>
        public static List<string> Grupos()
        {
            _grupos = new List<Grupo>();
            var c = Controller(); var nombres = new List<string>(); if (c == null) return nombres;
            var cohete = Campo(c, "_rocketRoot") as RectTransform; var maquina = Campo(c, "_machineLayer") as RectTransform;
            void Recorre(RectTransform raiz, string prefijo)
            {
                if (raiz == null) return;
                var porClave = new Dictionary<string, Grupo>();
                for (int i = 0; i < raiz.childCount; i++)
                {
                    var hijo = raiz.GetChild(i).gameObject; if (!hijo.activeSelf) continue;
                    var clave = ClaveDe(hijo.name);
                    if (!porClave.TryGetValue(clave, out var g)) { g = new Grupo { Nombre = prefijo + clave }; porClave[clave] = g; _grupos.Add(g); }
                    g.Objetos.Add(hijo);
                }
            }
            Recorre(cohete, "R.");
            Recorre(maquina, "M.");
            foreach (var g in _grupos) nombres.Add(g.Nombre);
            return nombres;
        }

        private static void Guarda(GameObject go) { _estado.Add(new KeyValuePair<GameObject, bool>(go, go.activeSelf)); }

        private static void Restaura()
        {
            for (int i = _estado.Count - 1; i >= 0; i--) if (_estado[i].Key != null) _estado[i].Key.SetActive(_estado[i].Value);
            _estado.Clear();
            if (_telon != null) { UnityEngine.Object.DestroyImmediate(_telon); _telon = null; }
        }

        private static void PonTelon(Color color)
        {
            var c = Controller(); if (c == null) return;
            var raiz = Campo(c, "_sceneRoot") as RectTransform; if (raiz == null) return;
            if (_telon == null)
            {
                _telon = new GameObject("PropuestaTelon", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)_telon.transform; rt.SetParent(raiz, false);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(-3000f, -3000f); rt.offsetMax = new Vector2(3000f, 3000f);
                _telon.GetComponent<Image>().raycastTarget = false; rt.SetAsFirstSibling();
            }
            _telon.GetComponent<Image>().color = color;
        }

        /// <summary>Deja visible SOLO el grupo [indice] (o ninguno si es −1: queda la sala) sobre un telón del [color]; los engranajes van sin su brillo, soporte ni sombra horneada. [Vuelve] devuelve todo a como estaba.</summary>
        public static void Solo(int indice, Color? color)
        {
            Restaura();
            var c = Controller(); if (c == null) return;
            foreach (var g in _grupos) foreach (var o in g.Objetos) { Guarda(o); o.SetActive(false); }
            if (indice >= 0 && indice < _grupos.Count)
            {
                foreach (var o in _grupos[indice].Objetos)
                {
                    o.SetActive(true);
                    if (o.name.StartsWith("Gear"))
                        for (int i = 0; i < o.transform.childCount; i++)
                        {
                            var h = o.transform.GetChild(i).gameObject;
                            if (h.name == "Glow" || h.name == "Support" || h.name == "Shadow") { Guarda(h); h.SetActive(false); }
                        }
                }
            }
            var room = Campo(c, "_room") as Image;
            bool sala = indice < 0;
            if (room != null) { Guarda(room.gameObject); room.gameObject.SetActive(sala); }
            if (color != null) PonTelon(color.Value);
        }

        public static void Vuelve() { Restaura(); }

        /// <summary>Apaga (o enciende) las capas de la sombra blanda y del canto de los engranajes de la máquina: el cuadro «sin canto» para medir qué hace el canto.</summary>
        public static void SinCanto(bool sin)
        {
            var c = Controller(); if (c == null) return;
            var capa = Campo(c, "_machineLayer") as RectTransform; if (capa == null) return;
            foreach (var nombre in new[] { "Soft", "Canto" }) { var t = capa.Find(nombre); if (t != null) t.gameObject.SetActive(!sin); }
        }

        /// <summary>El costo de las capas: [iteraciones] cuadros en los que todos los engranajes giran un poco y se corre `AnimateScene` y se rehacen los lienzos; devuelve los ms por cuadro (CPU del Editor). Se compara con y sin canto.</summary>
        public static double MideCosto(int iteraciones, Func<double> renderUnaVez = null)
        {
            var c = Controller(); if (c == null) return -1;
            var tc = c.GetType();
            var maquina = tc.GetField("_mach", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(c); if (maquina == null) return -2;
            var gears = (System.Collections.IList)maquina.GetType().GetField("Gears").GetValue(maquina);
            var campoA = gears[0].GetType().GetField("A");
            var anima = tc.GetMethod("AnimateScene", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int w = 0; w < 20; w++) Un(c, anima, gears, campoA, w);                       // calentamiento
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iteraciones; i++) { Un(c, anima, gears, campoA, i + 20); if (renderUnaVez != null) renderUnaVez(); }
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds / iteraciones;
        }

        private static void Un(object c, MethodInfo anima, System.Collections.IList gears, FieldInfo campoA, int i)
        {
            foreach (var g in gears) campoA.SetValue(g, (float)campoA.GetValue(g) + 0.03f);
            anima.Invoke(c, new object[] { Time.realtimeSinceStartup, 0.016f });
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Usa todas las llaves tocando los primeros interruptores y toca uno más: deja el toque BLOQUEADO a la vista (la moneda con la aspa, el aviso y el contador).</summary>
        public static void Bloquea()
        {
            var c = Controller(); if (c == null) return; var tc = c.GetType();
            var mach = tc.GetField("_mach", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(c); if (mach == null) return;
            var sw = (System.Collections.IList)mach.GetType().GetField("Switches").GetValue(mach);
            var stage = mach.GetType().GetField("Stage").GetValue(mach); int keys = (int)stage.GetType().GetField("Keys").GetValue(stage);
            var ids = new List<int>(); foreach (var x in sw) ids.Add((int)x.GetType().GetField("Id").GetValue(x));
            var toggle = tc.GetMethod("ToggleSwitch", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < keys && i < ids.Count; i++) toggle.Invoke(c, new object[] { ids[i] });
            if (ids.Count > keys) toggle.Invoke(c, new object[] { ids[keys] });
        }

        /// <summary>Aprieta «Arrancar»: `_start.Touch.Press()` (el juego acepta el toque): la losa pasa a su imagen presionada y la letra y el ícono bajan con ella.</summary>
        public static void PresionaArrancar()
        {
            var c = Controller(); if (c == null) return; var tc = c.GetType();
            var start = tc.GetField("_start", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(c); if (start == null) return;
            var touch = start.GetType().GetField("Touch").GetValue(start); if (touch == null) return;
            touch.GetType().GetMethod("Press").Invoke(touch, null);
        }

        /// <summary>Toca el motor y dos correas (o lo que haya) con todas las llaves usadas: deja el cambio bloqueado a la vista (la moneda con la aspa) y el motor presionado.</summary>
        public static void Presiona()
        {
            var c = Controller(); if (c == null) return;
            var tc = c.GetType(); var marca = tc.GetMethod("MarkPressed", BindingFlags.NonPublic | BindingFlags.Instance);
            if (marca != null) marca.Invoke(c, new object[] { -1 });
        }

        /// <summary>Pone en pantalla la máquina de un nivel (con correas, barras y varias piezas) para sacarla: el cohete y la máquina se arman con ese nivel.</summary>
        public static bool CargaNivel(int nivel)
        {
            var c = Controller(); if (c == null) return false;
            var tc = TipoDe("NeuroVida.Games.Engranajes.EngranajesContract"); if (tc == null) return false;
            var rng = Campo(c, "_rng") as System.Random ?? new System.Random(7);
            var generate = tc.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static); if (generate == null) return false;
            object maquina = generate.Invoke(null, new object[] { nivel, rng, null });
            var show = c.GetType().GetMethod("ShowMachine", BindingFlags.NonPublic | BindingFlags.Instance); if (show == null) return false;
            show.Invoke(c, new[] { maquina });
            var fase = c.GetType().GetField("_phase", BindingFlags.NonPublic | BindingFlags.Instance);                          // «Arrancar» y las llaves solo se ven activos en la fase de juego
            if (fase != null) fase.SetValue(c, Enum.ToObject(fase.FieldType, 2));
            return true;
        }
    }
}
