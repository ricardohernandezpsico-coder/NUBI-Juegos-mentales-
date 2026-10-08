using System;
using System.Collections.Generic;

namespace NeuroVida.Games.Correo
{
    /// <summary>Qué señal trae una carta: ninguna, el sello dorado (focal: se mira el sello) o un lazo (no focal: está en la carta, no en el sello).</summary>
    public enum MailCue { None, Gold, Lazo }

    /// <summary>Una carta de la cinta: el planeta de su sello (su buzón), su señal y si vino en un saco.</summary>
    public sealed class MailLetter
    {
        public int Planet;
        public MailCue Cue;
        public bool Rush;
        public bool IsCue => Cue != MailCue.None;
    }

    public enum MailState { Open, Hit, Miss, Commission }

    /// <summary>Un encargo del día. Por evento (<see cref="Cue"/> dorado o lazo → caja fuerte) o por hora (<see cref="IsTime"/>: encender el faro en la ventana de <see cref="Moment"/>). Se dan en la mañana y no se ven durante el día.</summary>
    public sealed class MailTodo
    {
        public bool IsTime;
        public MailCue Cue;
        public MailMoment Moment;
        public bool Routine, Cancelled;
        public MailState State;
        public int Hits, Misses, Early;
        public float W0, W1;
    }

    /// <summary>Un saco que cae en el día: a esa fracción del día llegan 5 cartas seguidas, una cada 0,85 s.</summary>
    public sealed class MailRush
    {
        public float At;
        public int Left = MailContract.RushLetters;
        public float T;
    }

    public enum MailEventKind { Spawned, Fell, RushStarted, RadioCancelled, WindowClosed, DayOver }

    /// <summary>Lo que pasó solo durante el día (sin un toque): la pantalla lo muestra y lo suena.</summary>
    public struct MailEvent
    {
        public MailEventKind Kind;
        public MailLetter Letter;
        public MailTodo Todo;
        /// <summary>En <see cref="MailEventKind.Fell"/>: la carta que cayó traía una señal (era con sello dorado o con lazo).</summary>
        public bool WasCue;
    }

    public enum MailTap { Ignored, BoxRight, BoxWrong, SafeCue, SafeWrongLetter, SafeNothing, BeaconHit, BeaconCommission, BeaconEarly, BeaconNotNow, Peek, PeekCooldown }

    /// <summary>Lo que pasó con un toque (la pantalla muestra y suena según esto).</summary>
    public struct MailTapResult
    {
        public MailTap Kind;
        public MailLetter Letter;
        public int Box;
        public MailTodo Todo;
        /// <summary>Escalón de la racha que se acaba de alcanzar (1 = ×10, 2 = ×20, 3 = ×30 o más); 0 = ninguno.</summary>
        public int ComboTier;
        public int Combo;
        /// <summary>La carta con señal se fue a un buzón (no a la caja fuerte): era un encargo perdido.</summary>
        public bool CueMissed;
        public float Fraction;
    }

    /// <summary>Una línea del resumen del día: el encargo con su marca (✓ o raya) y su detalle.</summary>
    public sealed class MailRecapItem
    {
        public MailTodo Todo;
        public bool Ok;
        public string Text, Detail;
    }

    /// <summary>Las cifras del día (se suman en <see cref="MailTally"/>).</summary>
    public sealed class MailDayStat
    {
        public int PmOk, PmAll, Right, Sorted, Late, BestCombo;
        public float Accuracy;
        public int Events, EventsOk, TimesAll, TimesOk, Cancels, Commissions, Early, Peeks, GoodPeeks, SafeFalse;
        /// <summary>Las cartas señal que se escaparon, por tipo (para que el consejo nombre la señal que de verdad se perdió: docs §12).</summary>
        public int GoldMissed, LazoMissed;
        public bool Perfect;
    }

    public sealed class MailRecap
    {
        public readonly List<MailRecapItem> Items = new List<MailRecapItem>();
        public MailDayStat Stat = new MailDayStat();
    }

    /// <summary>
    /// Un día de la estación de correo (docs/diseno-correo-estacion.md; copiado del boceto aprobado: <c>planDay</c>, <c>beginDay</c>, <c>step</c>, <c>spawn</c>, <c>tapBox</c>, <c>tapSafe</c>, <c>tapBeacon</c>,
    /// <c>tapClock</c> y <c>endDay</c>). Puro y con pruebas: el tiempo avanza con <see cref="Step"/> (segundos de juego, o sea con la pausa) y el azar viene de un <see cref="Random"/> que se le da.
    /// </summary>
    public sealed class MailDay
    {
        public readonly MailStage Stage;
        public readonly int Level, DayNo;
        public readonly float Seconds;
        public readonly List<MailTodo> Todos = new List<MailTodo>();
        public MailTodo CancelTodo;
        public float CancelAt;
        public bool CancelFired;
        public readonly List<MailLetter> Plan = new List<MailLetter>();
        public int NextIndex;
        public readonly List<MailLetter> Belt = new List<MailLetter>();
        public readonly List<MailRush> RushPlan = new List<MailRush>();
        public MailRush Rush;
        public readonly List<float> Peeks = new List<float>();
        public readonly List<MailEvent> Events = new List<MailEvent>();

        public float DayT, SpawnT;
        public int Sorted, Right, WrongBox, Late, Combo, BestCombo, CueHits, CueMiss, SafeFalse, Grace = MailContract.BeltCapacity;
        public bool Ended;
        public float PeekAt = -100f;
        private readonly Random _rng;

        private MailDay(MailStage stage, int level, int dayNo, float seconds, Random rng)
        {
            Stage = stage;
            Level = level;
            DayNo = dayNo;
            Seconds = seconds;
            _rng = rng;
        }

        public float Fraction => Math.Min(1f, DayT / Seconds);

        // ------------------------------------------------------------------ armar el día

        /// <summary>Arma el día: los encargos (el de todos los días se fija el primer día y se repite aunque cambie la etapa), el aviso de la radio, el plan de cartas y los sacos. <paramref name="routine"/> es el momento del encargo
        /// de todos los días (null = aún no hay).</summary>
        public static MailDay Create(int level, int dayNo, ref MailMoment? routine, Random rng, float seconds = MailContract.DaySeconds)
        {
            var st = MailContract.Stage(level);
            var d = new MailDay(st, level, dayNo, seconds, rng);
            if (st.Gold > 0) d.Todos.Add(new MailTodo { Cue = MailCue.Gold });
            if (st.Lazo > 0) d.Todos.Add(new MailTodo { Cue = MailCue.Lazo });
            var times = new List<MailMoment>(st.Times);
            // lo de todos los días: el primer encargo de hora se fija el día 1 y se repite
            if (st.Routine && times.Count > 0)
            {
                if (!routine.HasValue) routine = times[0];
                else if (!times.Contains(routine.Value)) times[0] = routine.Value;      // sigue igual aunque cambie la etapa
            }
            foreach (var m in times)
            {
                float c = MailContract.MomentCenter(m), w = st.Win / 2f;
                d.Todos.Add(new MailTodo { IsTime = true, Moment = m, Routine = st.Routine && routine.HasValue && routine.Value == m, W0 = c - w, W1 = c + w });
            }
            // un encargo cancelado: uno de hora que NO sea el de todos los días (si no hay, el de hora); desde el día 2 y con probabilidad 0,6 (primero hay que haberlo practicado)
            if (st.Cancel && dayNo >= 2 && rng.NextDouble() < 0.6)
            {
                MailTodo pick = null, first = null;
                foreach (var t in d.Todos)
                {
                    if (!t.IsTime) continue;
                    if (first == null) first = t;
                    if (pick == null && !t.Routine) pick = t;
                }
                pick = pick ?? first;
                if (pick != null) { d.CancelTodo = pick; d.CancelAt = Math.Max(0.12f, MailContract.MomentCenter(pick.Moment) - 0.3f); }
            }
            d.PlanSacks();
            d.PlanLetters();
            return d;
        }

        /// <summary>Los sacos del día (cambian el ritmo: calma, apuro, calma): 1 (2 desde la etapa 5), separados por al menos el 20 % del día; en las etapas 1-5 no caen cerca de la hora de un encargo.</summary>
        private void PlanSacks()
        {
            int nR = MailContract.RushCount(Level);
            for (int tries = 0; RushPlan.Count < nR && tries < 200; tries++)
            {
                float f = 0.16f + (float)_rng.NextDouble() * 0.66f;
                bool close = false;
                foreach (var r in RushPlan) if (Math.Abs(r.At - f) < 0.2f) close = true;
                if (close) continue;
                if (Level <= 5)
                {
                    bool nearHour = false;
                    foreach (var t in Todos) if (t.IsTime && f > t.W0 - 0.08f && f < t.W1 + 0.04f) nearHour = true;
                    if (nearHour) continue;
                }
                RushPlan.Add(new MailRush { At = f });
            }
            RushPlan.Sort((a, b) => a.At.CompareTo(b.At));
        }

        /// <summary>Las cartas del día: planetas al azar y las cartas señal repartidas, nunca en los primeros segundos ni dos seguidas (y, para que ninguna se quede sin llegar, no tan tarde como para no alcanzar a
        /// tocarla aunque caiga un saco).</summary>
        private void PlanLetters()
        {
            var st = Stage;
            int n = (int)Math.Ceiling(Seconds / st.Every) + 2;
            for (int i = 0; i < n; i++) Plan.Add(new MailLetter { Planet = _rng.Next(st.Boxes) });
            int rushDelay = (int)Math.Ceiling(RushPlan.Count * (MailContract.RushLetters * MailContract.RushEvery));
            int lastIndex = Math.Min(n - 4, (int)((Seconds - rushDelay - 6f) / st.Every));
            if (lastIndex < 3) lastIndex = Math.Min(n - 4, 3);
            Put(MailCue.Gold, st.Gold, lastIndex);
            Put(MailCue.Lazo, st.Lazo, lastIndex);
        }

        private void Put(MailCue cue, int count, int lastIndex)
        {
            int span = lastIndex - 3 + 1;
            for (int tries = 0; count > 0 && span > 0 && tries < 200; tries++)
            {
                int i = 3 + _rng.Next(span);
                if (Plan[i].IsCue || Plan[i - 1].IsCue || Plan[i + 1].IsCue) continue;
                Plan[i].Cue = cue;
                count--;
            }
        }

        /// <summary>Empieza el día: sale la primera carta.</summary>
        public void Begin() => Spawn(null);

        /// <summary>Adelanta el reloj del día hasta esa fracción (solo el tutorial: salta a justo antes de la hora del encargo). No cierra ventanas ni dispara avisos: los pasos siguientes lo hacen.</summary>
        public void JumpTo(float fraction)
        {
            DayT = Math.Max(DayT, Math.Min(Seconds * 0.999f, fraction * Seconds));
            SpawnT = 0f;
        }

        // ------------------------------------------------------------------ el paso del tiempo

        /// <summary>Un paso de <paramref name="dt"/> segundos de juego: llegan las cartas (y los sacos), la radio cancela el encargo, se cierran las ventanas de hora sin avisar y, a los 50 s, termina el día.</summary>
        public void Step(float dt)
        {
            if (Ended) return;
            DayT += dt;
            SpawnT += dt;
            float f = Fraction;
            if (Rush == null && RushPlan.Count > 0 && f >= RushPlan[0].At)
            {
                Rush = RushPlan[0];
                RushPlan.RemoveAt(0);
                Rush.T = MailContract.RushStartDelay;
                Events.Add(new MailEvent { Kind = MailEventKind.RushStarted });
            }
            if (Rush != null)
            {
                Rush.T += dt;
                if (Rush.T >= MailContract.RushEvery)
                {
                    Rush.T -= MailContract.RushEvery;
                    Spawn(new MailLetter { Planet = _rng.Next(Stage.Boxes), Rush = true });
                    if (--Rush.Left <= 0)
                    {
                        Rush = null;
                        SpawnT = 0f;
                        Grace = Belt.Count + 1;
                    }
                }
            }
            else if (SpawnT >= Stage.Every)
            {
                SpawnT -= Stage.Every;
                Spawn(null);
            }
            if (CancelTodo != null && !CancelFired && f >= CancelAt)
            {
                CancelFired = true;
                CancelTodo.Cancelled = true;
                Events.Add(new MailEvent { Kind = MailEventKind.RadioCancelled, Todo = CancelTodo });
            }
            foreach (var t in Todos)
            {
                if (t.IsTime && t.State == MailState.Open && !t.Cancelled && f > t.W1)
                {
                    t.State = MailState.Miss;
                    Events.Add(new MailEvent { Kind = MailEventKind.WindowClosed, Todo = t });
                }
            }
            if (DayT >= Seconds && !Ended)
            {
                Ended = true;
                Events.Add(new MailEvent { Kind = MailEventKind.DayOver });
            }
        }

        private void Spawn(MailLetter extra)
        {
            var l = extra;
            if (l == null)
            {
                if (NextIndex >= Plan.Count) return;
                l = Plan[NextIndex++];
            }
            Belt.Add(l);
            Events.Add(new MailEvent { Kind = MailEventKind.Spawned, Letter = l });
            // la cinta se llenó: la más antigua cae a «atrasadas» (después de un saco, la fila se va vaciando sin castigo)
            if (Belt.Count > (Rush != null ? MailContract.RushBeltCapacity : Math.Max(MailContract.BeltCapacity, Grace)))
            {
                var old = Belt[0];
                Belt.RemoveAt(0);
                Late++;
                Combo = 0;
                if (old.IsCue) { CueMiss++; NoteCueMiss(old); }
                Events.Add(new MailEvent { Kind = MailEventKind.Fell, Letter = old, WasCue = old.IsCue });
            }
        }

        private MailTodo CueTodo(MailCue cue)
        {
            foreach (var t in Todos) if (!t.IsTime && t.Cue == cue) return t;
            return null;
        }

        private void NoteCueMiss(MailLetter l)
        {
            var t = CueTodo(l.Cue);
            if (t != null) t.Misses++;
        }

        private void ShrinkGrace()
        {
            if (Grace > 0) Grace = Math.Max(MailContract.BeltCapacity, Math.Min(Grace, Belt.Count + 1));
        }

        // ------------------------------------------------------------------ los toques

        /// <summary>Un toque en el buzón <paramref name="box"/>. <paramref name="frontReady"/>: la carta de adelante ya llegó a su lugar (si no, el toque no hace nada).</summary>
        public MailTapResult TapBox(int box, bool frontReady)
        {
            var res = new MailTapResult { Kind = MailTap.Ignored, Box = box };
            if (Belt.Count == 0 || !frontReady) return res;
            var l = Belt[0];
            Belt.RemoveAt(0);
            Sorted++;
            ShrinkGrace();
            res.Letter = l;
            if (l.IsCue) { CueMiss++; NoteCueMiss(l); res.CueMissed = true; }
            if (l.Planet == box)
            {
                Right++;
                Combo++;
                BestCombo = Math.Max(BestCombo, Combo);
                res.Kind = MailTap.BoxRight;
                if (Combo % MailContract.ComboStep == 0) res.ComboTier = Math.Min(3, Combo / MailContract.ComboStep);
            }
            else
            {
                WrongBox++;
                Combo = 0;
                res.Kind = MailTap.BoxWrong;
            }
            res.Combo = Combo;
            return res;
        }

        /// <summary>Un toque en la caja fuerte: la carta de adelante se guarda si trae una señal (¡encargo cumplido!); una carta normal vuelve («Esa carta no va a la caja fuerte»).</summary>
        public MailTapResult TapSafe(bool frontReady)
        {
            var res = new MailTapResult { Kind = MailTap.SafeNothing };
            if (Belt.Count == 0 || !frontReady) return res;
            var l = Belt[0];
            Belt.RemoveAt(0);
            ShrinkGrace();
            res.Letter = l;
            if (l.IsCue)
            {
                CueHits++;
                var t = CueTodo(l.Cue);
                if (t != null) t.Hits++;
                res.Kind = MailTap.SafeCue;
                res.Todo = t;
            }
            else
            {
                SafeFalse++;
                Combo = 0;
                Belt.Insert(0, l);
                res.Kind = MailTap.SafeWrongLetter;
            }
            res.Combo = Combo;
            return res;
        }

        /// <summary>Un toque en el faro: a tiempo (dentro de la ventana de un encargo) lo cumple; antes de hora o fuera de ventana no hace nada; si el encargo estaba cancelado, es un error de comisión.</summary>
        public MailTapResult TapBeacon()
        {
            float f = Fraction;
            var res = new MailTapResult { Fraction = f };
            MailTodo inWin = null, cancelled = null, soon = null;
            foreach (var t in Todos)
            {
                if (!t.IsTime) continue;
                if (t.Cancelled && f >= t.W0 - 0.04f && f <= t.W1 + 0.04f && cancelled == null) cancelled = t;
                if (t.State != MailState.Open) continue;
                if (!t.Cancelled && f >= t.W0 && f <= t.W1 && inWin == null) inWin = t;
                if (!t.Cancelled && f < t.W0 && soon == null) soon = t;
            }
            if (inWin != null)
            {
                inWin.State = MailState.Hit;
                res.Kind = MailTap.BeaconHit;
                res.Todo = inWin;
                return res;
            }
            if (cancelled != null)
            {
                cancelled.State = MailState.Commission;
                res.Kind = MailTap.BeaconCommission;
                res.Todo = cancelled;
                return res;
            }
            if (soon != null)
            {
                soon.Early++;
                res.Kind = MailTap.BeaconEarly;
                res.Todo = soon;
                return res;
            }
            res.Kind = MailTap.BeaconNotNow;
            return res;
        }

        /// <summary>Un toque en el reloj tapado: se destapa 1,6 s (no se puede volver a tocar antes) y anota a qué hora del día se miró.</summary>
        public MailTapResult TapClock()
        {
            var res = new MailTapResult { Fraction = Fraction };
            if (DayT - PeekAt < MailContract.PeekSeconds) { res.Kind = MailTap.PeekCooldown; return res; }
            PeekAt = DayT;
            Peeks.Add(Fraction);
            res.Kind = MailTap.Peek;
            return res;
        }

        // ------------------------------------------------------------------ el resumen del día

        /// <summary>Lo que se ve al terminar el día: cada encargo con su marca, las cartas, las miradas al reloj. Un encargo por evento cuya carta nunca alcanzó a llegar a la cinta (o quedó sin tocar al terminar) no tuvo
        /// oportunidad y no se cuenta.</summary>
        public MailRecap Summarize()
        {
            var r = new MailRecap();
            var s = r.Stat;
            var timeTodos = new List<MailTodo>();
            foreach (var t in Todos)
            {
                if (!t.IsTime)
                {
                    int all = t.Hits + t.Misses;
                    s.Events += all;
                    s.EventsOk += t.Hits;
                    if (t.Cue == MailCue.Gold) s.GoldMissed += t.Misses; else s.LazoMissed += t.Misses;
                    if (all == 0) continue;
                    r.Items.Add(new MailRecapItem { Todo = t, Ok = t.Hits == all, Text = t.Cue == MailCue.Gold ? MailContract.ItemGold : MailContract.ItemLazo, Detail = MailContract.DetCount(t.Hits, all) });
                    continue;
                }
                timeTodos.Add(t);
                s.Early += t.Early;
                if (t.Cancelled)
                {
                    s.Cancels++;
                    bool commission = t.State == MailState.Commission;
                    if (commission) s.Commissions++;
                    r.Items.Add(new MailRecapItem { Todo = t, Ok = !commission, Text = MailContract.ItemCancelled(t.Moment), Detail = commission ? MailContract.DetCommission : MailContract.DetNoCommission });
                }
                else
                {
                    s.TimesAll++;
                    if (t.State == MailState.Hit) s.TimesOk++;
                    r.Items.Add(new MailRecapItem { Todo = t, Ok = t.State == MailState.Hit, Text = MailContract.ItemTime(t.Moment, t.Routine), Detail = t.State == MailState.Hit ? MailContract.DetHit : MailContract.DetMiss });
                }
            }
            foreach (var it in r.Items) if (it.Ok) s.PmOk++;
            s.PmAll = r.Items.Count;
            s.Accuracy = Sorted > 0 ? (float)Right / Sorted : 1f;
            s.Right = Right; s.Sorted = Sorted; s.Late = Late; s.BestCombo = BestCombo; s.SafeFalse = SafeFalse;
            s.Peeks = Peeks.Count;
            foreach (float p in Peeks)
                foreach (var t in timeTodos)
                    if (p >= t.W0 - MailContract.NearHourBefore && p <= t.W1) { s.GoodPeeks++; break; }
            s.Perfect = s.PmAll > 0 && s.PmOk == s.PmAll;
            return r;
        }
    }

    /// <summary>La partida: 4 días seguidos. Lleva la etapa (sube o baja al terminar cada día), el encargo de todos los días y las cifras de cada día.</summary>
    public sealed class MailRun
    {
        public int Level, MaxLevelReached, DayNo, Floor = 1, Ceiling = MailContract.MaxLevel;
        public MailMoment? Routine;
        public readonly MailTally Tally = new MailTally();
        public readonly List<MailDayStat> History = new List<MailDayStat>();
        public int BestDayRecord;
        public bool NewRecordToday, Up;
        private readonly Random _rng;

        public MailRun(int startLevel, int bestRecord, Random rng)
        {
            Level = Math.Max(1, Math.Min(MailContract.MaxLevel, startLevel));
            MaxLevelReached = Level;
            BestDayRecord = Math.Max(0, bestRecord);
            _rng = rng;
        }

        public bool Finished => DayNo >= MailContract.Days;

        /// <summary>El siguiente día de la partida (null si ya fueron los 4).</summary>
        public MailDay NextDay(float seconds = MailContract.DaySeconds)
        {
            if (Finished) return null;
            DayNo++;
            return MailDay.Create(Level, DayNo, ref Routine, _rng, seconds);
        }

        /// <summary>Cierra el día: suma sus cifras, sube o baja la etapa (docs §4) y anota el récord de cartas en un día perfecto.</summary>
        public void Complete(MailDay day, MailRecap recap)
        {
            var s = recap.Stat;
            History.Add(s);
            Tally.Add(s);
            Up = MailContract.DayWasUp(s.PmOk, s.PmAll, s.Accuracy);
            Level = MailContract.Advance(Level, s.PmOk, s.PmAll, s.Accuracy, Floor, Ceiling);
            MaxLevelReached = Math.Max(MaxLevelReached, Level);
            NewRecordToday = s.Perfect && s.Right > BestDayRecord;
            if (NewRecordToday) BestDayRecord = s.Right;
        }
    }
}
