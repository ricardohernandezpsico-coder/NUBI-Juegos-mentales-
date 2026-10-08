using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuroVida.Games.Parejas
{
    public enum ConState { Down, Up, Done }

    /// <summary>Cómo se encontró un grupo: <see cref="Memory"/> = fuiste directo a una compañera YA VISTA (línea dorada continua); <see cref="New"/> = la luz que abriste era nueva, a la primera vista (línea celeste punteada).</summary>
    public enum ConLine { None, Memory, New }

    /// <summary>Una luz del cielo: el objeto que esconde (su tipo y su variante, 1 = el gemelo), su grupo, dónde está y en qué estado.</summary>
    public sealed class ConLight
    {
        public int Id, Kind, Variant, Size, Group;
        public string Key;
        public ConPt Pos;
        public ConState State;
        public bool Seen;
        public ConLine Ring;
        /// <summary>Cuándo aparece la luz (ms del reloj de juego): se puede tocar 150 ms después.</summary>
        public float AppearAtMs;
    }

    /// <summary>Una línea entre dos luces ya unidas: su tipo, su curva y cuándo empieza a trazarse (la segunda línea de un trío sale 200 ms después).</summary>
    public sealed class ConLink
    {
        public ConLight A, B;
        public ConLine Type;
        public ConLinkGeo Geo;
        public float AtMs;
    }

    /// <summary>Todo lo que pasó con un toque, para que la pantalla lo muestre (el cielo no sabe de pantallas).</summary>
    public sealed class ConTapResult
    {
        public bool Accepted;
        public ConLight Opened;
        /// <summary>Las luces que se cerraron al instante porque se tocó otra (las dos abiertas que no coincidían).</summary>
        public List<ConLight> ClosedNow = new List<ConLight>();
        public bool FirstOfTurn, Matched, Opportunity, Hit, Miss, Useless, ThirdMissing;
        /// <summary>Las compañeras ya vistas que seguían escondidas (donde brilla la pista tras un «se te escapó»).</summary>
        public List<ConLight> Kin = new List<ConLight>();
        public List<ConLight> Completed = new List<ConLight>();
        public List<ConLink> NewLinks = new List<ConLink>();
        public bool CompletedByMemory, BoardDone;
        /// <summary>Si las abiertas quedaron esperando (no coincidían): cuándo se cierran solas.</summary>
        public List<ConLight> Pending = new List<ConLight>();
        public float PendingCloseAtMs;
    }

    /// <summary>
    /// UN cielo (docs/diseno-constelaciones.md §3): las luces, quién está abierta y las reglas de un toque, copiadas del <c>tap()</c> y el <c>complete()</c> del boceto aprobado. Puro, con pruebas.
    /// <list type="bullet">
    /// <item>Un turno es dar vuelta <c>size</c> luces iguales (2 en una pareja, 3 en un trío). Si la que se abre no coincide con la primera del turno, el turno termina y las abiertas quedan un momento
    /// (<see cref="ConstelacionContract.CloseAfterMs"/>) o hasta que se toque otra luz: NADA bloquea el toque, cada luz se puede tocar apenas termina de aparecer.</item>
    /// <item><b>Oportunidad de memoria</b>: desde la segunda luz del turno, si alguna compañera de la primera YA SE VIO antes y sigue escondida. <b>Acierto</b> = tocar una de esas compañeras; <b>se te escapó</b> = tocar otra.
    /// Sin compañera vista no hay oportunidad, y la primera luz del turno nunca se juzga.</item>
    /// <item>La <b>racha de memoria</b> sube con cada acierto y solo la corta un «se te escapó»: explorar luces nuevas o fallar sin oportunidad no la corta (continúa de un cielo al otro).</item>
    /// <item>Un grupo se completa cuando se abren todos sus miembros; el tipo de cada línea es el de su luz de llegada, y el aro de las luces el del grupo (dorado si alguna llegada fue de memoria).</item>
    /// </list>
    /// </summary>
    public sealed class ConstelacionSky
    {
        public readonly ConStage Stage;
        public readonly ConLight[] Lights;
        public readonly ConPlacement Placement;
        public readonly List<ConLight> Face = new List<ConLight>();
        public readonly List<ConLine> Types = new List<ConLine>();
        public readonly List<ConLink> Links = new List<ConLink>();
        /// <summary>Cada oportunidad de este cielo: true = acierto de memoria, false = se te escapó (los puntos de la fila «De memoria»).</summary>
        public readonly List<bool> Opps = new List<bool>();
        public List<ConLight> Pending { get; private set; } = new List<ConLight>();
        public float PendingCloseAtMs { get; private set; }

        public int Turns, Hits, Misses, Useless, GroupsFound, MemoryGroups;
        public int Streak, BestStreak;
        private readonly float _closeScale;

        public int OpportunityCount => Opps.Count;
        public bool Complete => Lights.All(l => l.State == ConState.Done);

        private ConstelacionSky(ConStage stage, ConLight[] lights, ConPlacement placement, int streak, int bestStreak, float closeScale)
        {
            Stage = stage;
            Lights = lights;
            Placement = placement;
            Streak = streak;
            BestStreak = Math.Max(bestStreak, streak);
            _closeScale = closeScale;
        }

        /// <summary>Un cielo nuevo de la etapa <paramref name="stage"/>: arma los grupos (los gemelos son DOS grupos distintos: el objeto y su versión parecida), mezcla las luces y las reparte por el cielo de <paramref name="w"/> × <paramref name="h"/> dp.
        /// <paramref name="startMs"/> es el reloj de juego en que empieza: las luces aparecen escalonadas (45 ms entre una y otra).</summary>
        public static ConstelacionSky Create(ConStage stage, Random rng, float w, float h, float startMs = 0f, int streak = 0, int bestStreak = 0, float closeScale = 1f)
        {
            var twinKinds = Shuffle(ConstelacionContract.TwinKinds.ToList(), rng).Take(stage.Twins).ToList();
            var groups = new List<(int kind, int variant, int size)>();
            foreach (int k in twinKinds) { groups.Add((k, 0, stage.Size)); groups.Add((k, 1, stage.Size)); }
            var rest = Shuffle(Enumerable.Range(0, ConstelacionContract.ObjectKinds).Where(k => !twinKinds.Contains(k)).ToList(), rng);
            int ri = rest.Count - 1;
            while (groups.Count < stage.Groups) groups.Add((rest[ri--], 0, stage.Size));
            // en la mezcla, algunos grupos (de los que no son gemelos) vienen de a tres
            int madeTrios = 0;
            for (int g = 0; g < groups.Count && madeTrios < stage.Trios; g++)
                if (!twinKinds.Contains(groups[g].kind)) { groups[g] = (groups[g].kind, groups[g].variant, 3); madeTrios++; }

            var lights = new List<ConLight>();
            for (int gi = 0; gi < groups.Count; gi++)
                for (int r = 0; r < groups[gi].size; r++)
                    lights.Add(new ConLight { Kind = groups[gi].kind, Variant = groups[gi].variant, Key = groups[gi].kind + ":" + groups[gi].variant, Size = groups[gi].size, Group = gi });
            Shuffle(lights, rng);
            var place = ConstelacionLayout.Place(lights.Count, w, h, rng);
            for (int i = 0; i < lights.Count; i++)
            {
                lights[i].Id = i;
                lights[i].Pos = place.Points[i];
                lights[i].AppearAtMs = startMs + 120f + i * ConstelacionContract.AppearStepMs;
            }
            return new ConstelacionSky(stage, lights.ToArray(), place, streak, bestStreak, closeScale);
        }

        /// <summary>Un cielo con las luces que se le den (con su tipo, variante, grupo, tamaño y posición): para las pruebas y para armar a mano un cielo concreto.</summary>
        public static ConstelacionSky FromLights(ConStage stage, IList<ConLight> lights, float w, float h, int streak = 0, int bestStreak = 0, float closeScale = 1f)
        {
            var arr = lights.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i].Id = i;
                if (string.IsNullOrEmpty(arr[i].Key)) arr[i].Key = arr[i].Kind + ":" + arr[i].Variant;
            }
            var place = new ConPlacement { Points = arr.Select(l => l.Pos).ToArray(), R = ConstelacionLayout.RadiusFor(arr.Length, w, h), Width = w, Height = h };
            return new ConstelacionSky(stage, arr, place, streak, bestStreak, closeScale);
        }

        private static List<T> Shuffle<T>(List<T> a, Random rng)
        {
            for (int i = a.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T t = a[i]; a[i] = a[j]; a[j] = t;
            }
            return a;
        }

        /// <summary>Vuelve a poner las luces con otra hora de aparición (cuando el cielo ya está armado pero todavía no se muestra: la tarjeta «NUEVO» o la cuenta regresiva).</summary>
        public void RestartAppearance(float startMs)
        {
            for (int i = 0; i < Lights.Length; i++) Lights[i].AppearAtMs = startMs + 120f + i * ConstelacionContract.AppearStepMs;
        }

        // ------------------------------------------------------------------ lo que falta

        public int PairsLeft => Lights.Count(l => l.State != ConState.Done && l.Size == 2) / 2;
        public int TriosLeft => Lights.Count(l => l.State != ConState.Done && l.Size == 3) / 3;

        // ------------------------------------------------------------------ un toque

        /// <summary>true si la luz se puede tocar ahora: está dormida y ya terminó de aparecer lo suficiente.</summary>
        public bool CanTap(int id, float nowMs) =>
            id >= 0 && id < Lights.Length && Lights[id].State == ConState.Down && nowMs >= Lights[id].AppearAtMs + ConstelacionContract.TapableAfterMs;

        /// <summary>Cierra las luces que quedaron abiertas sin coincidir (a los 850 ms o al tocar otra). Devuelve las que cerró.</summary>
        public List<ConLight> ClosePending()
        {
            var closed = new List<ConLight>();
            foreach (var c in Pending) if (c.State == ConState.Up) { c.State = ConState.Down; closed.Add(c); }
            Pending = new List<ConLight>();
            return closed;
        }

        /// <summary>El reloj corre: si las abiertas que no coincidían ya cumplieron su tiempo, se cierran solas.</summary>
        public List<ConLight> Tick(float nowMs) => Pending.Count > 0 && nowMs >= PendingCloseAtMs ? ClosePending() : new List<ConLight>();

        public ConTapResult Tap(int id, float nowMs)
        {
            var res = new ConTapResult();
            if (!CanTap(id, nowMs)) return res;
            res.Accepted = true;
            var card = Lights[id];
            res.ClosedNow = ClosePending();
            bool wasSeen = card.Seen, opp = false, hit = false;
            if (Face.Count > 0)
            {
                var first = Face[0];
                var kin = Lights.Where(k => k.Key == first.Key && k.State == ConState.Down && k.Seen).ToList();
                opp = kin.Count > 0;
                hit = kin.Contains(card);
                res.Kin = kin;
            }
            card.State = ConState.Up;
            card.Seen = true;
            res.Opened = card;
            if (Face.Count == 0)
            {
                Face.Add(card);
                Types.Clear();
                Turns++;
                res.FirstOfTurn = true;
                return res;
            }
            res.Opportunity = opp;
            res.Hit = hit;
            if (opp) Opps.Add(hit);
            else if (wasSeen) { Useless++; res.Useless = true; }     // una luz ya vista que no era compañera, sin nada que recordar: una vuelta de más
            if (card.Key == Face[0].Key)
            {
                res.Matched = true;
                Types.Add(hit ? ConLine.Memory : ConLine.New);
                Face.Add(card);
                if (hit)
                {
                    Hits++;
                    Streak++;
                    BestStreak = Math.Max(BestStreak, Streak);
                }
                if (Face.Count == card.Size) CompleteGroup(res, nowMs);
                else res.ThirdMissing = Face.Count == 2;
            }
            else
            {
                // no son iguales: quedan abiertas un momento (o hasta el próximo toque)
                if (opp)
                {
                    Misses++;
                    Streak = 0;
                    res.Miss = true;
                }
                else res.Kin = new List<ConLight>();
                var cards = new List<ConLight>(Face) { card };
                Face.Clear();
                Pending = cards;
                PendingCloseAtMs = nowMs + ConstelacionContract.CloseAfterMs * _closeScale;
                res.Pending = cards;
                res.PendingCloseAtMs = PendingCloseAtMs;
            }
            return res;
        }

        private void CompleteGroup(ConTapResult res, float nowMs)
        {
            var f = new List<ConLight>(Face);
            bool mem = Types.Contains(ConLine.Memory);
            foreach (var c in f)
            {
                c.State = ConState.Done;
                c.Ring = mem ? ConLine.Memory : ConLine.New;
            }
            var lightPoints = Lights.Select(l => l.Pos).ToList();
            for (int i = 1; i < f.Count; i++)
            {
                var geo = new ConLinkGeo { A = f[i - 1].Pos, B = f[i].Pos };
                var (cx, cy) = ConstelacionLayout.Route(geo.A, geo.B, Placement.R, Placement.Width, Placement.Height, lightPoints, Links.Select(l => l.Geo).ToList());
                geo.Cx = cx;
                geo.Cy = cy;
                ConstelacionLayout.Trim(geo, Placement.R);
                var link = new ConLink { A = f[i - 1], B = f[i], Type = Types[i - 1], Geo = geo, AtMs = nowMs + (i - 1) * 200f };
                Links.Add(link);
                res.NewLinks.Add(link);
            }
            GroupsFound++;
            if (mem) MemoryGroups++;
            res.Completed = f;
            res.CompletedByMemory = mem;
            Face.Clear();
            Types.Clear();
            res.BoardDone = Complete;
        }
    }
}
