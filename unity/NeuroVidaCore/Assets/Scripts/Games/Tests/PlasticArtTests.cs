using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Tests
{
    /// <summary>
    /// La plasticina de los juegos (Tarea 94): los atlas de <c>Resources/Plasticina</c> están completos y pesan poco, las losas tienen su nine-slice, el texto de las pastillas se lee contra su losa, y SIN el atlas todo cae al arte procedural de siempre (un juego se migra sin romper a los demás).
    /// </summary>
    public sealed class PlasticArtTests
    {
        /// <summary>Lo más que un juego puede sumar al APK (bytes, estimado: el atlas sin compresión de textura, deflate). Meta de Ricardo: ~1,5 MB por juego.</summary>
        private const int GameBudgetBytes = 1536 * 1024;
        /// <summary>Lo común (losas, marcas, anillo) se paga UNA vez para los 17 juegos.</summary>
        private const int CommonBudgetBytes = 3 * 1024 * 1024;

        private readonly List<GameObject> _made = new List<GameObject>();

        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; PlasticArt.Reset(); }

        [TearDown]
        public void TearDown()
        {
            PlasticArt.Disabled = false;
            PlasticArt.Reset();
            foreach (var g in _made) if (g != null) UnityEngine.Object.DestroyImmediate(g);
            _made.Clear();
        }

        private RectTransform NewArea()
        {
            var go = new GameObject("PlasticArea", typeof(RectTransform));
            _made.Add(go);
            ((RectTransform)go.transform).sizeDelta = new Vector2(1080f, 1920f);
            return (RectTransform)go.transform;
        }

        private static readonly string[] Groups = { "comun", "nubi", "radar", "piloto", "freno", "calculo", "disparate", "satelites", "rastro", "stroop", "punta" };

        // ------------------------------------------------------------------ los atlas

        [Test]
        public void EveryGroupExists_AndEverySpriteFitsItsAtlas()
        {
            foreach (var name in Groups)
            {
                var g = PlasticArt.Group(name);
                Assert.IsNotNull(g, "falta el atlas de plasticina «" + name + "» (Assets/Resources/Plasticina/" + name + "): correr tools/juegos/empaqueta.py");
                Assert.AreEqual(g.Manifest.ancho, g.Atlas.width, name + ": el manifiesto y la textura no miden lo mismo");
                Assert.AreEqual(g.Manifest.alto, g.Atlas.height, name);
                int n = 0;
                foreach (var sprite in g.Names)
                {
                    var e = g.Entry(sprite);
                    Assert.GreaterOrEqual(e.x, 0, name + "/" + sprite);
                    Assert.GreaterOrEqual(e.y, 0, name + "/" + sprite);
                    Assert.LessOrEqual(e.x + e.w, g.Atlas.width, name + "/" + sprite + " se sale del atlas por la derecha");
                    Assert.LessOrEqual(e.y + e.h, g.Atlas.height, name + "/" + sprite + " se sale del atlas por arriba");
                    var s = g.Get(sprite);
                    Assert.IsNotNull(s, name + "/" + sprite);
                    Assert.AreEqual(e.w, Mathf.RoundToInt(s.rect.width), name + "/" + sprite);
                    n++;
                }
                Assert.Greater(n, 0, name + " no tiene sprites");
            }
        }

        [Test]
        public void EachGroupWeighsUnderItsBudget()
        {
            foreach (var name in Groups)
            {
                var g = PlasticArt.Group(name);
                Assert.IsNotNull(g, name);
                int budget = name == "radar" ? GameBudgetBytes : name == "comun" ? CommonBudgetBytes : GameBudgetBytes;
                Assert.LessOrEqual(g.Manifest.peso.deflate_rgba32, budget,
                    name + ": el atlas suma ~" + g.Manifest.peso.deflate_rgba32 / 1024 + " KB al APK sin compresión de textura (tope " + budget / 1024 + " KB). Se baja el tamaño de las imágenes o se comprime la textura (ASTC: ~" + g.Manifest.peso.astc6x6 / 1024 + " KB)");
            }
        }

        [Test]
        public void EverySlabHasItsNineSliceAndMargins()
        {
            var g = PlasticArt.Group("comun");
            Assert.IsNotNull(g);
            int slabs = 0;
            foreach (var sprite in g.Names)
            {
                if (!sprite.StartsWith("losa_")) continue;
                var e = g.Entry(sprite);
                Assert.IsTrue(e.borde != null && e.borde.Length == 4, sprite + ": sin borde de nine-slice");
                foreach (var b in e.borde) Assert.Greater(b, 20, sprite + ": borde de nine-slice demasiado chico");
                Assert.IsTrue(e.margen != null && e.margen.Length == 4, sprite + ": sin márgenes de sombra");
                var s = g.Get(sprite);
                Assert.AreEqual(e.borde[0], s.border.x, 0.01f, sprite);
                slabs++;
            }
            Assert.AreEqual(26, slabs, "las losas del kit (ch 6, md 12, gr 2) y las 6 de las casillas «noche» (normal, presionada, acierto, error, apagada y la apagada presionada de las herramientas de Carga exacta)");
            // las combinaciones que usan los juegos existen, normal y presionada
            foreach (SlabTone tone in new[] { SlabTone.Sun, SlabTone.Cream, SlabTone.Sky, SlabTone.Lilac, SlabTone.Coral, SlabTone.Lime, SlabTone.Night, SlabTone.Hit, SlabTone.Miss, SlabTone.Dim })
                Assert.IsTrue(PlasticSlab.Available(SlabSize.Button, tone), "falta la losa md " + tone);
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Panel, SlabTone.Cream), "falta la lámina grande crema");
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Chip, SlabTone.Sun) && PlasticSlab.Available(SlabSize.Chip, SlabTone.Lime), "faltan las losas chicas");
            Assert.IsNotNull(PlasticSlab.Sprite(SlabSize.Button, SlabTone.Night, true), "la casilla «noche» tiene su imagen presionada");
            Assert.AreNotEqual(PlasticSlab.Sprite(SlabSize.Button, SlabTone.Dim, false), PlasticSlab.Sprite(SlabSize.Button, SlabTone.Dim, true), "la losa apagada (las herramientas de Carga exacta, que se tocan) tiene su imagen presionada");
            Assert.AreEqual(SlabSize.Button, PlasticSlab.Resolve(SlabSize.Chip, SlabTone.Coral), "sin losa chica coral, se usa la mediana (como en la app)");
        }

        // ------------------------------------------------------------------ el texto se lee contra la losa

        private static float Luminance(Color32 c)
        {
            float R(float v) { v /= 255f; return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f); }
            return 0.2126f * R(c.r) + 0.7152f * R(c.g) + 0.0722f * R(c.b);
        }

        private static float Contrast(Color32 a, Color32 b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        /// <summary>El atlas como textura LEGIBLE (el importado no lo es): se lee el PNG del disco.</summary>
        internal static Texture2D ReadAtlas(string group)
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Plasticina", group, "atlas.png");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "no se pudo leer " + path);
            return tex;
        }

        /// <summary>El color del centro de la cara de una losa (la parte lisa que se estira y donde va el texto).</summary>
        internal static Color32 SlabFace(Texture2D atlas, PlasticEntry e)
        {
            // el centro del CUERPO: a la izquierda y arriba están los márgenes de la sombra
            int cx = e.x + (e.borde[0] + (e.w - e.borde[2])) / 2, cy = e.y + (e.borde[1] + (e.h - e.borde[3])) / 2;
            return atlas.GetPixel(cx, cy);
        }

        [Test]
        public void TheTextOfEverySlabIsReadable()
        {
            var g = PlasticArt.Group("comun");
            Assert.IsNotNull(g);
            var atlas = ReadAtlas("comun");
            try
            {
                var ink = (Color32)NeuroStyle.Ink;
                var white = new Color32(255, 255, 255, 255);
                foreach (var pair in new[] { ("sol", false), ("crema", false), ("lima", false), ("cielo", false), ("coral", false), ("lila", false), ("noche", true), ("acierto", true), ("error", true), ("apagada", true) })
                {
                    var e = g.Entry("losa_md_" + pair.Item1);
                    Assert.IsNotNull(e, pair.Item1);
                    var face = SlabFace(atlas, e);
                    // sobre las losas claras va tinta; sobre las oscuras (las casillas de los juegos), texto claro
                    float c = Contrast(face, pair.Item2 ? white : ink);
                    Assert.GreaterOrEqual(c, 4.5f, "losa md " + pair.Item1 + ": el texto " + (pair.Item2 ? "claro" : "tinta") + " rinde " + c.ToString("0.0") + ":1 (mínimo 4,5)");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(atlas); }
        }

        [Test]
        public void TheRenderPipelineGivesTheSameKitAsTheApp()
        {
            var g = PlasticArt.Group("comun");
            Assert.IsNotNull(g);
            float dif = g.Measure("control_losa_crema_dif_media", 1f);
            Assert.Less(dif, 0.03f, "la losa crema re-renderizada con tools/juegos debe parecerse a la del kit de la app (diferencia media " + dif.ToString("0.000") + ")");
        }

        // ------------------------------------------------------------------ sin el atlas, el arte de siempre

        [Test]
        public void WithoutThePlasticAtlas_EverythingFallsBackToTheProceduralArt()
        {
            PlasticArt.Disabled = true;
            Assert.IsNull(PlasticArt.Group("comun"));
            Assert.IsFalse(PlasticSlab.Available(SlabSize.Button, SlabTone.Sun));
            Assert.IsFalse(NubiTeacherSprite.IsPlastic);
            Assert.IsNotNull(NubiTeacherSprite.Get(), "la Nubi plana de siempre");
            Assert.IsNotNull(AnswerMarkSprite.Check());
            Assert.IsNotNull(AnswerMarkSprite.Cross());
            var noHost = new GameObject("host").AddComponent<HostBehaviour>();
            _made.Add(noHost.gameObject);
            var hud = new GameHud(NewArea(), "Sin plasticina", 48f, noHost);
            hud.SetLevel(1);
            hud.SetInfo("1 de 8");
            foreach (var slab in hud.Rect.GetComponentsInChildren<Transform>(true)) Assert.AreNotEqual("Slab", slab.name, "sin atlas no hay losas");
            var pause = PauseMenu.Create(NewArea(), () => { }, () => { }, () => { });
            _made.Add(pause.gameObject);
            foreach (var slab in pause.GetComponentsInChildren<Transform>(true)) Assert.AreNotEqual("Slab", slab.name, "la pausa sin atlas es la de siempre");
        }

        [Test]
        public void WithThePlasticAtlas_TheHudPauseAndNubiUseIt()
        {
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Chip, SlabTone.Cream));
            var host = new GameObject("host").AddComponent<HostBehaviour>();
            _made.Add(host.gameObject);
            var hud = new GameHud(NewArea(), "Con plasticina", 48f, host);
            hud.SetLevel(2);
            hud.SetInfo("3 de 8");
            Assert.IsNotNull(hud.Rect.Find("LevelChip/Slab"), "la pastilla de nivel lleva su losa");
            Assert.IsNotNull(hud.Rect.Find("InfoChip/Slab"));
            Assert.IsNotNull(hud.Rect.Find("StreakPill/Slab"));
            Assert.AreEqual(NeuroStyle.Ink, hud.Rect.Find("LevelChip/Text").GetComponent<Text>().color, "texto tinta sobre la losa clara");
            var pause = PauseMenu.Create(NewArea(), () => { }, () => { }, () => { });
            _made.Add(pause.gameObject);
            int slabs = 0;
            foreach (var t in pause.GetComponentsInChildren<Transform>(true)) if (t.name == "Slab") slabs++;
            Assert.GreaterOrEqual(slabs, 4, "el panel y los botones de la pausa son losas");
            Assert.IsTrue(NubiTeacherSprite.IsPlastic, "la Nubi de los tutoriales es la 3D");
            var nubi = NubiTeacherSprite.Get();
            Assert.Greater(nubi.rect.width, nubi.rect.height, "la Nubi 3D no es cuadrada (con la sombra mide 720 × 548)");
        }

        [Test]
        public void ThePressedSlabIsADifferentImage_AndTheLabelDropsAndComesBack()
        {
            var go = new GameObject("Boton", typeof(RectTransform));
            _made.Add(go);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(640f, 150f);
            var img = go.AddComponent<Image>();
            var slab = PlasticSlab.TryStyle(img, SlabSize.Button, SlabTone.Sun, hit: true);
            Assert.IsNotNull(slab);
            Assert.AreEqual(0f, img.color.a, "el fondo de siempre queda invisible");
            Assert.IsTrue(img.raycastTarget, "pero sigue recibiendo los toques");
            var label = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            label.SetParent(rect, false);
            var press = go.AddComponent<PlasticPress>();
            press.Init(slab, SlabSize.Button, SlabTone.Sun, label);
            var normal = slab.sprite;
            press.OnPointerDown(null);
            Assert.AreNotSame(normal, slab.sprite, "la losa presionada es otra imagen");
            Assert.Less(label.anchoredPosition.y, 0f, "el texto baja al apoyar el dedo");
            press.OnPointerUp(null);
            Assert.AreSame(normal, slab.sprite);
            Assert.AreEqual(0f, label.anchoredPosition.y, 1e-4f, "y vuelve");
        }

        [Test]
        public void ASlabBodyIsExactlyTheRectOfItsHost_AndTheImageStickOutByTheShadowMargins()
        {
            var go = new GameObject("Host", typeof(RectTransform));
            _made.Add(go);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(500f, 150f);
            var img = go.AddComponent<Image>();
            var slab = PlasticSlab.TryStyle(img, SlabSize.Button, SlabTone.Cream);
            Assert.IsNotNull(slab);
            var m = PlasticSlab.Margins(SlabSize.Button, SlabTone.Cream);
            var r = (RectTransform)slab.transform;
            Assert.AreEqual(-m.x, r.offsetMin.x, 1e-3f);
            Assert.AreEqual(-m.w, r.offsetMin.y, 1e-3f);
            Assert.AreEqual(m.z, r.offsetMax.x, 1e-3f);
            Assert.AreEqual(m.y, r.offsetMax.y, 1e-3f);
            Assert.AreEqual(Image.Type.Sliced, slab.type);
            Assert.Greater(m.w, m.y, "la sombra cae hacia abajo: más margen abajo que arriba");
        }

        // ------------------------------------------------------------------ las pruebas que arman cosas necesitan un MonoBehaviour que haga de «runner»
        private sealed class HostBehaviour : MonoBehaviour { }
    }
}
