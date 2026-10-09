using System;

namespace NeuroVida.Games.Piloto
{
    /// <summary>
    /// Lo que suma un vuelo de Piloto Estelar (lógica pura): pilotaje (tiempo dentro de la ruta y ventanas de 1,5 s), señales (atrapadas, que se fueron, toques equivocados), racha, puntos e hiperimpulso. TODO se mide con las dos tareas juntas: no hay una cuenta de
    /// una tarea sola. De acá salen «En la ruta», «Señales de tu misión», «Toques equivocados» y la medida «Tus señales a los mandos».
    /// </summary>
    public sealed class PilotRun
    {
        // pilotaje
        public float InsideSeconds { get; private set; }
        public float FlightSeconds { get; private set; }
        public int Windows { get; private set; }
        public int WindowsPassed { get; private set; }
        /// <summary>Ventanas de pilotaje limpias seguidas (≥ 85 % dentro).</summary>
        public int CleanWindows { get; private set; }
        private float _windowT, _windowIn;

        // señales
        /// <summary>Señales de la misión atrapadas / señales de la misión resueltas (atrapadas + las que se fueron) / toques equivocados / señales que no eran de la misión resueltas (tocadas por error + las que se dejaron pasar).</summary>
        public int Hits { get; private set; }
        public int Targets { get; private set; }
        public int FalseAlarms { get; private set; }
        public int NonTargets { get; private set; }
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        public int Points { get; private set; }
        public int HyperCount { get; private set; }

        public int Resolved => Targets + NonTargets;
        public float InLaneFraction => FlightSeconds > 0.001f ? InsideSeconds / FlightSeconds : 0f;
        public float? SignalScore => PilotContract.SignalScore(Hits, FalseAlarms, Targets);
        public int Score => PilotContract.Score(SignalScore, InLaneFraction);

        /// <summary>Avanza el vuelo <paramref name="dt"/> s con la nave dentro o fuera de la ruta. Devuelve true cuando se cierra una ventana de 1,5 s; <paramref name="passed"/> dice si fue limpia.</summary>
        public bool Fly(float dt, bool inside, out bool passed)
        {
            passed = false;
            if (dt <= 0f) return false;
            FlightSeconds += dt;
            _windowT += dt;
            if (inside) { InsideSeconds += dt; _windowIn += dt; }
            if (_windowT < PilotContract.DriveWindowSeconds) return false;
            passed = _windowIn / _windowT >= PilotContract.DriveWindowPass;
            Windows++;
            if (passed) { WindowsPassed++; CleanWindows++; } else CleanWindows = 0;
            _windowT = _windowIn = 0f;
            return true;
        }

        /// <summary>Una señal de la misión atrapada: suma los puntos (el doble en hiperimpulso) y devuelve cuántos.</summary>
        public int Catch(bool hyper)
        {
            Hits++;
            Targets++;
            int pts = PilotContract.Points(hyper);
            Points += pts;
            Good();
            return pts;
        }

        /// <summary>Una señal de la misión que se fue sin atrapar (omisión): corta la racha.</summary>
        public void Miss()
        {
            Targets++;
            Streak = 0;
        }

        /// <summary>Se tocó una señal que NO era de la misión (toque equivocado): corta la racha.</summary>
        public void FalseAlarm()
        {
            FalseAlarms++;
            NonTargets++;
            Streak = 0;
        }

        /// <summary>Una señal que no era de la misión se dejó pasar (acierto del motor, en silencio): cuenta para la racha.</summary>
        public void Ignore()
        {
            NonTargets++;
            Good();
        }

        private void Good()
        {
            Streak++;
            BestStreak = Math.Max(BestStreak, Streak);
        }

        /// <summary>¿Toca hiperimpulso? Cada 5 señales bien resueltas seguidas, con al menos 3 ventanas de pilotaje limpias seguidas (y sin otro en curso).</summary>
        public bool ShouldHyper(bool hyperActive) =>
            !hyperActive && Streak > 0 && Streak % PilotContract.HyperEvery == 0 && CleanWindows >= PilotContract.HyperCleanWindows;

        public void CountHyper() { HyperCount++; }
    }
}
