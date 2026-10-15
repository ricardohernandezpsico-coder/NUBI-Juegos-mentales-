using NUnit.Framework;
using UnityEngine;
using NeuroVida.Games.Shared;

namespace NeuroVida.Games.Intrusa.Tests
{
    /// <summary>
    /// La estrella intrusa en plasticina (Tarea 101): todo sale de las losas del kit (las placas de las palabras, las tres opciones de «¿Qué las une?», «Seguir» y el panel del final); las estrellas, sus halos y destellos, el cometa, el grabado y las 77 figuras se quedan procedurales. Estas pruebas
    /// cuidan que las losas existan (crema, lima y coral: al elegir una opción cambia el tono, y la correcta no elegida se tiñe lima sin marca) y que la interfaz se arme con y sin el atlas.
    /// </summary>
    public sealed class IntrusaPlasticTests
    {
        [SetUp]
        public void SetUp() { PlasticArt.Disabled = false; }

        [TearDown]
        public void TearDown() { PlasticArt.Disabled = false; }

        [Test]
        public void TheKitSlabsTheGameUses_Exist_InTheirThreeStates()
        {
            foreach (var tone in new[] { SlabTone.Cream, SlabTone.Lime, SlabTone.Coral })
            {
                Assert.IsTrue(PlasticSlab.Available(SlabSize.Button, tone), "falta la losa md " + tone + " (opciones y «Seguir»)");
                Assert.IsNotNull(PlasticSlab.Sprite(SlabSize.Chip, tone), "falta la losa chica " + tone + " (o la mediana que la reemplaza)");
            }
            Assert.IsTrue(PlasticSlab.Available(SlabSize.Panel, SlabTone.Night) || PlasticSlab.Available(SlabSize.Button, SlabTone.Night), "el panel del final");
        }

        [Test]
        public void TheInterfaceBuilds_WithAndWithoutThePlasticAtlas_AndTheSlabsAreOnlyThereWithIt()
        {
            foreach (bool disabled in new[] { false, true })
            {
                PlasticArt.Disabled = disabled;
                var go = new GameObject("IntrusaPlasticAudit");
                try
                {
                    var controller = go.AddComponent<IntrusaGameController>();
                    var build = typeof(IntrusaGameController).GetMethod("BuildUi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    build.Invoke(controller, null);
                    int slabs = 0;
                    foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (t.name == "Slab") slabs++;
                    if (disabled) Assert.AreEqual(0, slabs, "sin plasticina no hay losas");
                    else Assert.GreaterOrEqual(slabs, 5 + 3 + 1 + 1, "con plasticina: 5 placas, 3 opciones, «Seguir» y el panel del final son losas (" + slabs + ")");
                    int touches = go.GetComponentsInChildren<PlasticTouch>(true).Length;           // Tarea 104: las 3 opciones y «Seguir» se ven presionadas (con la huella) al tocarlas
                    Assert.AreEqual(disabled ? 0 : 4, touches, "las tres opciones y «Seguir» llevan su estado presionado solo con plasticina");
                }
                finally { Object.DestroyImmediate(go); }
            }
        }
    }
}
