using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Piloto
{
    /// <summary>Una baliza de la ruta: a qué distancia está (dp recorridos desde el inicio), dónde está el centro de la ruta ahí y cuánto mide de medio ancho. Cada una guarda lo suyo al crearse. <see cref="LitAt"/> / <see cref="BadAt"/>: cuándo se encendió (la nave pasó dentro) o parpadeó (pasó fuera).</summary>
    public sealed class Beacon
    {
        public float P, Cx, Half;
        public float LitAt = -10f, BadAt = -10f;
        public bool Passed;
    }

    /// <summary>
    /// La ruta de balizas (lógica pura): una baliza cada <see cref="PilotContract.BeaconGap"/> dp. Cada baliza guarda su centro y su ancho al crearse, así un cambio de nivel no deforma lo ya visible: la curva y el ancho se acercan de a poco (15 % por baliza) a los del
    /// nivel de pilotaje. Entre dos balizas el centro y el ancho se interpolan.
    /// </summary>
    public sealed class PilotRoute
    {
        private readonly List<Beacon> _beacons = new List<Beacon>();
        private float _curve, _half;

        public IReadOnlyList<Beacon> Beacons => _beacons;
        /// <summary>La distancia de la próxima baliza por crear.</summary>
        public float NextP { get; private set; }

        public PilotRoute(int driveLevel)
        {
            _curve = PilotContract.Curve(driveLevel);
            _half = PilotContract.HalfWidth(driveLevel);
        }

        /// <summary>Crea balizas hasta cubrir <paramref name="upToP"/> dp; <paramref name="driveLevel"/> es el nivel con que se vuela AHORA (en el inicio suave, dos menos).</summary>
        public void Extend(float upToP, int driveLevel)
        {
            while (NextP < upToP)
            {
                _curve += (PilotContract.Curve(driveLevel) - _curve) * 0.15f;
                _half += (PilotContract.HalfWidth(driveLevel) - _half) * 0.15f;
                _beacons.Add(new Beacon { P = NextP, Cx = PilotContract.CenterAt(NextP, _curve, _half), Half = _half });
                NextP += PilotContract.BeaconGap;
            }
        }

        /// <summary>Descarta las balizas que quedaron atrás (a menos de <paramref name="minP"/> dp), dejando una de respaldo.</summary>
        public void Prune(float minP)
        {
            while (_beacons.Count > 2 && _beacons[1].P < minP) _beacons.RemoveAt(0);
        }

        /// <summary>Centro y medio ancho de la ruta a <paramref name="p"/> dp (interpolados entre las dos balizas vecinas; fuera del rango, los de la baliza más cercana).</summary>
        public void At(float p, out float center, out float half)
        {
            int n = _beacons.Count;
            if (n == 0) { center = PilotContract.FieldWidth * 0.5f; half = PilotContract.HalfWidth(1); return; }
            if (p <= _beacons[0].P) { center = _beacons[0].Cx; half = _beacons[0].Half; return; }
            for (int i = 0; i < n - 1; i++)
            {
                var a = _beacons[i];
                var b = _beacons[i + 1];
                if (p >= a.P && p < b.P)
                {
                    float k = (p - a.P) / (b.P - a.P);
                    center = a.Cx + (b.Cx - a.Cx) * k;
                    half = a.Half + (b.Half - a.Half) * k;
                    return;
                }
            }
            center = _beacons[n - 1].Cx;
            half = _beacons[n - 1].Half;
        }
    }
}
