using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Parejas; // SymbolSprite (asteroides)
using NeuroVida.Games.Secuencia;

namespace NeuroVida.Games.Shared
{
    /// <summary>
    /// "Mundo" de fondo de un juego. Todos comparten el sello de la app: el mismo cielo nocturno
    /// (<see cref="NeuroStyle.NightGradient"/>), nebulosas y estrellas en perspectiva. Encima, cada juego tiene
    /// UN elemento propio ligado a su mecánica (lunas gemelas en Parejas, superficie lunar en Ruta del Tesoro...), para que jugar varios seguidos no se sienta repetitivo. Es decoración pura: todo va
    /// detrás del contenido y nada recibe toques.
    /// </summary>
    public sealed class GameWorld
    {
        public string Name;
        public Color NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.22f);
        public Vector2 NebulaAPos = new Vector2(0.12f, 0.88f);
        public Color NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.18f);
        public Vector2 NebulaBPos = new Vector2(0.92f, 0.12f);
        public int Stars = 50;
        public Vector2 VanishingPoint = new Vector2(0.5f, 0.62f);
        public int Bokeh;

        /// <summary>Lunas: posición normalizada (x, y) y tamaño en unidades del canvas (z).</summary>
        public Vector3[] Moons = new Vector3[0];
        public Color[] MoonTints = new Color[0];

        /// <summary>Superficie de una luna en la parte baja (0 = sin superficie), como fracción del alto.</summary>
        public float SurfaceHeight;
        public Color SurfaceColor = NeuroStyle.Hex(0x9C94D6);

        /// <summary>Asteroides de arcilla que flotan y giran despacio: posición normalizada (x, y) y tamaño (z).</summary>
        public Vector3[] Asteroids = new Vector3[0];

        public int OrbitRings;

        /// <summary>Cúpula de observatorio sobre el borde de la superficie (Lluvia de meteoros).</summary>
        public bool HasDome;
        public Vector2 OrbitCenter = new Vector2(0.5f, 0.45f);

        /// <summary>Planetas de arcilla: posición normalizada (x, y) y diámetro (z); color en <see cref="PlanetColors"/>.</summary>
        public Vector3[] Planets = new Vector3[0];
        public Color[] PlanetColors = new Color[0];

        /// <summary>Cielo QUIETO (estrellas fijas que titilan, sin perspectiva) con una banda de Vía Láctea diagonal: La estrella intrusa.</summary>
        public bool StaticSky;
        public int TwinkleStars;
        public bool MilkyWay;

        public int Constellations;
        public float MeteorEverySeconds;
        public string FloatingGlyphs;

        // ---- los mundos de los 9 juegos ----

        public static GameWorld Constellation => new GameWorld
        {
            Name = "Constelación", Constellations = 3,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.24f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.20f),
        };

        public static GameWorld TwinMoons => new GameWorld
        {
            Name = "Lunas gemelas", Bokeh = 5,
            Moons = new[] { new Vector3(0.74f, 0.845f, 150f), new Vector3(0.885f, 0.80f, 96f) },
            MoonTints = new[] { NeuroStyle.Hex(0xFFF4D6), NeuroStyle.Hex(0xE4DCFF) },
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.26f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.16f),
        };

        // Sin uso desde que Ruta del Tesoro se retiró (4-oct-2026): queda como mundo disponible para otro juego.
        public static GameWorld TreasureMoon => new GameWorld
        {
            Name = "Luna del tesoro", SurfaceHeight = 0.15f, Stars = 46, VanishingPoint = new Vector2(0.5f, 0.3f),
            Asteroids = new[] { new Vector3(0.02f, 0.58f, 120f), new Vector3(0.985f, 0.40f, 96f), new Vector3(0.90f, 0.18f, 64f) },
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.24f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.16f),
            NebulaBPos = new Vector2(0.8f, 0.35f),
        };

        public static GameWorld Neon => new GameWorld
        {
            Name = "Neón", Bokeh = 9,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.26f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.26f),
        };

        // Sin uso desde que Comparación Instantánea se retiró (4-oct-2026): queda como mundo disponible para otro juego.
        public static GameWorld PlanetDuel => new GameWorld
        {
            Name = "Duelo de planetas",
            Planets = new[] { new Vector3(-0.02f, 0.06f, 620f), new Vector3(1.03f, 0.93f, 520f) },
            PlanetColors = new[] { NeuroStyle.Sky, NeuroStyle.Coral },
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.18f), NebulaAPos = new Vector2(0.9f, 0.85f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.20f), NebulaBPos = new Vector2(0.1f, 0.15f),
        };

        // Sin uso desde que Cambio de Chip se retiró (3-oct-2026): queda como mundo disponible para otro juego.
        public static GameWorld Orbits => new GameWorld
        {
            Name = "Órbitas", OrbitRings = 4,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Lime, 0.14f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.20f),
        };

        // Sin uso desde que Detective de Series se retiró (4-oct-2026): queda como mundo disponible para otro juego (no es el juego «Lluvia de meteoros»).
        public static GameWorld MeteorShower => new GameWorld
        {
            Name = "Lluvia de meteoros", MeteorEverySeconds = 2.4f,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.24f), NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.12f),
        };

        /// <summary>Piloto Estelar: las estrellas nacen arriba (hacia donde vuela la nave) y pasan a los costados;
        /// el juego sube su velocidad (<c>StarfieldFx.Warp</c>) con la velocidad de vuelo.</summary>
        public static GameWorld Hyperspace => new GameWorld
        {
            Name = "Hiperespacio", Stars = 90, VanishingPoint = new Vector2(0.5f, 0.96f),
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.20f), NebulaAPos = new Vector2(0.5f, 1.0f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.14f), NebulaBPos = new Vector2(0.5f, 0.0f),
        };

        /// <summary>Radar: cielo quieto y oscuro (nada debe competir con el destello), un resplandor verde de fósforo
        /// detrás del radar y unas pocas luces desenfocadas.</summary>
        public static GameWorld RadarStation => new GameWorld
        {
            Name = "Estación de radar", Stars = 40, Bokeh = 3,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Lime, 0.10f), NebulaAPos = new Vector2(0.5f, 0.62f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.16f), NebulaBPos = new Vector2(0.9f, 0.08f),
        };

        /// <summary>Satélites: cielo quieto (nada se mueve que se parezca a un satélite), resplandor sol y celeste.</summary>
        public static GameWorld MissionControl => new GameWorld
        {
            Name = "Control de misión", Stars = 45,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.10f), NebulaAPos = new Vector2(0.15f, 0.9f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.18f), NebulaBPos = new Vector2(0.85f, 0.1f),
        };

        /// <summary>Freno de Emergencia: base de lanzamiento sobre la superficie de una luna; resplandor coral arriba
        /// (hacia donde despegan los cohetes).</summary>
        public static GameWorld LaunchBase => new GameWorld
        {
            Name = "Base de lanzamiento", SurfaceHeight = 0.2f, Stars = 50, VanishingPoint = new Vector2(0.5f, 0.35f),
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.16f), NebulaAPos = new Vector2(0.5f, 0.95f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.14f), NebulaBPos = new Vector2(0.1f, 0.4f),
        };

        /// <summary>Aterrizaje Lunar: la superficie de una luna ocupa la cuarta parte de abajo (la regla va en su
        /// borde) y un planeta asoma a la derecha.</summary>
        public static GameWorld LunarRange => new GameWorld
        {
            Name = "Campo de aterrizaje", SurfaceHeight = 0.26f, Stars = 50, VanishingPoint = new Vector2(0.5f, 0.4f),
            Planets = new[] { new Vector3(1.04f, 0.56f, 340f) }, PlanetColors = new[] { NeuroStyle.Grape },
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.16f), NebulaAPos = new Vector2(0.15f, 0.85f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.10f), NebulaBPos = new Vector2(0.5f, 0.3f),
        };

        /// <summary>Aterrizaje Lunar (renovación del 10-oct): el cielo de la app SIN estrellas ni perspectiva ni planeta ni superficie y con nebulosas muy tenues: todo lo que se ve en el cielo (estrellas que titilan, la Tierra, un satélite, la luna y sus dos cordilleras que se desplazan) lo arma
        /// el propio juego (nada fijo junto a la regla: serviría de pista).</summary>
        public static GameWorld LunarBase => new GameWorld
        {
            Name = "Base lunar", Stars = 0,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.10f), NebulaAPos = new Vector2(0.17f, 0.85f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.07f), NebulaBPos = new Vector2(0.9f, 0.3f),
        };

        /// <summary>Acoplamiento: muelle de la estación, cielo quieto (nada gira en el fondo: el giro es la tarea) y un
        /// planeta celeste asomando abajo a la izquierda.</summary>
        public static GameWorld DockingBay => new GameWorld
        {
            Name = "Muelle de la estación", Stars = 48,
            Planets = new[] { new Vector3(-0.06f, 0.36f, 380f) }, PlanetColors = new[] { NeuroStyle.Sky },
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.20f), NebulaAPos = new Vector2(0.85f, 0.8f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Lime, 0.10f), NebulaBPos = new Vector2(0.5f, 0.25f),
        };

        // Sin uso desde que Tráfico Estelar se retiró (4-oct-2026): queda como mundo disponible para otro juego.
        /// <summary>Tráfico Estelar: centro de tráfico; cielo quieto con nebulosa uva arriba (de donde sale el portal).</summary>
        public static GameWorld TrafficHub => new GameWorld
        {
            Name = "Centro de tráfico", Stars = 50,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.24f), NebulaAPos = new Vector2(0.5f, 0.95f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.14f), NebulaBPos = new Vector2(0.5f, 0.1f),
        };

        /// <summary>Bitácora de Misión: cielo de exploración con constelaciones tenues y alguna estrella fugaz (destellos
        /// lentos, lejos del mapa: decoran sin distraer).</summary>
        public static GameWorld Logbook => new GameWorld
        {
            Name = "Bitácora", Stars = 60, Constellations = 2, MeteorEverySeconds = 9f,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.24f), NebulaAPos = new Vector2(0.2f, 0.85f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.16f), NebulaBPos = new Vector2(0.85f, 0.2f),
        };

        /// <summary>Rumbo a Casa: espacio profundo SIN estrellas de fondo (unas estrellas lejanas fijas servirían de
        /// brújula y regalarían el rumbo: la única referencia lejana es el faro, y solo en la mitad de los viajes). El juego
        /// dibuja su propio polvo de estrellas, que se mueve con la nave. Nebulosas muy tenues.</summary>
        public static GameWorld DeepSpace => new GameWorld
        {
            Name = "Espacio profundo", Stars = 0,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.10f), NebulaAPos = new Vector2(0.5f, 0.75f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.14f), NebulaBPos = new Vector2(0.5f, 0.2f),
        };

        /// <summary>Lluvia de meteoros: el borde curvo de un planeta abajo, con la cúpula de un observatorio, bajo el
        /// cielo de la app. Los meteoros se apagan en la "atmósfera", justo encima.</summary>
        public static GameWorld Observatory => new GameWorld
        {
            Name = "Observatorio", SurfaceHeight = 0.16f, SurfaceColor = NeuroStyle.Hex(0x9C94D6), HasDome = true, Stars = 70,
            VanishingPoint = new Vector2(0.5f, 0.4f),
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.14f), NebulaAPos = new Vector2(0.15f, 0.8f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.16f), NebulaBPos = new Vector2(0.85f, 0.25f),
        };

        /// <summary>¿Verdad o disparate?: una sala de radio en el espacio. Cielo de la app con estrellas un poco más vivas y una
        /// superficie baja (la plataforma de los botones); la antena es del juego, no del fondo.</summary>
        public static GameWorld Radio => new GameWorld
        {
            Name = "Sala de radio", SurfaceHeight = 0.10f, SurfaceColor = NeuroStyle.Hex(0x8A92D8), Stars = 80,
            VanishingPoint = new Vector2(0.5f, 0.5f),
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.16f), NebulaAPos = new Vector2(0.2f, 0.78f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.18f), NebulaBPos = new Vector2(0.85f, 0.3f),
        };

        /// <summary>Cosecha de palabras: el cielo de la app con nebulosas más verdes (el huerto es el elemento propio y lo arma el juego).</summary>
        public static GameWorld Huerto => new GameWorld
        {
            Name = "Huerto", SurfaceHeight = 0f, Stars = 70,
            VanishingPoint = new Vector2(0.5f, 0.5f),
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Lime, 0.10f), NebulaAPos = new Vector2(0.5f, 0.38f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.14f), NebulaBPos = new Vector2(0.9f, 0.85f),
        };

        /// <summary>La estrella intrusa (Atlas celeste): el cielo de la app más profundo y quieto, con ~230 estrellas fijas (~28 titilan),
        /// una banda de Vía Láctea inclinada −35° y nebulosas lila y coral. Nada se mueve que se parezca a una figura.</summary>
        public static GameWorld CieloProfundo => new GameWorld
        {
            Name = "Cielo profundo", StaticSky = true, Stars = 230, TwinkleStars = 28, MilkyWay = true,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.16f), NebulaAPos = new Vector2(0.18f, 0.78f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.09f), NebulaBPos = new Vector2(0.88f, 0.22f),
        };

        /// <summary>Rastro de luz (cielo de cristal): el fondo C quieto (el degradé común), ~240 estrellas fijas (~30 titilan) y dos nebulosas lila y coral MÁS
        /// tenues que las de Cielo profundo (brillo medio ≈ 13,1 contra ≈ 13,9 de La estrella intrusa sin su Vía Láctea; nunca más claro). El tinte de cada modo
        /// lo pone el juego solo en los bordes (viñeta, alfa 0,05). Nada se mueve solo: la chispa y los luceros son la tarea.</summary>
        public static GameWorld CieloDeCristal => new GameWorld
        {
            Name = "Cielo de cristal", StaticSky = true, Stars = 240, TwinkleStars = 30,
            NebulaA = NeuroStyle.WithAlpha(NeuroStyle.Grape, 0.09f), NebulaAPos = new Vector2(0.17f, 0.72f),
            NebulaB = NeuroStyle.WithAlpha(NeuroStyle.Coral, 0.05f), NebulaBPos = new Vector2(0.9f, 0.18f),
        };
    }

    /// <summary>Arma el fondo de un <see cref="GameWorld"/> dentro de un RectTransform que ocupa la pantalla.</summary>
    public static class WorldBackdrop
    {
        public static void Build(RectTransform bgRect, GameWorld world)
        {
            var bg = bgRect.gameObject.GetComponent<Image>();
            if (bg == null) bg = bgRect.gameObject.AddComponent<Image>();
            bg.sprite = NeuroStyle.NightGradient();
            bg.color = Color.white;
            bg.raycastTarget = false;

            UiFx.AddBackgroundGlow(bgRect, world.NebulaAPos, 1600f, world.NebulaA);
            UiFx.AddBackgroundGlow(bgRect, world.NebulaBPos, 1500f, world.NebulaB);

            var ambient = bgRect.gameObject.AddComponent<WorldAmbient>();
            ambient.Init(bgRect);

            if (world.MilkyWay) AddMilkyWay(bgRect);
            if (world.StaticSky) AddStaticSky(bgRect, ambient, world);
            else
            {
                var starsRect = Layer(bgRect, "Stars");
                var stars = starsRect.gameObject.AddComponent<StarfieldFx>();
                stars.VanishingPoint = world.VanishingPoint;
                stars.Build(starsRect, world.Stars, 10f, world.Name.GetHashCode());
            }

            for (int i = 0; i < world.Moons.Length; i++)
                AddMoon(bgRect, ambient, world.Moons[i], i < world.MoonTints.Length ? world.MoonTints[i] : NeuroStyle.Cream);
            for (int i = 0; i < world.Asteroids.Length; i++) AddAsteroid(bgRect, ambient, world.Asteroids[i], i);
            if (world.SurfaceHeight > 0f) AddSurface(bgRect, ambient, world); // tapa las estrellas de abajo
            if (world.HasDome) AddDome(bgRect, world);
            if (world.OrbitRings > 0) AddOrbits(bgRect, ambient, world);
            for (int i = 0; i < world.Planets.Length; i++)
                AddPlanet(bgRect, world.Planets[i], i < world.PlanetColors.Length ? world.PlanetColors[i] : NeuroStyle.Grape);
            if (world.Constellations > 0) AddConstellations(bgRect, ambient, world.Constellations, world.Name.GetHashCode());
            if (world.MeteorEverySeconds > 0f) ambient.EnableMeteors(world.MeteorEverySeconds);
            if (!string.IsNullOrEmpty(world.FloatingGlyphs)) ambient.AddGlyphs(world.FloatingGlyphs, 14);
            if (world.Bokeh > 0) bgRect.gameObject.AddComponent<CountdownAmbient>().Build(bgRect, world.Bokeh);
        }

        // ---- piezas ----

        private static Sprite _milkyWay;

        /// <summary>Banda de Vía Láctea: ~1500 puntos gaussianos (velo rgba(200,190,255,0,09)) a lo largo de una franja, hecha UNA vez
        /// y puesta inclinada −35°. Es una textura de 512×256: barata y sin animación.</summary>
        private static void AddMilkyWay(RectTransform parent)
        {
            if (_milkyWay == null) _milkyWay = BakeMilkyWay();
            var rect = Node(parent, "MilkyWay", new Vector2(0.5f, 0.56f), new Vector2(2300f, 1150f));
            rect.localRotation = Quaternion.Euler(0f, 0f, -35f);
            Img(rect, _milkyWay, Color.white);
        }

        private static Sprite BakeMilkyWay()
        {
            const int w = 512, h = 256;
            var acc = new float[w * h];
            var rng = new System.Random(2810);
            for (int n = 0; n < 1500; n++)
            {
                // gaussiana de Box-Muller a lo ancho de la franja: más densa en el centro
                double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
                float gy = (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2));
                float cx = (float)(rng.NextDouble() * (w - 40) + 20);
                float cy = h * 0.5f + gy * h * 0.13f;
                float r = 1.5f + (float)rng.NextDouble() * 4.5f;
                int x0 = Mathf.Max(0, (int)(cx - r - 1)), x1 = Mathf.Min(w - 1, (int)(cx + r + 1));
                int y0 = Mathf.Max(0, (int)(cy - r - 1)), y1 = Mathf.Min(h - 1, (int)(cy + r + 1));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                        if (d < 1f) acc[y * w + x] += 0.09f * (1f - d * d);
                    }
            }
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++)
            {
                float a = Mathf.Clamp01(acc[i]);
                px[i] = new Color32(200, 190, 255, (byte)(a * 255f));
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Estrellas fijas (fondo C): cada una un disco chico; las primeras [TwinkleStars] titilan despacio.</summary>
        private static void AddStaticSky(RectTransform parent, WorldAmbient ambient, GameWorld world)
        {
            var layer = Layer(parent, "StaticSky");
            var rng = new System.Random(world.Name.GetHashCode());
            for (int i = 0; i < world.Stars; i++)
            {
                float size = 3f + (float)rng.NextDouble() * 5f;
                var star = Node(layer, "S", new Vector2((float)rng.NextDouble(), (float)rng.NextDouble()), new Vector2(size, size));
                float a = 0.35f + (float)rng.NextDouble() * 0.55f;
                var img = Img(star, DiscSprite.Get(), i % 6 == 0 ? NeuroStyle.WithAlpha(NeuroStyle.StarWarm, a) : new Color(1f, 1f, 1f, a));
                if (i < world.TwinkleStars) ambient.Breathe(img, a, a * 0.55f, 0.8f + (float)rng.NextDouble() * 1.6f);
            }
        }

        private static void AddMoon(RectTransform parent, WorldAmbient ambient, Vector3 moon, Color tint)
        {
            var anchor = new Vector2(moon.x, moon.y);
            float size = moon.z;
            var glow = Node(parent, "MoonGlow", anchor, new Vector2(size * 3.6f, size * 3.6f));
            var glowImg = Img(glow, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(tint, 0.26f));
            ambient.Breathe(glowImg, 0.26f, 0.08f);

            var disc = Node(parent, "Moon", anchor, new Vector2(size, size));
            Img(disc, DiscSprite.Get(), tint);
            // Cráteres suaves: dan volumen sin dibujar una cara.
            var craters = new[] { new Vector3(-0.18f, 0.12f, 0.26f), new Vector3(0.2f, -0.14f, 0.18f), new Vector3(0.05f, 0.26f, 0.12f) };
            foreach (var c in craters)
            {
                var r = Node(disc, "Crater", new Vector2(0.5f, 0.5f), new Vector2(size * c.z, size * c.z));
                r.anchoredPosition = new Vector2(c.x * size, c.y * size);
                Img(r, DiscSprite.Get(), new Color(0.55f, 0.5f, 0.7f, 0.16f));
            }
        }

        private static void AddSurface(RectTransform parent, WorldAmbient ambient, GameWorld world)
        {
            // Luz del borde del horizonte (respira despacio) y la superficie de arcilla encima.
            var rim = Node(parent, "HorizonGlow", new Vector2(0.5f, world.SurfaceHeight), new Vector2(1500f, 300f));
            var rimImg = Img(rim, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.16f));
            ambient.Breathe(rimImg, 0.16f, 0.05f, 0.6f);

            var surface = Layer(parent, "MoonSurface");
            surface.anchorMax = new Vector2(1f, world.SurfaceHeight + 0.02f); // la franja incluye un margen sobre el arco
            Img(surface, LunarSurfaceSprite.Get(world.SurfaceColor), Color.white);
        }

        private static void AddDome(RectTransform parent, GameWorld world)
        {
            // La cúpula apoya en el punto más alto del arco de la superficie (el sprite de la superficie dibuja su cima a
            // 290/326 de la franja, que mide SurfaceHeight + 0,02 del alto).
            float rimY = (world.SurfaceHeight + 0.02f) * (290f / 326f);
            var dome = Node(parent, "Observatory", new Vector2(0.5f, rimY - 0.004f), new Vector2(330f, 330f));
            dome.pivot = new Vector2(0.5f, ObservatorySprite.BaseFraction);
            Img(dome, ObservatorySprite.Get(), Color.white);
        }

        private static void AddAsteroid(RectTransform parent, WorldAmbient ambient, Vector3 a, int i)
        {
            // Asteroide de arcilla (el mismo de Parejas), apagado hacia la noche: es fondo, no debe competir.
            var rect = Node(parent, "Asteroid", new Vector2(a.x, a.y), new Vector2(a.z, a.z));
            Img(rect, SymbolSprite.Get(ShapeKind.Asteroid, i % 2 == 0 ? 1 : 0), new Color(0.62f, 0.62f, 0.78f, 0.9f));
            rect.localRotation = Quaternion.Euler(0f, 0f, i * 70f);
            ambient.Sway(rect, 10f + i * 4f, 0.25f + i * 0.08f);
            ambient.Spin(rect, (i % 2 == 0 ? 1f : -1f) * (5f + i * 2f));
        }

        private static void AddOrbits(RectTransform parent, WorldAmbient ambient, GameWorld world)
        {
            var satColors = new[] { NeuroStyle.Lime, NeuroStyle.Sky, NeuroStyle.Sun, NeuroStyle.Grape };
            for (int i = 0; i < world.OrbitRings; i++)
            {
                float d = 820f + i * 420f;
                var ring = Node(parent, "Orbit", world.OrbitCenter, new Vector2(d, d));
                Img(ring, ThinRingSprite(), NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.09f - i * 0.012f));
                // Un satélite por órbita, en sentidos alternos y a distinta velocidad.
                var sat = Node(parent, "Satellite", world.OrbitCenter, new Vector2(26f + i * 4f, 26f + i * 4f));
                Img(sat, DiscSprite.Get(), NeuroStyle.WithAlpha(satColors[i % satColors.Length], 0.8f));
                var satGlow = Node(sat, "Glow", new Vector2(0.5f, 0.5f), new Vector2(110f, 110f));
                Img(satGlow, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(satColors[i % satColors.Length], 0.35f));
                ambient.Orbit(sat, d * 0.5f * 0.985f, (i % 2 == 0 ? 1f : -1f) * (0.22f - i * 0.03f), i * 1.7f);
            }
        }

        private static void AddPlanet(RectTransform parent, Vector3 planet, Color color)
        {
            // Planeta de arcilla semi oculto en una esquina: borde tinta, relleno apagado hacia la noche (es fondo,
            // no debe competir con el juego) y un brillo arriba a la izquierda.
            var anchor = new Vector2(planet.x, planet.y);
            float size = planet.z;
            var glow = Node(parent, "PlanetGlow", anchor, new Vector2(size * 1.9f, size * 1.9f));
            Img(glow, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(color, 0.22f));
            var border = Node(parent, "Planet", anchor, new Vector2(size + 18f, size + 18f));
            Img(border, DiscSprite.Get(), NeuroStyle.Ink);
            var fill = Node(border, "Fill", new Vector2(0.5f, 0.5f), new Vector2(size, size));
            Img(fill, DiscSprite.Get(), Color.Lerp(NeuroStyle.NightMid, color, 0.62f));
            var band = Node(fill, "Band", new Vector2(0.5f, 0.42f), new Vector2(size * 0.95f, size * 0.12f));
            var bandImg = Img(band, RoundedRectSprite.Get(16), NeuroStyle.WithAlpha(NeuroStyle.Ink, 0.18f));
            bandImg.type = Image.Type.Sliced;
            var shine = Node(fill, "Shine", new Vector2(0.33f, 0.68f), new Vector2(size * 0.32f, size * 0.32f));
            Img(shine, RadialGlowSprite.Get(), NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.35f));
        }

        private static void AddConstellations(RectTransform parent, WorldAmbient ambient, int count, int seed)
        {
            var rng = new System.Random(seed);
            // En los márgenes (arriba-izquierda, abajo-derecha, medio-izquierda), lejos del tablero central.
            var zones = new[] { new Rect(0.05f, 0.70f, 0.35f, 0.14f), new Rect(0.60f, 0.06f, 0.35f, 0.14f), new Rect(0.04f, 0.30f, 0.18f, 0.2f) };
            for (int c = 0; c < count && c < zones.Length; c++)
            {
                var zone = zones[c];
                int n = 4 + rng.Next(3);
                var points = new List<Vector2>();
                for (int i = 0; i < n; i++)
                    points.Add(new Vector2(zone.x + (float)rng.NextDouble() * zone.width, zone.y + (float)rng.NextDouble() * zone.height));
                points.Sort((a, b) => a.x.CompareTo(b.x));
                for (int i = 0; i < n - 1; i++) AddLine(parent, ambient, points[i], points[i + 1]);
                for (int i = 0; i < n; i++)
                {
                    var star = Node(parent, "ConstellationStar", points[i], new Vector2(46f, 46f));
                    var img = Img(star, SparkleSprite.Get(), NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.75f));
                    ambient.Breathe(img, 0.6f, 0.35f, 1.1f + (float)rng.NextDouble() * 1.4f);
                }
            }
        }

        private static void AddLine(RectTransform parent, WorldAmbient ambient, Vector2 a, Vector2 b)
        {
            // Largo y ángulo dependen del tamaño real de la pantalla (varía según el alto del teléfono):
            // los calcula WorldAmbient cuando ese tamaño ya se conoce.
            var line = Node(parent, "ConstellationLine", (a + b) * 0.5f, new Vector2(0f, 3f));
            Img(line, null, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.16f));
            ambient.Connect(line, a, b);
        }

        // ---- utilidades ----

        private static RectTransform Layer(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform Node(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        private static Image Img(RectTransform rect, Sprite sprite, Color color)
        {
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Sprite _thinRing;

        /// <summary>Anillo fino (órbita), blanco: el <see cref="RingSprite"/> es demasiado grueso para esto.</summary>
        private static Sprite ThinRingSprite()
        {
            if (_thinRing != null) return _thinRing;
            const int size = 512;
            const float radius = 0.492f, half = 0.0035f, edge = 0.002f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size - 0.5f, dy = (y + 0.5f) / size - 0.5f;
                    float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius);
                    float a = 1f - Mathf.Clamp01((d - half) / edge);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(pixels);
            tex.Apply();
            _thinRing = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _thinRing;
        }
    }

    /// <summary>Animaciones lentas del fondo de un mundo (respirar, mecerse, girar, orbitar, meteoros, letras que suben).
    /// Todo en tiempo sin escala y sin tocar nada del juego.</summary>
    public sealed class WorldAmbient : MonoBehaviour
    {
        private RectTransform _area;
        private float _time;

        private readonly List<(Image img, float baseA, float amp, float speed)> _breathers = new List<(Image, float, float, float)>();
        private readonly List<(RectTransform rect, Vector2 home, float amp, float speed)> _swayers = new List<(RectTransform, Vector2, float, float)>();
        private readonly List<(RectTransform rect, float degPerSecond, Quaternion home)> _spinners = new List<(RectTransform, float, Quaternion)>();
        private readonly List<(RectTransform rect, float radius, float speed, float phase)> _orbiters = new List<(RectTransform, float, float, float)>();
        private readonly List<(RectTransform rect, Vector2 a, Vector2 b)> _lines = new List<(RectTransform, Vector2, Vector2)>();
        private Vector2 _linesLaidFor;
        private Vector2 _restSize = new Vector2(-1f, -1f); // tamaño con el que se dibujó el reposo (-1 = no dibujado)

        private float _meteorEvery, _nextMeteor;
        private RectTransform[] _meteors;
        private Image[] _meteorImages;
        private float[] _meteorAge;
        private Vector2[] _meteorFrom, _meteorDir;

        private RectTransform[] _glyphs;
        private Vector2[] _glyphNorm;
        private float[] _glyphSpeed;
        private Vector2[] _glyphHome;

        public void Init(RectTransform area) => _area = area;

        public void Breathe(Image img, float baseAlpha, float amplitude, float speed = 0.8f) =>
            _breathers.Add((img, baseAlpha, amplitude, speed));

        public void Sway(RectTransform rect, float amplitude, float speed) =>
            _swayers.Add((rect, rect.anchoredPosition, amplitude, speed));

        public void Spin(RectTransform rect, float degPerSecond) => _spinners.Add((rect, degPerSecond, rect.localRotation));

        public void Orbit(RectTransform rect, float radius, float speed, float phase) =>
            _orbiters.Add((rect, radius, speed, phase));

        /// <summary>Línea entre dos puntos normalizados del área (constelaciones).</summary>
        public void Connect(RectTransform line, Vector2 a, Vector2 b) => _lines.Add((line, a, b));

        public void EnableMeteors(float everySeconds)
        {
            _meteorEvery = everySeconds;
            _nextMeteor = 0.8f;
            _meteors = new RectTransform[2];
            _meteorImages = new Image[2];
            _meteorAge = new[] { 99f, 99f };
            _meteorFrom = new Vector2[2];
            _meteorDir = new Vector2[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Meteor");
                go.transform.SetParent(_area, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0f); // la cabeza va adelante; la estela queda atrás
                rect.sizeDelta = new Vector2(14f, 300f);
                var img = go.AddComponent<Image>();
                img.sprite = RadialGlowSprite.Get();
                img.raycastTarget = false;
                img.color = new Color(1f, 1f, 1f, 0f);
                _meteors[i] = rect;
                _meteorImages[i] = img;
            }
        }

        public void AddGlyphs(string letters, int count)
        {
            var rng = new System.Random(3);
            var colors = new[] { NeuroStyle.Grape, NeuroStyle.Coral, NeuroStyle.Cream, NeuroStyle.Sky };
            _glyphs = new RectTransform[count];
            _glyphNorm = new Vector2[count];
            _glyphSpeed = new float[count];
            _glyphHome = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Glyph");
                go.transform.SetParent(_area, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(220f, 220f);
                rect.localRotation = Quaternion.Euler(0f, 0f, ((float)rng.NextDouble() - 0.5f) * 40f);
                var text = go.AddComponent<Text>();
                text.font = UiFonts.Bold;
                text.text = letters[rng.Next(letters.Length)].ToString();
                text.fontSize = 70 + rng.Next(90);
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                text.color = NeuroStyle.WithAlpha(colors[i % colors.Length], 0.07f + (float)rng.NextDouble() * 0.07f);
                _glyphs[i] = rect;
                _glyphNorm[i] = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                _glyphSpeed[i] = 0.012f + (float)rng.NextDouble() * 0.02f;
                _glyphHome[i] = _glyphNorm[i];
            }
        }

        private void Update() => Step(Time.unscaledDeltaTime);

        /// <summary>Un cuadro de <paramref name="dt"/> segundos (público para las pruebas EditMode, que no tienen cuadros).</summary>
        public void Step(float dt)
        {
            _time += dt;
            if (_area == null) return;
            Vector2 size = _area.rect.size;

            if (_lines.Count > 0 && size != _linesLaidFor && size.x > 0f) LayoutLines(size); // es disposición, no movimiento

            if (!Motion.Decorative)
            {
                DrawRest(size);
                return;
            }
            _restSize = new Vector2(-1f, -1f);

            foreach (var b in _breathers)
            {
                var c = b.img.color;
                c.a = Mathf.Max(0f, b.baseA + b.amp * Mathf.Sin(_time * b.speed * Mathf.PI * 2f * 0.25f));
                b.img.color = c;
            }
            foreach (var s in _swayers)
                s.rect.anchoredPosition = s.home + new Vector2(Mathf.Sin(_time * s.speed) * s.amp, 0f);
            foreach (var sp in _spinners) sp.rect.Rotate(0f, 0f, sp.degPerSecond * dt);
            foreach (var o in _orbiters)
            {
                float a = o.phase + _time * o.speed;
                o.rect.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * o.radius;
            }

            if (_meteors != null) UpdateMeteors(dt, size);

            if (_glyphs != null)
            {
                for (int i = 0; i < _glyphs.Length; i++)
                {
                    _glyphNorm[i].y += _glyphSpeed[i] * dt;
                    if (_glyphNorm[i].y > 1.1f) _glyphNorm[i].y = -0.1f;
                    float sway = Mathf.Sin(_time * 0.4f + i) * 0.015f;
                    _glyphs[i].anchoredPosition = new Vector2((_glyphNorm[i].x + sway) * size.x, _glyphNorm[i].y * size.y);
                }
            }
        }

        /// <summary>Disposición de las líneas de las constelaciones (largo y giro según el tamaño del área); no es movimiento.</summary>
        private void LayoutLines(Vector2 size)
        {
            _linesLaidFor = size;
            foreach (var l in _lines)
            {
                var pa = Vector2.Scale(l.a, size);
                var pb = Vector2.Scale(l.b, size);
                l.rect.sizeDelta = new Vector2(Vector2.Distance(pa, pb), 3f);
                l.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg);
            }
        }

        /// <summary>"Quitar animaciones": el fondo queda en su estado de reposo (alfa base, posiciones de casa, sin
        /// estrellas fugaces) y no cambia más. Se dibuja una vez y solo se repite si cambia el tamaño del área.</summary>
        private void DrawRest(Vector2 size)
        {
            if (size == _restSize) return;
            _restSize = size;
            foreach (var b in _breathers)
            {
                var c = b.img.color;
                c.a = b.baseA;
                b.img.color = c;
            }
            foreach (var s in _swayers) s.rect.anchoredPosition = s.home;
            foreach (var sp in _spinners) sp.rect.localRotation = sp.home;
            foreach (var o in _orbiters) o.rect.anchoredPosition = new Vector2(Mathf.Cos(o.phase), Mathf.Sin(o.phase)) * o.radius;
            if (_meteors != null)
            {
                for (int i = 0; i < _meteors.Length; i++)
                {
                    _meteorAge[i] = 99f;
                    _meteorImages[i].color = new Color(1f, 1f, 1f, 0f);
                }
            }
            if (_glyphs != null)
            {
                for (int i = 0; i < _glyphs.Length; i++)
                {
                    _glyphNorm[i] = _glyphHome[i];
                    _glyphs[i].anchoredPosition = new Vector2(_glyphHome[i].x * size.x, _glyphHome[i].y * size.y);
                }
            }
        }

        private void UpdateMeteors(float dt, Vector2 size)
        {
            _nextMeteor -= dt;
            if (_nextMeteor <= 0f)
            {
                _nextMeteor = _meteorEvery; // ritmo regular a propósito: el mundo de las series tiene un patrón
                int slot = _meteorAge[0] > _meteorAge[1] ? 0 : 1;
                _meteorAge[slot] = 0f;
                float x = Random.Range(0.25f, 1.05f) * size.x;
                _meteorFrom[slot] = new Vector2(x, size.y * Random.Range(0.72f, 0.98f));
                _meteorDir[slot] = new Vector2(-0.82f, -0.57f); // siempre en diagonal hacia abajo-izquierda
                _meteors[slot].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_meteorDir[slot].y, _meteorDir[slot].x) * Mathf.Rad2Deg + 90f);
            }
            for (int i = 0; i < _meteors.Length; i++)
            {
                _meteorAge[i] += dt;
                const float life = 0.9f;
                float t = _meteorAge[i] / life;
                if (t > 1f)
                {
                    _meteorImages[i].color = new Color(1f, 1f, 1f, 0f);
                    continue;
                }
                _meteors[i].anchoredPosition = _meteorFrom[i] + _meteorDir[i] * (t * 900f);
                _meteorImages[i].color = new Color(1f, 0.95f, 0.85f, Mathf.Sin(t * Mathf.PI) * 0.85f);
            }
        }
    }
}
