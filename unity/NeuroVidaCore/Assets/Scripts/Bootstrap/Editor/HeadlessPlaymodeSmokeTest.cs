using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeuroVida.Bridge.EditorTools
{
    /// <summary>
    /// Smoke test headless: abre la escena de prueba del piloto, entra en Play, deja
    /// correr unos segundos reales (suficiente para countdown + primera presentación de
    /// secuencia) y reporta si hubo errores/excepciones en consola antes de salir. No
    /// simula toques de usuario -- solo confirma que el arranque real (parseo de JSON,
    /// seed del DDA, construcción de UI, síntesis de audio) no explota.
    ///
    /// Invocar con:
    /// Unity.exe -batchmode -nographics -projectPath &lt;path&gt;
    ///   -executeMethod NeuroVida.Bridge.EditorTools.HeadlessPlaymodeSmokeTest.Run
    /// (sin -quit -- este script llama a EditorApplication.Exit cuando termina).
    /// </summary>
    public static class HeadlessPlaymodeSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/SecuenciaPilotoTest.unity";
        private static float RunSeconds = 6f;

        private static int _errorCount;
        private static double _enteredPlayAt;
        private static bool _isRunning;
        private static bool _previousDomainReloadDisabled;
        private static EnterPlayModeOptions _previousOptions;

        /// <summary>Con la variable de entorno <c>NUBI_REDUCE_MOTION=1</c> el smoke corre con "quitar animaciones" activo
        /// (la config del juego lleva <c>reduce_motion = true</c>, como cuando el teléfono tiene la escala de animación en 0).</summary>
        private static bool ReduceMotionRequested => Environment.GetEnvironmentVariable("NUBI_REDUCE_MOTION") == "1";

        /// <summary>Smoke de «Rastro de luz» (id <c>secuencia</c>, el juego por defecto de la escena): cuenta regresiva y la primera muestra de la chispa.</summary>
        public static void Run() => RunGame(null, 10f);

        /// <summary>Rastro de luz con su tutorial guiado (<c>show_tutorial</c>): la tarjeta de Nubi maestra queda esperando un toque que el smoke no da.</summary>
        public static void RunTutorial() => RunGame(null, 12f, tutorial: true);

        /// <summary>Los tutoriales guiados de los otros tres juegos del inicio (la tarjeta de Nubi sigue sola y se juega el principio de la ronda guiada).</summary>
        public static void RunTutorialFreno() => RunGame("freno", 12f, tutorial: true);
        public static void RunTutorialAterrizaje() => RunGame("aterrizaje", 12f, tutorial: true);
        public static void RunTutorialMeteoros() => RunGame("meteoros", 12f, tutorial: true);
        /// <summary>Tinta o Palabra («Dos orillas») con su tutorial guiado (sin versión corta: no está en el inicio).</summary>
        public static void RunTutorialStroop() => RunGame("stroop", 12f, tutorial: true);
        /// <summary>«En la punta de la lengua» (id anagramas) con su tutorial guiado: la tarjeta de Nubi y la primera palabra, donde Nubi toca «¡La tengo!» y arma la palabra.</summary>
        public static void RunTutorialAnagramas() => RunGame("anagramas", 12f, tutorial: true);

        /// <summary>«Carga exacta» (id calculo) con su tutorial guiado: la tarjeta de Nubi y la carga fácil (3 celdas, solo sumas) con tres focos: celda, «+» y otra celda.</summary>
        public static void RunTutorialCalculo() => RunGame("calculo", 12f, tutorial: true);

        /// <summary>«Engranajes» (id engranajes) con su tutorial guiado: la tarjeta de Nubi y una máquina fácil con tres focos (la flecha del motor, la pieza de la pregunta y el botón correcto).</summary>
        public static void RunTutorialEngranajes() => RunGame("engranajes", 12f, tutorial: true);

        /// <summary>«Constelaciones» (id parejas) con su tutorial guiado: la tarjeta de Nubi, una luz, otra distinta, la pareja de la primera (el hueco cubre todas las dormidas), la línea dorada y la fila «De memoria».</summary>
        public static void RunTutorialParejas() => RunGame("parejas", 12f, tutorial: true);

        /// <summary>«Bodega de carga» (id bodega) con su tutorial guiado: la tarjeta de Nubi, la carga que entra por la esclusa y el robot la guarda, el toque en la escotilla pedida y un error de muestra.</summary>
        public static void RunTutorialBodega() => RunGame("bodega", 12f, tutorial: true);

        /// <summary>La versión corta del inicio («Tu punto de partida»: <c>assessment</c>) de Freno, Aterrizaje y Meteoros.</summary>
        public static void RunCortoFreno() => RunGame("freno", 10f, assessment: true);
        public static void RunCortoAterrizaje() => RunGame("aterrizaje", 10f, assessment: true);
        public static void RunCortoMeteoros() => RunGame("meteoros", 10f, assessment: true);

        /// <summary>Mismo smoke test pero con "Tinta o Palabra" (Stroop) como juego.</summary>
        public static void RunStroop() => RunGame("stroop", 9f);

        /// <summary>Mismo smoke test pero con «En la punta de la lengua» (id anagramas) como juego.</summary>
        public static void RunAnagramas() => RunGame("anagramas", 9f);

        /// <summary>Mismo smoke test pero con «Carga exacta» (id calculo) como juego.</summary>
        public static void RunCalculo() => RunGame("calculo", 9f);

        /// <summary>Mismo smoke test pero con «Engranajes» (id engranajes) como juego.</summary>
        public static void RunEngranajes() => RunGame("engranajes", 24f);

        /// <summary>Mismo smoke test pero con «Bodega de carga» (id bodega) como juego (se juega sola: pedidos pares acertados, impares con un error primero).</summary>
        public static void RunBodega() => RunGame("bodega", 30f);

        /// <summary>Mismo smoke test pero con «Piloto Estelar: la ruta de las balizas» como juego (se juega sola: guía la nave por la ruta, atrapa las señales de la misión y de vez en cuando se equivoca o se sale; los sectores duran 6 s en el smoke, así que pasa por los tres y por la pantalla final).</summary>
        public static void RunPiloto() => RunGame("piloto", 30f);

        /// <summary>«Piloto Estelar» con su tutorial guiado: la tarjeta de Nubi, «Esta es tu misión», «Desliza aquí para guiar la nave» (con las señales ya apareciendo), el toque «de verdad» sobre una señal de la misión, las parecidas y los sectores.</summary>
        public static void RunTutorialPiloto() => RunGame("piloto", 14f, tutorial: true);

        /// <summary>Mismo smoke test pero con «Rescate relámpago» (id radar) como juego (se juega solo: marca las cápsulas que eran, de vez en cuando una que no estaba, y toca «¡Rescatar!»; son 4 rondas y la última es una lluvia).</summary>
        public static void RunRadar() => RunGame("radar", 30f);

        /// <summary>«Rescate relámpago» con su tutorial guiado: la tarjeta de Nubi, «Mira el radar», los dos toques «de verdad» en el tablero, «Ahora toca ¡Rescatar!», la nave y la roca gris.</summary>
        public static void RunTutorialRadar() => RunGame("radar", 14f, tutorial: true);

        /// <summary>Mismo smoke test pero con Satélites como juego.</summary>
        public static void RunSatelites() => RunGame("satelites", 55f);

        /// <summary>«Satélites: enciende tu planeta» (id satelites) con su tutorial guiado: la tarjeta de Nubi, «Algunos traen un mensaje», mirar cómo giran, los dos toques «de verdad» sobre los satélites con mensaje y «¡Cada mensaje enciende una luz!».</summary>
        public static void RunTutorialSatelites() => RunGame("satelites", 14f, tutorial: true);

        /// <summary>Mismo smoke test pero con Freno de Emergencia como juego.</summary>
        public static void RunFreno() => RunGame("freno", 9f);

        /// <summary>Mismo smoke test pero con Lluvia de meteoros como juego.</summary>
        public static void RunMeteoros() => RunGame("meteoros", 9f);

        /// <summary>Mismo smoke test pero con ¿Verdad o disparate? como juego.</summary>
        public static void RunDisparate() => RunGame("disparate", 9f);

        /// <summary>Mismo smoke test pero con Cosecha de palabras como juego.</summary>
        public static void RunCosecha() => RunGame("cosecha", 9f);

        /// <summary>Mismo smoke test pero con La estrella intrusa como juego.</summary>
        public static void RunIntrusa() => RunGame("intrusa", 9f);

        /// <summary>Mismo smoke test pero con Aterrizaje Lunar como juego.</summary>
        public static void RunAterrizaje() => RunGame("aterrizaje", 9f);

        /// <summary>Mismo smoke test pero con Acoplamiento como juego.</summary>
        public static void RunAcoplamiento() => RunGame("acoplamiento", 9f);


        /// <summary>Mismo smoke test pero con Bitácora de Misión como juego.</summary>
        public static void RunBitacora() => RunGame("bitacora", 9f);

        /// <summary>Mismo smoke test pero con Rumbo a Casa como juego.</summary>
        /// <summary>«La estación de correo» (id correo) con su tutorial guiado: la tarjeta de Nubi, el buzón del sello, la hoja del día, la carta dorada a la caja fuerte, el reloj tapado y el faro a la hora.</summary>
        public static void RunTutorialCorreo() => RunGame("correo", 12f, tutorial: true);

        /// <summary>«Cosecha de palabras» (id cosecha) con su tutorial guiado: la tarjeta de Nubi y una ronda de práctica con las letras de CASA (la primera ficha, las otras tres, Sembrar, mirar cómo brota y Borrar), con un toque «de verdad» en cada hueco.</summary>
        public static void RunTutorialCosecha() => RunGame("cosecha", 12f, tutorial: true);

        /// <summary>«¿Verdad o disparate?» (id disparate) con su tutorial guiado: la tarjeta de Nubi y tres frases de práctica (una verdad, un disparate y una donde se ve la señal), con un toque «de verdad» en cada botón.</summary>
        public static void RunTutorialDisparate() => RunGame("disparate", 12f, tutorial: true);

        /// <summary>«La estrella intrusa» (id intrusa) con su tutorial guiado: la tarjeta de Nubi y una ronda de práctica (cuatro frutas y un zapato): el aviso, el toque en la intrusa (con un toque «de verdad»), la chispa que dibuja la figura y el atlas.</summary>
        public static void RunTutorialIntrusa() => RunGame("intrusa", 12f, tutorial: true);

        public static void RunRumbo() => RunGame("rumbo", 9f);

        /// <summary>Mismo smoke test pero con «La estación de correo» (id correo) como juego (se juega sola: clasifica, guarda las señal, mira el reloj y enciende el faro; los días duran 12 s en el smoke).</summary>
        public static void RunCorreo() => RunGame("correo", 85f);

        /// <summary>Mismo smoke test pero con «Constelaciones» (id parejas) como juego (se juega sola: explora, recuerda y de vez en cuando se le escapa una compañera).</summary>
        public static void RunParejas() => RunGame("parejas", 30f);

        // ------------------------------------------------------------------ encadenado en UN solo proceso de Unity

        /// <summary>Nombre de cada juego (el sufijo de su método Run*) con su id y los segundos de partida del smoke.</summary>
        private static readonly (string Name, string Id, float Seconds)[] Catalog =
        {
            ("Run", null, 10f), ("Tutorial", "secuencia", 12f),
            ("TutorialFreno", "freno", 12f), ("TutorialAterrizaje", "aterrizaje", 12f), ("TutorialMeteoros", "meteoros", 12f), ("TutorialStroop", "stroop", 12f), ("TutorialAnagramas", "anagramas", 12f), ("TutorialCalculo", "calculo", 12f), ("TutorialEngranajes", "engranajes", 12f), ("TutorialBodega", "bodega", 12f), ("TutorialParejas", "parejas", 12f), ("TutorialCorreo", "correo", 12f), ("TutorialCosecha", "cosecha", 12f), ("TutorialDisparate", "disparate", 12f), ("TutorialIntrusa", "intrusa", 12f), ("TutorialSatelites", "satelites", 14f), ("TutorialPiloto", "piloto", 14f), ("TutorialRadar", "radar", 14f),
            ("CortoFreno", "freno", 10f), ("CortoAterrizaje", "aterrizaje", 10f), ("CortoMeteoros", "meteoros", 10f), ("Stroop", "stroop", 9f), 
            ("Calculo", "calculo", 9f), ("Engranajes", "engranajes", 24f), ("Bodega", "bodega", 30f), ("Anagramas", "anagramas", 9f),
            ("Parejas", "parejas", 30f), ("Piloto", "piloto", 30f), ("PilotoReto", "piloto", 30f), ("PilotoMudo", "piloto", 30f), ("Radar", "radar", 30f), ("RadarReto", "radar", 30f), ("RadarMudo", "radar", 30f), ("Satelites", "satelites", 55f),
            ("Freno", "freno", 9f), ("Aterrizaje", "aterrizaje", 9f), ("Acoplamiento", "acoplamiento", 9f),
            ("Bitacora", "bitacora", 9f), ("Rumbo", "rumbo", 9f), ("Correo", "correo", 85f), ("Meteoros", "meteoros", 9f),
            ("Disparate", "disparate", 9f), ("Cosecha", "cosecha", 9f), ("Intrusa", "intrusa", 9f),
        };

        /// <summary>
        /// Una corrida «Pantalla*» por juego SIN tutorial, en forma de TELÉFONO (lienzo de 1080x2400): la primera pantalla jugable y la pausa (el primer panel con botones), con la guardia de textos <see cref="ScanTextPlacement"/>. El Editor sin
        /// gráficos es una ventana de 640x480 (4:3), donde la disposición de un juego pensado para teléfono no cabe: ahí la guardia no sirve, por eso va aparte. «PantallaCorreo» dura más (40 s) y corre en dos formas
        /// (20:9 y 16:9) para ver la hoja, la partida y el resumen con sus botones.
        /// </summary>
        private static readonly (string Name, string Id, float Seconds)[] PantallaCatalog = BuildPantallaCatalog();

        private static (string Name, string Id, float Seconds)[] BuildPantallaCatalog()
        {
            // los juegos con tutorial ya corren en forma de teléfono (3 formas) con la guardia de textos: su primera pantalla jugable y la pausa se ven ahí, sin repetirlos aquí (salvo Correo, que tiene hoja, resumen y pantalla final propios)
            var withTutorial = new HashSet<string>();
            foreach (var g in Catalog) if (g.Name.StartsWith("Tutorial")) withTutorial.Add(g.Id ?? "secuencia");
            var list = new List<(string, string, float)>();
            foreach (var g in Catalog)
            {
                if (g.Name.StartsWith("Tutorial") || g.Name.StartsWith("Corto")) continue;
                if (g.Name != "Correo" && g.Name != "Satelites" && g.Name != "Piloto" && g.Name != "Radar" && withTutorial.Contains(g.Id ?? "secuencia")) continue;
                list.Add(("Pantalla" + g.Name, g.Id, g.Name == "Correo" ? 40f : g.Name == "Satelites" ? 55f : g.Name == "Piloto" || g.Name == "Radar" ? 30f : Mathf.Min(g.Seconds, 9f)));
            }
            return list.ToArray();
        }

        /// <summary>
        /// Una corrida «Aviso*» por juego con aviso (Tarea 55): a los 5,5 s el smoke MUESTRA un aviso de prueba de dos renglones y la guardia <see cref="ScanToastCover"/> comprueba que no tapa ningún texto, botón ni el estímulo del juego (las mismas reglas con que el aviso se
        /// acomoda: <see cref="NeuroVida.Games.Shared.ToastPlacement"/>), en la pantalla más alta y en la más baja. Bitácora (retirada) no entra.
        /// </summary>
        private static readonly (string Name, string Id, float Seconds)[] AvisoCatalog =
        {
            ("AvisoAcoplamiento", "acoplamiento", 10.5f), ("AvisoAnagramas", "anagramas", 10.5f), ("AvisoAterrizaje", "aterrizaje", 10.5f), ("AvisoCosecha", "cosecha", 10.5f), ("AvisoDisparate", "disparate", 10.5f),
            ("AvisoFreno", "freno", 10.5f), ("AvisoIntrusa", "intrusa", 10.5f), ("AvisoMeteoros", "meteoros", 10.5f),
            ("AvisoRumbo", "rumbo", 10.5f), ("AvisoStroop", "stroop", 10.5f),
        };

        /// <summary>
        /// Una corrida «HowTo*» por juego con el tutorial nuevo (Tarea 56): a los 9 s de partida el smoke abre «Cómo se juega» (la ronda guiada sobre la partida en curso, que avanza sola) y comprueba que termina, que no hay errores y que la
        /// partida vuelve a estar en marcha (<c>CanShowHowTo</c> otra vez). Es lo que prueba que la partida se aparta y se retoma bien.
        /// </summary>
        private static readonly (string Name, string Id, float Seconds)[] HowToCatalog =
        {
            ("HowToCosecha", "cosecha", 60f), ("HowToDisparate", "disparate", 60f), ("HowToIntrusa", "intrusa", 60f), ("HowToSatelites", "satelites", 60f), ("HowToPiloto", "piloto", 60f), ("HowToRadar", "radar", 60f),
        };

        private static bool _howToStarted, _howToDone;
        private static double _howToAt;

        /// <summary>
        /// Con <c>NUBI_STALE_PAUSE=1</c> el smoke reproduce lo que pasó en el teléfono (Tarea 58): «Salir» del menú de pausa deja el menú a la vista y, al cargarse el juego siguiente, la escena se destruye con el menú adentro. Aquí se
        /// abre un menú de pausa, se destruye y se deja el reloj en su lugar: si el estado de «pausa abierta» sobrevive, el foco de Nubi ya no recibe ningún toque.
        /// </summary>
        private static bool StalePauseRequested => Environment.GetEnvironmentVariable("NUBI_STALE_PAUSE") == "1";
        private static bool _staleDone;

        private static void SimulateStalePause()
        {
            _staleDone = true;
            var host = new GameObject("StalePauseHost");
            var menu = NeuroVida.Games.Shared.PauseMenu.Create(host.transform, null, null, null);
            menu.Show();
            UnityEngine.Object.DestroyImmediate(host);                  // la escena que se recarga se lleva el menú
            NeuroVida.Games.Shared.GameClock.Reset();
            Debug.Log($"[SmokeTest] pausa vieja simulada: PauseMenu.Open={NeuroVida.Games.Shared.PauseMenu.Open}");
        }

        private static void DriveHowTo(double played)
        {
            var controller = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.GameControllerBase>();
            if (controller == null) return;
            if (!_howToStarted)
            {
                if (played < 9.0) return;
                _howToStarted = true;
                _howToAt = played;
                if (!controller.CanShowHowTo)
                {
                    _errorCount++;
                    Debug.Log($"[SmokeTest] Error capturado: «Cómo se juega» no está disponible a los 9 s de partida en {_current.Name}");
                    return;
                }
                controller.ShowHowTo();
                Debug.Log($"[SmokeTest] {_current.Name}: «Cómo se juega» abierto a los {played:0.#} s");
                return;
            }
            if (!_howToDone && played > _howToAt + 3.0 && controller.CanShowHowTo)
            {
                _howToDone = true;
                Debug.Log($"[SmokeTest] {_current.Name}: «Cómo se juega» terminó y la partida volvió ({played - _howToAt:0.#} s)");
            }
        }

        private static void CheckHowTo()
        {
            if (!_current.Name.StartsWith("HowTo")) return;
            if (_howToDone) return;
            _errorCount++;
            Debug.Log($"[SmokeTest] Error capturado: «Cómo se juega» no terminó o la partida no volvió a estar en marcha en {_current.Name}");
        }

        /// <summary>Juegos cuyo aviso puede quedar sobre el juego, con el porqué (lista corta).</summary>
        private static readonly (string Id, string Why)[] ToastCoverExceptions =
        {
            ("rumbo", "su señal, su baliza y su nave se mueven por todo el mapa (la pantalla alta tiene una franja libre; la corta no) y Rumbo se revisa en la Etapa 2 de la hoja de ruta; su aviso va arriba, debajo de la consigna"),
        };

        private static readonly Queue<(string Name, string Id, float Seconds)> _queue = new Queue<(string, string, float)>();
        private static readonly Queue<int> _heights = new Queue<int>();       // alto de pantalla de cada corrida de _queue (0 = el de la ventana del Editor)
        private static (string Name, string Id, float Seconds) _current;
        private static int _currentHeight;
        private static int _listOk, _listTotal;
        private static bool _listRunning, _listPlaying, _listAnyFailed;

        /// <summary>
        /// Smoke de VARIOS juegos en un solo proceso (abrir Unity 23 veces es lo que más tarda). La lista sale de la variable de entorno
        /// NUBI_SMOKE_GAMES o del argumento <c>-smokeGames</c> (nombres separados por comas: Meteoros,Intrusa; vacía = los 23). Cada juego
        /// entra a Play, corre su tiempo, sale, y entra el siguiente (sin domain reload, igual que el smoke de uno solo).
        /// </summary>
        public static void RunList()
        {
            var raw = Environment.GetEnvironmentVariable("NUBI_SMOKE_GAMES");
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++) if (args[i] == "-smokeGames") raw = args[i + 1];

            _queue.Clear();
            _heights.Clear();
            if (string.IsNullOrWhiteSpace(raw))
            {
                foreach (var g in Catalog) Enqueue(g);
                foreach (var g in PantallaCatalog) Enqueue(g);
                foreach (var g in AvisoCatalog) Enqueue(g);
                foreach (var g in HowToCatalog) Enqueue(g);
            }
            else
            {
                foreach (var part in raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = part.Trim();
                    var found = false;
                    foreach (var g in Catalog)
                    {
                        if (!string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                        Enqueue(g);
                        found = true;
                        break;
                    }
                    if (!found)
                        foreach (var g in AvisoCatalog)
                        {
                            if (!string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                            Enqueue(g);
                            found = true;
                            break;
                        }
                    if (!found)
                        foreach (var g in HowToCatalog)
                        {
                            if (!string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                            Enqueue(g);
                            found = true;
                            break;
                        }
                    if (!found)
                        foreach (var g in PantallaCatalog)
                        {
                            if (!string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                            Enqueue(g);
                            found = true;
                            break;
                        }
                    if (!found)
                    {
                        Debug.Log($"[SmokeTest] FALLÓ -- juego desconocido en la lista: {name}");
                        EditorApplication.Exit(2);
                        return;
                    }
                }
            }

            _listTotal = _queue.Count;
            _listOk = 0;
            _listAnyFailed = false;
            EditorSceneManager.OpenScene(ScenePath);
            _previousDomainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled;
            _previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Application.logMessageReceived += OnLog;
            _listRunning = true;
            _listPlaying = false;
            EditorApplication.update += OnUpdateList;
            StartNextInList();
        }

        /// <summary>Alto de las tres formas de pantalla (ancho 1080) con que se revisa el tutorial de cada juego: 20:9 (el teléfono de Ricardo), 18:9 y 16:9.</summary>
        private static readonly int[] AuditHeights = { 2400, 2160, 1920 };

        /// <summary>Los tutoriales guiados corren en las tres formas de pantalla: <see cref="NeuroVida.Games.Shared.NubiCoach"/> registra cada paso y se comprueba que Nubi y su globo no tapen nada.</summary>
        private static void Enqueue((string Name, string Id, float Seconds) g)
        {
            if (g.Name.StartsWith("Tutorial"))
            {
                var only = Environment.GetEnvironmentVariable("NUBI_COACH_HEIGHTS");      // p. ej. «2160,1920»: repite solo esas formas
                foreach (var h in AuditHeights)
                {
                    if (!string.IsNullOrEmpty(only) && Array.IndexOf(only.Split(','), h.ToString()) < 0) continue;
                    _queue.Enqueue((g.Name, g.Id, g.Seconds + CoachAuditExtraSeconds)); _heights.Enqueue(h);
                }
                return;
            }
            if (g.Name.StartsWith("Aviso"))
            {
                foreach (var h in new[] { 2400, 1920 }) { _queue.Enqueue(g); _heights.Enqueue(h); }
                return;
            }
            if (g.Name.StartsWith("Pantalla"))
            {
                // la guardia de textos en forma de TELÉFONO (ver PantallaCatalog)
                foreach (var h in g.Name == "PantallaCorreo" ? new[] { 2400, 1920 } : new[] { 2400 }) { _queue.Enqueue(g); _heights.Enqueue(h); }
                return;
            }
            _queue.Enqueue(g);
            _heights.Enqueue(0);
        }

        /// <summary>Más tiempo para que la ronda guiada llegue a sus últimos pasos (cada foco de «tocar» sigue solo a los 1,2 s en el Editor).</summary>
        private const float CoachAuditExtraSeconds = 22f;

        private static void StartNextInList()
        {
            _current = _queue.Dequeue();
            _currentHeight = _heights.Dequeue();
            _screenLogged = false;
            _textGuardSeen.Clear();
            _guardPrev.Clear();
            _guardOk.Clear();
            _hudNow.Clear();
            _hudPrev.Clear();
            _hudSeen.Clear();
            _toastNow.Clear();
            _toastPrev.Clear();
            _toastSeen.Clear();
            _sampleToastShown = false;
            _howToStarted = _howToDone = false;
            _staleDone = false;
            NeuroVida.Games.Shared.NubiCoach.EditorProbe = _current.Name.StartsWith("Tutorial");        // toque «de verdad» en el hueco de cada paso de Tocar (Tarea 58)
            NeuroVida.Games.Shared.NubiCoach.ProbeFailures.Clear();
            NeuroVida.Games.Shared.NubiCoach.RealTouchesAccepted = 0;
            NeuroVida.Games.Shared.GuidedTutorial.EditorPressFrame = -1;      // un toque de prueba de la corrida anterior no cuenta en esta
            _textGuardAt = 0.0;
            _entryStartAt = EditorApplication.timeSinceStartup;
            _pauseShowStage = 0;
            NeuroVida.Games.Shared.NubiCoach.AuditEnabled = _current.Name.StartsWith("Tutorial");
            NeuroVida.Games.Shared.NubiCoach.AuditGame = _current.Id ?? "secuencia";
            NeuroVida.Games.Shared.NubiCoach.AuditSteps.Clear();
            _auditCanvases.Clear();
            _errorCount = 0;
            _enteredPlayAt = 0;
            _pauseStage = 0;
            _listPlaying = true;
            EditorPlaytestBootstrap.GameIdOverride = _current.Id;
            EditorPlaytestBootstrap.ReduceMotionOverride = ReduceMotionRequested;
            // «Tutorial*» = con la tarjeta de Nubi (sigue sola en el Editor) y la ronda guiada; «Corto*» = la versión corta del inicio (assessment)
            bool tutorial = _current.Name.StartsWith("Tutorial");
            EditorPlaytestBootstrap.ShowTutorialOverride = tutorial;
            EditorPlaytestBootstrap.TimedOverride = _current.Name == "PilotoReto" || _current.Name == "RadarReto";            // el Reto (con reloj) de Piloto; las demás corridas son Precisión
            EditorPlaytestBootstrap.SoundOffOverride = _current.Name.EndsWith("Mudo");                                       // «…Mudo»: con «Efectos de sonido» apagado no tiene que sonar NADA (Tarea 64)
            _audioSampleAt = 0.0;
            _audioHeard = _audioReported = false;
            EditorPlaytestBootstrap.AssessmentOverride = _current.Name.StartsWith("Corto");
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoContinue = tutorial || _current.Name.StartsWith("HowTo");      // «HowTo*» no arranca con tutorial: lo abre el smoke desde la partida
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = _current.Name == "PilotoMudo" || _current.Name == "RadarMudo" || _current.Name == "Engranajes" || _current.Name == "Bodega" || _current.Name == "Parejas" || _current.Name == "Correo" || _current.Name == "Satelites" || _current.Name == "PantallaSatelites" || _current.Name == "Piloto" || _current.Name == "PilotoReto" || _current.Name == "PantallaPiloto" || _current.Name == "Radar" || _current.Name == "RadarReto" || _current.Name == "PantallaRadar" || _current.Name == "PantallaCorreo" || _current.Name == "PantallaEngranajes" || _current.Name == "PantallaBodega" || _current.Name == "PantallaParejas";      // la partida de Engranajes se juega sola (máquinas pares: la solución; impares: sin tocar)
            EditorApplication.EnterPlaymode();
        }

        // ------------------------------------------------------------------ la pausa de cada juego responde

        private static int _pauseStage;
        private static double _pauseAt;

        /// <summary>
        /// Después de los segundos de partida, abre la pausa del juego y comprueba que SE PUEDE TOCAR: que haya un EventSystem y que, en el centro de cada botón («Continuar»,
        /// «Reiniciar», «Cómo se juega» si está y «Salir»), lo primero que recibe el toque sea ese botón (así también se atrapa algo del juego que tape la pausa). Después toca
        /// «Continuar» y comprueba que la pausa se cierra y el reloj de juego sigue. Devuelve true cuando terminó (cada llamada es un paso, una por cuadro).
        /// </summary>
        private static bool PauseCheckDone()
        {
            double now = EditorApplication.timeSinceStartup;
            switch (_pauseStage)
            {
                case 0:
                {
                    var entry = UnityEngine.Object.FindObjectOfType<NeuroVida.Bridge.GameEntryPoint>();
                    if (entry == null) { PauseFail("no hay GameEntryPoint para abrir la pausa"); _pauseStage = 9; return true; }
                    var show = typeof(NeuroVida.Bridge.GameEntryPoint).GetMethod("ShowPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    show.Invoke(entry, null);
                    _pauseAt = now;
                    _pauseStage = 1;
                    return false;
                }
                case 1:
                {
                    if (now - _pauseAt < 0.6) return false;
                    var menu = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.PauseMenu>();
                    if (menu == null || !menu.IsShown) { PauseFail("la pausa no quedó visible"); _pauseStage = 9; return true; }
                    var es = UnityEngine.EventSystems.EventSystem.current;
                    if (es == null) { PauseFail("no hay EventSystem: ningún botón de la pausa responde"); _pauseStage = 9; return true; }
                    UnityEngine.GameObject continueButton = null;
                    foreach (var kv in menu.VisibleButtons())
                    {
                        var screen = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, kv.Value.TransformPoint(kv.Value.rect.center));
                        // la ventana de juego del Editor es mucho más ancha que un teléfono: con cuatro botones el último puede quedar fuera de ella (en un teléfono entra de sobra)
                        if (screen.x < 0f || screen.y < 0f || screen.x > UnityEngine.Screen.width || screen.y > UnityEngine.Screen.height)
                        {
                            Debug.Log("[SmokeTest] aviso: «" + kv.Key + "» queda fuera de la ventana del Editor (" + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height + "): no se prueba");
                            continue;
                        }
                        var data = new UnityEngine.EventSystems.PointerEventData(es) { position = screen };
                        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                        es.RaycastAll(data, hits);
                        if (hits.Count == 0 || !hits[0].gameObject.transform.IsChildOf(kv.Value))
                            PauseFail("el toque en el centro de «" + kv.Key + "» no llega al botón (primero recibe: " + (hits.Count == 0 ? "nada" : hits[0].gameObject.name) + ")");
                        else if (kv.Key.Contains("Continuar")) continueButton = hits[0].gameObject;
                    }
                    if (continueButton != null)
                    {
                        var click = new UnityEngine.EventSystems.PointerEventData(es);
                        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(continueButton, click, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                    }
                    _pauseAt = now;
                    _pauseStage = 2;
                    return false;
                }
                case 2:
                {
                    if (now - _pauseAt < 0.3) return false;
                    var menu = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.PauseMenu>();
                    if (menu != null && menu.IsShown) PauseFail("«Continuar» no cerró la pausa");
                    if (NeuroVida.Games.Shared.GameClock.Paused) PauseFail("«Continuar» no reanudó el reloj de juego");
                    _pauseStage = 9;
                    return true;
                }
                default:
                    return true;
            }
        }

        private static int _pauseShowStage;
        private static double _pauseShowAt;

        /// <summary>Abre la pausa del juego, la deja 1 s a la vista (la guardia de textos revisa sus botones) y la CIERRA con «Continuar» (si se saliera de Play con la pausa abierta, el reloj de juego quedaría detenido para la corrida
        /// siguiente: el smoke corre sin recargar el dominio y ese estado estático se hereda). Cada llamada es un paso, una por cuadro.</summary>
        private static bool PauseShowDone()
        {
            double now = EditorApplication.timeSinceStartup;
            switch (_pauseShowStage)
            {
                case 0:
                {
                    var entry = UnityEngine.Object.FindObjectOfType<NeuroVida.Bridge.GameEntryPoint>();
                    if (entry != null)
                    {
                        var show = typeof(NeuroVida.Bridge.GameEntryPoint).GetMethod("ShowPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        show.Invoke(entry, null);
                    }
                    _pauseShowAt = now;
                    _pauseShowStage = 1;
                    return false;
                }
                case 1:
                {
                    if (now - _pauseShowAt < 1.0) return false;
                    var menu = UnityEngine.Object.FindObjectOfType<NeuroVida.Games.Shared.PauseMenu>();
                    if (menu != null && menu.IsShown)
                        foreach (var kv in menu.VisibleButtons())
                            if (kv.Key.Contains("Continuar"))
                            {
                                UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(kv.Value.gameObject, new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current), UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                                break;
                            }
                    _pauseShowAt = now;
                    _pauseShowStage = 2;
                    return false;
                }
                default:
                    if (now - _pauseShowAt < 0.3) return false;
                    if (NeuroVida.Games.Shared.GameClock.Paused) { _errorCount++; Debug.Log("[SmokeTest] Error capturado: pausa de " + _current.Name + ": «Continuar» no reanudó el reloj de juego (corrida en forma de teléfono)"); }
                    return true;
            }
        }

        // ------------------------------------------------------------------ guardia de textos: ¿cada texto está donde se ve?

        /// <summary>
        /// Textos que a propósito se dibujan FUERA de la imagen de la que cuelgan (rótulos de una barra o de una bandera): (juego, final de la ruta del objeto). Solo se exceptúa la regla del padre; la de «dentro de la pantalla» sigue valiendo.
        /// Cualquier otro texto fuera de lugar, en cualquier juego, hace FALLAR el smoke.
        /// </summary>
        private static readonly (string Id, string PathEnd)[] TextGuardExceptions =
        {
            ("aterrizaje", "Ruler/Min"),     // los extremos «0» y «10» de la regla, debajo de la barra
            ("aterrizaje", "Ruler/Max"),
            ("aterrizaje", "Flag/Label"),    // el número de la bandera, arriba de ella
        };

        /// <summary>Tamaño mínimo de letra de TODO texto visible, en dp (el lienzo de los juegos mide 1080 unidades de ancho = 360 dp: 3 unidades por dp). Pensamos en mayores: nada más chico que esto (CLAUDE.md, «Reglas que no se rompen»).</summary>
        private const float MinFontDp = 14f;

        /// <summary>Textos que a propósito van más chicos que <see cref="MinFontDp"/>: (juego, final de la ruta del objeto, por qué). Cualquier otro texto bajo 14 dp, en cualquier juego, hace FALLAR el smoke. Mantenerla CORTA y justificada.</summary>
        private static readonly (string Id, string PathEnd, string Why)[] TextSizeExceptions =
        {
            ("*", "CountdownScreen/StyleStamp", "marca de versión de las builds de depuración (CountdownScreen.StyleStamp): no es parte del juego y se quita antes de publicar"),
            ("bitacora", "*", "Bitácora de Misión está RETIRADA de la app (5-oct): ya no se ve; su código y su arranque en el smoke se conservan sin rehacerle la letra"),
        };

        private static bool SizeIsException(UnityEngine.UI.Text t)
        {
            string path = GuardPath(t.transform);
            foreach (var e in TextSizeExceptions)
                if ((e.Id == "*" || e.Id == (_current.Id ?? "secuencia")) && (e.PathEnd == "*" || path.EndsWith(e.PathEnd, StringComparison.Ordinal))) return true;
            return false;
        }

        private static bool GuardIsException(string rule, UnityEngine.UI.Text t)
        {
            if (!rule.StartsWith("fuera de su padre")) return false;
            string path = GuardPath(t.transform);
            foreach (var e in TextGuardExceptions)
                if (e.Id == (_current.Id ?? "secuencia") && path.EndsWith(e.PathEnd, StringComparison.Ordinal)) return true;
            return false;
        }

        private static readonly HashSet<string> _textGuardSeen = new HashSet<string>();
        private static double _textGuardAt;
        private static double _entryStartAt;
        private static readonly Vector3[] _guardCorners = new Vector3[4];
        private static readonly Dictionary<string, Rect> _guardPrev = new Dictionary<string, Rect>();
        private static readonly HashSet<string> _guardNow = new HashSet<string>();
        /// <summary>Texto → huella de lo que se revisó (su rect, su texto y el rect del padre) la última vez que estaba bien: mientras no cambie, no se vuelve a medir (medir el texto es lo caro).</summary>
        private static readonly Dictionary<int, int> _guardOk = new Dictionary<int, int>();

        /// <summary>Un texto solo se informa si queda fuera de lugar en DOS revisiones seguidas (0,4 s) y en el mismo sitio (≤ 2 unidades): lo que está animándose (una burbuja que entra, un número que crece) pasa por posiciones raras de camino y no es el error;
        /// un texto mal posicionado se queda quieto donde está.</summary>
        private static void GuardCandidate(bool strict, string rule, UnityEngine.UI.Text t, Rect drawn, Rect screen, string shape, Rect? parent)
        {
            if (GuardIsException(rule, t)) return;
            string key = rule + "|" + t.GetInstanceID();
            _guardNow.Add(key);
            if (_guardPrev.TryGetValue(key, out var prev) && Mathf.Abs(prev.x - drawn.x) <= 2f && Mathf.Abs(prev.y - drawn.y) <= 2f && Mathf.Abs(prev.width - drawn.width) <= 2f)
                GuardReport(strict, rule, t, drawn, screen, shape, parent);
            _guardPrev[key] = drawn;
        }

        private static Rect GuardWorldRect(RectTransform rt)
        {
            rt.GetWorldCorners(_guardCorners);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var c in _guardCorners) { x0 = Mathf.Min(x0, c.x); y0 = Mathf.Min(y0, c.y); x1 = Mathf.Max(x1, c.x); y1 = Mathf.Max(y1, c.y); }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        private static string GuardPath(Transform t)
        {
            var parts = new List<string>();
            for (var c = t; c != null && parts.Count < 6; c = c.parent) parts.Add(c.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>El rect donde se DIBUJA un texto (en el mundo): el ancho y el alto reales del texto (<c>preferredWidth</c> y <c>preferredHeight</c> por la escala), acomodados según su alineación dentro de su rect y sin pasar del rect si
        /// se parte en líneas. El rect entero de un texto suele ser más grande que lo que se ve (un rótulo alineado a la izquierda dentro de un rect de 300 dp): medir el rect daría falsos avisos.</summary>
        private static Rect GuardTextRect(UnityEngine.UI.Text t)
        {
            var rect = GuardWorldRect(t.rectTransform);
            var sc = t.transform.lossyScale;
            float w = t.preferredWidth * Mathf.Abs(sc.x), h = t.preferredHeight * Mathf.Abs(sc.y);
            if (t.horizontalOverflow == HorizontalWrapMode.Wrap) w = Mathf.Min(w, rect.width);
            if (t.verticalOverflow == VerticalWrapMode.Truncate) h = Mathf.Min(h, rect.height);
            if (w <= 0f || h <= 0f) return rect;
            float x0, y0;
            switch (t.alignment)
            {
                case TextAnchor.UpperLeft: case TextAnchor.MiddleLeft: case TextAnchor.LowerLeft: x0 = rect.xMin; break;
                case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight: x0 = rect.xMax - w; break;
                default: x0 = rect.center.x - w / 2f; break;
            }
            switch (t.alignment)
            {
                case TextAnchor.UpperLeft: case TextAnchor.UpperCenter: case TextAnchor.UpperRight: y0 = rect.yMax - h; break;
                case TextAnchor.LowerLeft: case TextAnchor.LowerCenter: case TextAnchor.LowerRight: y0 = rect.yMin; break;
                default: y0 = rect.center.y - h / 2f; break;
            }
            return new Rect(x0, y0, w, h);
        }

        /// <summary>
        /// Dos reglas sobre todo <c>Text</c> activo, con texto y visible (opacidad &gt; 0), solo en las corridas en forma de teléfono («Tutorial*» y «Pantalla*»; en la ventana de 640x480 un juego de teléfono no cabe y daría falsos avisos):
        /// (1) lo que se dibuja del texto cae DENTRO de la pantalla (el lienzo raíz); (2) si su padre es un <c>Image</c> (un botón o una píldora), el centro de lo dibujado cae dentro del rect del padre. Atrapa el error de posicionar
        /// un hijo con coordenadas de la capa (queda desplazado y el botón se ve vacío), que las pruebas de lógica no ven. Se revisa cada 0,4 s durante toda la corrida (la primera pantalla jugable, la pausa y cada panel con botón que el
        /// juego abra). (4) el texto no queda cortado por el alto de su caja (con «Truncate» una línea que no cabe desaparece entera); (3) la letra mide al menos <see cref="MinFontDp"/> dp (14: el tamaño efectivo, con el ajuste automático ya hecho). Y aparte, <see cref="ScanHudCover"/>: nada tapa el HUD. Cada texto se informa una vez por corrida, con el juego, el objeto y la posición. Hace FALLAR el smoke en cualquier juego, salvo las excepciones de <see cref="TextGuardExceptions"/>.
        /// </summary>
        private static void ScanTextPlacement()
        {
            if (_currentHeight <= 0 || Environment.GetEnvironmentVariable("NUBI_TEXT_GUARD") == "0") return;
            double now = EditorApplication.timeSinceStartup;
            if (now - _textGuardAt < 0.5) return;
            _textGuardAt = now;
            const bool strictGame = true;
            string shape = "1080x" + _currentHeight;
            _guardNow.Clear();
            foreach (var t in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>())
            {
                if (t == null || !t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                if (t.canvasRenderer == null || t.color.a * t.canvasRenderer.GetInheritedAlpha() < 0.02f) continue;      // invisible (en fundido o sin opacidad): no se ve
                if (Mathf.Abs(t.transform.lossyScale.y) < 0.0001f || Mathf.Abs(t.transform.lossyScale.x) < 0.0001f) continue;      // escala cero (un botón que recién empieza a aparecer, como «Continuar» del final): no se dibuja nada, no hay letra que medir
                var canvas = t.canvas != null ? t.canvas.rootCanvas : null;
                if (canvas == null) continue;
                var screen = GuardWorldRect((RectTransform)canvas.transform);
                var parent = t.transform.parent as RectTransform;
                bool inImage = parent != null && parent.GetComponent<UnityEngine.UI.Image>() != null && parent.rect.width > 0f;
                var raw = GuardWorldRect(t.rectTransform);
                var pr = inImage ? GuardWorldRect(parent) : default(Rect);
                // tamaño EFECTIVO de la letra en dp: el que usó el ajuste automático (bestFit) o el fontSize, por la escala del objeto respecto del lienzo, entre las unidades por dp del lienzo (1080 de ancho = 360 dp)
                float rootScale = Mathf.Abs(canvas.transform.lossyScale.y) > 0.0001f ? Mathf.Abs(canvas.transform.lossyScale.y) : 1f;
                int used = t.resizeTextForBestFit && t.cachedTextGenerator.fontSizeUsedForBestFit > 0 ? t.cachedTextGenerator.fontSizeUsedForBestFit : t.fontSize;
                float dp = used * (Mathf.Abs(t.transform.lossyScale.y) / rootScale) / (screen.width / 360f);
                int sig = unchecked((((Mathf.RoundToInt(raw.x) * 31 + Mathf.RoundToInt(raw.y)) * 31 + Mathf.RoundToInt(raw.width)) * 31 + Mathf.RoundToInt(raw.height)) * 31 + t.text.GetHashCode() + (inImage ? Mathf.RoundToInt(pr.x) * 17 + Mathf.RoundToInt(pr.y) * 13 + Mathf.RoundToInt(pr.width) * 7 + Mathf.RoundToInt(pr.height) : 0) + used * 3 + Mathf.RoundToInt(dp * 10f));
                int id = t.GetInstanceID();
                if (_guardOk.TryGetValue(id, out var okSig) && okSig == sig) continue;          // no cambió desde que estaba bien
                var drawn = GuardTextRect(t);
                float tol = 0.004f * screen.width + 1f;
                bool flagged = false;
                // (1) dentro de la pantalla
                if (drawn.xMin < screen.xMin - tol || drawn.xMax > screen.xMax + tol || drawn.yMin < screen.yMin - tol || drawn.yMax > screen.yMax + tol)
                { GuardCandidate(strictGame, "fuera de la pantalla", t, drawn, screen, shape, null); flagged = true; }
                // (2) dentro de su padre, si el padre es un botón o una píldora
                if (inImage)
                {
                    var center = drawn.center;
                    float ptol = 0.002f * screen.width + 1f;
                    if (center.x < pr.xMin - ptol || center.x > pr.xMax + ptol || center.y < pr.yMin - ptol || center.y > pr.yMax + ptol)
                    { GuardCandidate(strictGame, "fuera de su padre «" + parent.name + "»", t, drawn, screen, shape, pr); flagged = true; }
                }
                // (3) tamaño mínimo de letra: 14 dp
                if (dp < MinFontDp - 0.05f && t.resizeTextForBestFit && !SizeIsException(t))
                {
                    // el tamaño que dejó el ajuste automático puede ser el de antes de pasar el lienzo a «espacio del mundo» (los menús que aparecen a mitad de la corrida): se regenera el texto y se vuelve a medir
                    t.SetAllDirty();
                    Canvas.ForceUpdateCanvases();
                    used = t.cachedTextGenerator.fontSizeUsedForBestFit > 0 ? t.cachedTextGenerator.fontSizeUsedForBestFit : t.fontSize;
                    dp = used * (Mathf.Abs(t.transform.lossyScale.y) / rootScale) / (screen.width / 360f);
                }
                if (dp < MinFontDp - 0.05f && !SizeIsException(t))
                { GuardCandidate(strictGame, "letra menor de " + MinFontDp + " dp (" + dp.ToString("0.0") + " dp)", t, drawn, screen, shape, null); flagged = true; }
                // (4) texto CORTADO por el alto de su caja: con «Truncate», una línea que no cabe en el alto del rect se OCULTA entera (el texto desaparece, no se recorta): pasa cuando se agranda la letra sin agrandar la caja. Lo que el texto pide de alto
                // (con el ajuste automático ya hecho, y en el ancho que tiene) no puede pasar del alto de su caja.
                if (t.verticalOverflow == VerticalWrapMode.Truncate && t.text.Length > 1)
                {
                    if (t.resizeTextForBestFit)
                    {
                        // con ajuste automático, «preferredHeight» mide con la letra más grande: se cuentan las letras que SÍ se dibujaron (si ni con la letra mínima cabe, faltan)
                        int visibleChars = t.cachedTextGenerator.characterCountVisible;
                        if (!t.supportRichText && !t.text.Contains("\n") && visibleChars > 0 && visibleChars < t.text.Length - 1)
                        { GuardCandidate(strictGame, "texto cortado por el alto de su caja (se ven " + visibleChars + " de " + t.text.Length + " letras)", t, drawn, screen, shape, null); flagged = true; }
                    }
                    else
                    {
                        float asks = t.preferredHeight, has = t.rectTransform.rect.height;
                        if (asks > has + 1f)
                        { GuardCandidate(strictGame, "texto cortado por el alto de su caja (pide " + Mathf.RoundToInt(asks) + " y la caja mide " + Mathf.RoundToInt(has) + ")", t, drawn, screen, shape, null); flagged = true; }
                    }
                }
                if (flagged) _guardOk.Remove(id); else _guardOk[id] = sig;
            }
            var gone = new List<string>();
            foreach (var k in _guardPrev.Keys) if (!_guardNow.Contains(k)) gone.Add(k);
            foreach (var k in gone) _guardPrev.Remove(k);
            ScanHudCover();
            ScanToastCover();
        }

        private static readonly HashSet<string> _toastNow = new HashSet<string>();
        private static readonly HashSet<string> _toastPrev = new HashSet<string>();
        private static readonly HashSet<string> _toastSeen = new HashSet<string>();
        private static bool _sampleToastShown;

        /// <summary>El <c>Toast</c> del juego en marcha (cada controlador lo guarda en su campo <c>_toast</c>), o null.</summary>
        private static NeuroVida.Games.Shared.Toast FindGameToast()
        {
            foreach (var mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
            {
                var field = mb.GetType().GetField("_toast", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null || field.FieldType != typeof(NeuroVida.Games.Shared.Toast)) continue;
                var toast = (NeuroVida.Games.Shared.Toast)field.GetValue(mb);
                if (toast != null) return toast;
            }
            return null;
        }

        /// <summary>Muestra un aviso de prueba (dos renglones, como los más largos de los juegos) con el <c>Toast</c> del juego en marcha.</summary>
        private static void ShowSampleToast()
        {
            _sampleToastShown = true;
            var toast = FindGameToast();
            if (toast == null) { Debug.Log($"[SmokeTest] aviso de prueba: {_current.Name} no tiene _toast"); return; }
            toast.Show("Aviso de prueba", "Una frase de dos renglones para probar dónde queda el aviso en esta pantalla", NeuroVida.Games.Shared.NeuroStyle.Sun, 4.2f);
            Debug.Log($"[SmokeTest] aviso de prueba en {_current.Name}: queda a {Mathf.RoundToInt(toast.PlacedTopU)} u del borde de arriba, choque {Mathf.RoundToInt(toast.PlacedOverlap)}");
        }

        /// <summary>
        /// El aviso a la vista NO tapa nada del juego: ningún texto, botón ni estímulo (<see cref="NeuroVida.Games.Shared.ToastPlacement"/>) por más de 800 unidades cuadradas, en dos revisiones seguidas. Solo corre en las corridas «Aviso*».
        /// </summary>
        private static void ScanToastCover()
        {
            if (!_current.Name.StartsWith("Aviso")) return;
            foreach (var ex in ToastCoverExceptions) if (ex.Id == _current.Id) return;
            var toast = FindGameToast();
            if (toast == null || toast.Rect == null || !toast.Rect.gameObject.activeInHierarchy) return;
            var group = toast.Rect.GetComponent<CanvasGroup>();
            if (group == null || group.alpha < 0.95f) return;
            var space = (RectTransform)toast.Rect.parent;
            var names = new List<string>();
            var boxes = NeuroVida.Games.Shared.ToastPlacement.Collect(space, toast.Rect, names);
            foreach (var z in toast.KeepOutZones)
                if (z != null) { boxes.Add(NeuroVida.Games.Shared.ToastPlacement.LocalRectOf(space, z)); names.Add("zona prohibida " + z.name); }
            var mine = NeuroVida.Games.Shared.ToastPlacement.LocalRectOf(space, toast.Rect);
            _toastNow.Clear();
            for (int i = 0; i < boxes.Count; i++)
            {
                float o = NeuroVida.Games.Shared.ToastPlacement.Overlap(mine, boxes[i]);
                if (o < 800f) continue;
                string key = names[i];
                _toastNow.Add(key);
                if (!_toastPrev.Contains(key) || !_toastSeen.Add(key)) continue;
                _errorCount++;
                Debug.Log($"[SmokeTest] Error capturado: guardia del aviso: {_current.Name} ({_current.Id}), 1080x{_currentHeight}: el aviso (de {Mathf.RoundToInt(mine.yMin)} a {Mathf.RoundToInt(mine.yMax)} desde arriba) tapa [{names[i]}] ({Mathf.RoundToInt(boxes[i].width)}x{Mathf.RoundToInt(boxes[i].height)} en {Mathf.RoundToInt(boxes[i].xMin)},{Mathf.RoundToInt(boxes[i].yMin)}): {Mathf.RoundToInt(o)} u2");
            }
            _toastPrev.Clear();
            foreach (var k in _toastNow) _toastPrev.Add(k);
        }

        /// <summary>Las piezas de foco de <see cref="NeuroVida.Games.Shared.NubiCoach"/> (velo, esquinas, marco, aro y dedo): oscurecen o enmarcan a propósito. Nubi y su globo NO son de foco: no deben tapar el HUD.</summary>
        private static bool IsCoachFocusPiece(UnityEngine.UI.Graphic g)
        {
            for (var c = g.transform; c != null; c = c.parent)
                if (c.name == "NubiCoach")
                {
                    string n = g.name;
                    return n.StartsWith("Veil") || n.StartsWith("Corner") || n == "Frame" || n == "Ring" || n == "Finger" || n == "FingerRim";
                }
            return false;
        }

        private static readonly HashSet<string> _hudNow = new HashSet<string>();
        private static readonly HashSet<string> _hudPrev = new HashSet<string>();
        private static readonly HashSet<string> _hudSeen = new HashSet<string>();

        /// <summary>
        /// Ningún cartel, globo o elemento del juego tapa el HUD de arriba (el título y los rótulos de «Nivel», el avance y la racha): se busca todo lo visible que NO es del HUD, del mismo lienzo, dibujado DESPUÉS de cada texto del HUD (o sea, delante)
        /// y que cubre más de la cuarta parte de lo que el texto dibuja. Se ignoran los fondos y los velos (más de 0,8 del ancho o 0,3 del alto de la pantalla) y los destellos (menores de 30 unidades). Como el resto de la guardia, solo informa lo que se
        /// repite en dos revisiones seguidas (los avisos duran más de 1 s a plena vista).
        /// </summary>
        private static void ScanHudCover()
        {
            _hudNow.Clear();
            UnityEngine.UI.Graphic[] graphics = null;
            foreach (var t in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>())
            {
                if (t == null || !t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text) || t.canvas == null || t.canvasRenderer == null) continue;
                if (t.color.a * t.canvasRenderer.GetInheritedAlpha() < 0.3f) continue;
                Transform hud = null;
                for (var c = t.transform.parent; c != null; c = c.parent) if (c.name == "Hud") { hud = c; break; }
                if (hud == null) continue;
                var canvas = t.canvas.rootCanvas;
                var screen = GuardWorldRect((RectTransform)canvas.transform);
                var mine = GuardTextRect(t);
                float area = mine.width * mine.height;
                if (area <= 0f) continue;
                int depth = t.canvasRenderer.absoluteDepth;
                if (graphics == null) graphics = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Graphic>();
                foreach (var g in graphics)
                {
                    if (g == null || g == t || !g.isActiveAndEnabled || g.canvas == null || g.canvasRenderer == null || g.canvas.rootCanvas != canvas) continue;
                    if (g.transform.IsChildOf(hud)) continue;
                    if (IsCoachFocusPiece(g)) continue;                              // el velo y el marco de foco del tutorial se ponen a propósito sobre el juego (Nubi y su globo, no: esos cuentan)
                    var gText = g as UnityEngine.UI.Text;
                    if (gText != null && string.IsNullOrWhiteSpace(gText.text)) continue;
                    if (g.color.a * g.canvasRenderer.GetInheritedAlpha() < (gText != null ? 0.25f : 0.4f)) continue;      // un halo o un aro casi transparente no tapa nada
                    if (g.canvasRenderer.absoluteDepth <= depth) continue;          // se dibuja ANTES: queda detrás del texto del HUD
                    var gr = gText != null ? GuardTextRect(gText) : GuardWorldRect(g.rectTransform);          // de un texto, lo que dibuja (no toda su caja)
                    if (gr.width >= 0.8f * screen.width || gr.height >= 0.3f * screen.height) continue;      // fondos y velos
                    if (gr.width < 30f && gr.height < 30f) continue;                                          // destellos
                    float ox = Mathf.Min(mine.xMax, gr.xMax) - Mathf.Max(mine.xMin, gr.xMin), oy = Mathf.Min(mine.yMax, gr.yMax) - Mathf.Max(mine.yMin, gr.yMin);
                    if (ox <= 0f || oy <= 0f || ox * oy < 0.25f * area) continue;
                    string key = GuardPath(t.transform) + "<-" + GuardPath(g.transform);
                    _hudNow.Add(key);
                    if (!_hudPrev.Contains(key) || !_hudSeen.Add(key)) continue;
                    string text = t.text.Replace("\n", " ");
                    _errorCount++;
                    Debug.Log($"[SmokeTest] Error capturado: guardia del HUD: {_current.Name} ({_current.Id}), 1080x{_currentHeight}: «{text}» [{GuardPath(t.transform)}] queda tapado por [{GuardPath(g.transform)}] ({Mathf.RoundToInt(gr.width)}x{Mathf.RoundToInt(gr.height)} en {Mathf.RoundToInt(gr.xMin)},{Mathf.RoundToInt(gr.yMin)})");
                }
            }
            _hudPrev.Clear();
            foreach (var k in _hudNow) _hudPrev.Add(k);
        }

        private static void GuardReport(bool strict, string rule, UnityEngine.UI.Text t, Rect rect, Rect screen, string shape, Rect? parent)
        {
            string path = GuardPath(t.transform);
            if (!_textGuardSeen.Add(rule + "|" + path)) return;
            string text = t.text.Replace("\n", " ");
            if (text.Length > 40) text = text.Substring(0, 40) + "…";
            string where = $"{_current.Name} ({_current.Id}), {shape}: «{text}» [{path}] {rule}: dibujado en ({Mathf.RoundToInt(rect.xMin)}..{Mathf.RoundToInt(rect.xMax)}, {Mathf.RoundToInt(rect.yMin)}..{Mathf.RoundToInt(rect.yMax)})"
                + (parent.HasValue ? $", padre en ({Mathf.RoundToInt(parent.Value.xMin)}..{Mathf.RoundToInt(parent.Value.xMax)}, {Mathf.RoundToInt(parent.Value.yMin)}..{Mathf.RoundToInt(parent.Value.yMax)})" : $", pantalla ({Mathf.RoundToInt(screen.xMin)}..{Mathf.RoundToInt(screen.xMax)}, {Mathf.RoundToInt(screen.yMin)}..{Mathf.RoundToInt(screen.yMax)})");
            if (strict) { _errorCount++; Debug.Log("[SmokeTest] Error capturado: guardia de textos: " + where); }
            else Debug.Log("[SmokeTest] AVISO guardia de textos: " + where);
        }

        private static bool CoachAuditReachedEnd()
        {
            foreach (var st in NeuroVida.Games.Shared.NubiCoach.AuditSteps) if (st.Text == NeuroVida.Games.Shared.CoachTexts.Ready) return true;
            return false;
        }

        private static bool _screenLogged;
        private static readonly HashSet<int> _auditCanvases = new HashSet<int>();
        internal static void ResetAuditCanvases() => _auditCanvases.Clear();

        /// <summary>
        /// La ventana del Editor sin gráficos mide 640x480 y no se puede cambiar: para revisar el tutorial en otras formas de pantalla, el lienzo de cada juego (de pantalla completa, con su
        /// <c>CanvasScaler</c> de 1080 de ancho) se pasa a «espacio del mundo» con 1080 x <paramref name="height"/> unidades, que es justo el área que vería el juego en ese teléfono. Se hace en cuanto aparece,
        /// antes de que el juego calcule su disposición.
        /// </summary>
        internal static void ForceAuditCanvasSize(int height)
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.GetComponent<UnityEngine.UI.CanvasScaler>() == null) continue;
                if (!_auditCanvases.Add(canvas.GetInstanceID())) continue;
                canvas.GetComponent<UnityEngine.UI.CanvasScaler>().enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = (RectTransform)canvas.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(1080f, height);
                rt.localScale = Vector3.one;
                rt.position = Vector3.zero;
            }
        }

        /// <summary>
        /// Los pasos del tutorial que registró <see cref="NeuroVida.Games.Shared.NubiCoach"/>: cada uno debe tener a Nubi y su globo SIN tapar el hueco, las zonas protegidas ni ningún texto del juego, con el texto en
        /// 3 líneas o menos y la letra en su tamaño. Deja el registro en <c>unity/test-results/coach-audit/&lt;juego&gt;-&lt;ancho&gt;x&lt;alto&gt;.json</c> (de ahí salen las láminas de revisión).
        /// </summary>
        private static void CheckCoachAudit()
        {
            if (!NeuroVida.Games.Shared.NubiCoach.AuditEnabled) return;
            NeuroVida.Games.Shared.NubiCoach.AuditEnabled = false;
            var steps = NeuroVida.Games.Shared.NubiCoach.AuditSteps;
            var game = NeuroVida.Games.Shared.NubiCoach.AuditGame;
            var dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "..", "test-results", "coach-audit"));
            System.IO.Directory.CreateDirectory(dir);
            var wrapper = new CoachAuditFile { Game = game, Width = Screen.width, Height = Screen.height, Steps = steps.ToArray() };
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, $"{game}-{_currentHeight}.json"), JsonUtility.ToJson(wrapper, true));
            Debug.Log($"[SmokeTest] tutorial de {game} a {Screen.width}x{Screen.height}: {steps.Count} paso(s) registrados");
            bool strict = true;
            if (steps.Count == 0 && strict) { _errorCount++; Debug.Log($"[SmokeTest] Error capturado: el tutorial de {game} no registró ningún paso"); }
            foreach (var failure in NeuroVida.Games.Shared.NubiCoach.ProbeFailures) { if (strict) _errorCount++; Debug.Log($"[SmokeTest] Error capturado: {_currentHeight}: {failure}"); }
            int touchSteps = 0;
            foreach (var st in steps) if (st.Kind == "Touch") touchSteps++;
            if (NeuroVida.Games.Shared.NubiCoach.RealTouchesAccepted < touchSteps)
            {
                if (strict) _errorCount++;
                Debug.Log($"[SmokeTest] Error capturado: {game} {_currentHeight}: {touchSteps} paso(s) de Tocar y solo {NeuroVida.Games.Shared.NubiCoach.RealTouchesAccepted} se cerraron por un toque de verdad en el hueco");
            }
            else Debug.Log($"[SmokeTest] {game} {_currentHeight}: los {touchSteps} paso(s) de Tocar se cerraron por un toque de verdad en el hueco");
            foreach (var st in steps)
            {
                string where = $"{game} {_currentHeight}, paso {st.Index} ({st.Kind}) «{st.Text}»";
                if (!st.Clean) { if (strict) _errorCount++; Debug.Log($"[SmokeTest] Error capturado: Nubi o el globo tapan algo ({Mathf.RoundToInt(st.Overlap)} u²{(st.TooLong ? ", el texto no cabe en 3 líneas" : "")}): {where}"); }
                if (st.ControlClash > 20f) { if (strict) _errorCount++; Debug.Log($"[SmokeTest] Error capturado: «Saltar tutorial» o el rótulo de práctica quedan sobre algo del juego ({Mathf.RoundToInt(st.ControlClash)} u²): {where}"); }
                if (st.ActualLines > NeuroVida.Games.Shared.CoachLayout.MaxLines) { if (strict) _errorCount++; Debug.Log($"[SmokeTest] Error capturado: el texto se dibuja en {st.ActualLines} líneas: {where}"); }
                if (st.ActualLines > st.Lines) { if (strict) _errorCount++; Debug.Log($"[SmokeTest] Error capturado: el globo se calculó para {st.Lines} línea(s) y el texto se dibuja en {st.ActualLines} (se saldría del globo): {where}"); }
            }
        }

        [Serializable]
        private sealed class CoachAuditFile { public string Game; public int Width, Height; public NeuroVida.Games.Shared.CoachStepReport[] Steps; }

        // ------------------------------------------------------------------ «Efectos de sonido» apagado (Tarea 64)

        private static double _audioSampleAt;
        private static bool _audioHeard, _audioReported;

        /// <summary>Cada 0,2 s mira si algún AudioSource está sonando. En las corridas «…Mudo» (sound_enabled = false) cualquier sonido es un error; en «Piloto» y «Radar» (con sonido) sirve de control de que la medida funciona.</summary>
        private static void SampleAudio()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _audioSampleAt < 0.2) return;
            _audioSampleAt = now;
            string playing = null;
            foreach (var s in UnityEngine.Object.FindObjectsOfType<AudioSource>())
                if (s != null && s.isPlaying) { playing = s.gameObject.name + "/" + (s.clip != null ? s.clip.name : "(sin clip)"); break; }
            if (playing == null) return;
            _audioHeard = true;
            if (_current.Name.EndsWith("Mudo") && !_audioReported)
            {
                _audioReported = true;
                _errorCount++;
                Debug.Log("[SmokeTest] Error capturado: con el sonido apagado (sound_enabled = false) sonó " + playing + " en " + _current.Name);
            }
        }

        private static void PauseFail(string what)
        {
            _errorCount++;
            Debug.Log("[SmokeTest] Error capturado: pausa de " + (_current.Name ?? "este juego") + ": " + what);
        }

        private static void OnUpdateList()
        {
            if (!_listRunning) return;
            if (_listPlaying)
            {
                if (!EditorApplication.isPlaying) return; // todavía entrando a Play
                if (_currentHeight > 0) ForceAuditCanvasSize(_currentHeight);
                if (!_screenLogged) { _screenLogged = true; Debug.Log($"[SmokeTest] pantalla {Screen.width}x{Screen.height}" + (_currentHeight > 0 ? $", lienzo de revisión 1080x{_currentHeight}" : "")); }
                if (_enteredPlayAt == 0) _enteredPlayAt = EditorApplication.timeSinceStartup;
                double played = EditorApplication.timeSinceStartup - _enteredPlayAt;
                ScanTextPlacement();
                if (_current.Name.EndsWith("Mudo") || _current.Name == "Piloto" || _current.Name == "Radar") SampleAudio();
                if (_current.Name.StartsWith("Aviso") && !_sampleToastShown && played >= 5.5) ShowSampleToast();
                if (_current.Name.StartsWith("HowTo")) DriveHowTo(played);
                if (StalePauseRequested && !_staleDone) SimulateStalePause();
                // la revisión del tutorial espera a que la ronda guiada llegue a su último aviso («¡Listo! Ahora va en serio»), con tope de 100 s
                if (NeuroVida.Games.Shared.NubiCoach.AuditEnabled) { if (played < 8 || (played < 100 && !CoachAuditReachedEnd())) return; }
                else if (played < _current.Seconds) return;
                if (_currentHeight == 0 && !PauseCheckDone()) return;  // abre la pausa y comprueba que responde (no en el lienzo de revisión: ahí no hay dónde tocar)
                if (_currentHeight > 0 && (_current.Name.StartsWith("Pantalla") || (_current.Name.StartsWith("Tutorial") && _currentHeight == 2400)) && !PauseShowDone()) return;      // en forma de teléfono solo se ABRE la pausa (para que la guardia de textos vea sus botones)

                _listPlaying = false;
                Debug.Log($"[SmokeTest] tiempo {_current.Name} ({(_currentHeight > 0 ? "1080x" + _currentHeight : "ventana")}): {Mathf.RoundToInt((float)(EditorApplication.timeSinceStartup - _entryStartAt))} s");
                CheckCoachAudit();
                CheckHowTo();
                if (_current.Name == "Piloto" || _current.Name == "Radar")
                    Debug.Log("[SmokeTest] control de audio de " + _current.Name + ": " + (_audioHeard ? "se oyó sonido (el instrumento sirve)" : "NO se detectó ningún sonido: la prueba de «sonido apagado» no sería concluyente"));
                EditorApplication.ExitPlaymode();
                CheckReduceMotionArrived();
                var ok = _errorCount == 0;
                if (ok) _listOk++; else _listAnyFailed = true;
                Debug.Log(ok
                    ? $"[SmokeTest] {_current.Name}: OK"
                    : $"[SmokeTest] {_current.Name}: FALLÓ -- {_errorCount} error(es)/excepción(es) durante la partida.");
                return;
            }

            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return; // todavía saliendo de Play
            if (_queue.Count > 0) { StartNextInList(); return; }

            _listRunning = false;
            EditorApplication.update -= OnUpdateList;
            Application.logMessageReceived -= OnLog;
            RestoreDomainReloadSettings();
            Debug.Log($"[SmokeTest] RunList: {_listOk}/{_listTotal} " + (_listAnyFailed ? "FALLÓ" : "OK"));
            EditorApplication.Exit(_listAnyFailed ? 1 : 0);
        }

        private static void RunGame(string gameId, float seconds, bool tutorial = false, bool assessment = false)
        {
            RunSeconds = seconds;
            _pauseStage = 0;
            _current = (gameId ?? "juego", gameId, seconds);
            EditorPlaytestBootstrap.ShowTutorialOverride = tutorial;
            EditorPlaytestBootstrap.TimedOverride = false;
            EditorPlaytestBootstrap.AssessmentOverride = assessment;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoContinue = tutorial;
            NeuroVida.Games.Shared.GuidedTutorial.EditorAutoPlayGame = (gameId == "engranajes" || gameId == "bodega" || gameId == "parejas" || gameId == "correo" || gameId == "satelites" || gameId == "piloto" || gameId == "radar") && !tutorial;
            EditorPlaytestBootstrap.GameIdOverride = gameId;
            EditorPlaytestBootstrap.ReduceMotionOverride = ReduceMotionRequested;
            EditorSceneManager.OpenScene(ScenePath);

            // Entrar a Play dispara por defecto un domain reload -- eso borra estado
            // estático y desuscribe cualquier evento registrado antes de llamar
            // EnterPlaymode(), dejando este smoke test colgado para siempre (bug real
            // encontrado en la primera corrida: el proceso de Unity quedaba girando en
            // vacío sin nunca llamar EditorApplication.Exit). Se desactiva acá, solo
            // para esta corrida, y se restaura al final.
            _previousDomainReloadDisabled = EditorSettings.enterPlayModeOptionsEnabled;
            _previousOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            Application.logMessageReceived += OnLog;
            EditorApplication.update += OnUpdate;
            _isRunning = true;
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Con <c>NUBI_REDUCE_MOTION=1</c>, comprueba que la bandera llegó de verdad al juego (si no, el smoke no probaría nada).</summary>
        private static void CheckReduceMotionArrived()
        {
            if (!ReduceMotionRequested || NeuroVida.Games.Shared.GameFeel.ReduceMotion) return;
            _errorCount++;
            Debug.Log("[SmokeTest] Error capturado: NUBI_REDUCE_MOTION=1 pero GameFeel.ReduceMotion quedó en false");
        }

        private static void RestoreDomainReloadSettings()
        {
            EditorSettings.enterPlayModeOptionsEnabled = _previousDomainReloadDisabled;
            EditorSettings.enterPlayModeOptions = _previousOptions;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                _errorCount++;
                Debug.Log($"[SmokeTest] Error capturado: {condition}\n{stackTrace}");
            }
        }

        private static void OnUpdate()
        {
            if (!_isRunning) return;
            if (!EditorApplication.isPlaying) return; // todavía terminando el domain reload de entrar a Play

            if (_enteredPlayAt == 0) _enteredPlayAt = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - _enteredPlayAt < RunSeconds) return;
            if (!PauseCheckDone()) return;  // abre la pausa y comprueba que responde

            _isRunning = false;
            CheckReduceMotionArrived();
            EditorApplication.update -= OnUpdate;
            Application.logMessageReceived -= OnLog;
            EditorApplication.ExitPlaymode();
            RestoreDomainReloadSettings();

            Debug.Log(_errorCount == 0
                ? "[SmokeTest] OK -- sin errores durante los primeros segundos de partida."
                : $"[SmokeTest] FALLÓ -- {_errorCount} error(es)/excepción(es) durante la partida.");

            EditorApplication.Exit(_errorCount == 0 ? 0 : 1);
        }
    }
}
