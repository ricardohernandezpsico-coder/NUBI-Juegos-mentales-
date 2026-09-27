using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Bridge;
using NeuroVida.Contracts;
using NeuroVida.Games.Secuencia; // RoundedRectSprite / RadialGlowSprite / RingSprite
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;

namespace NeuroVida.Games.Contacto
{
    /// <summary>
    /// "Primer Contacto": juego estrella de LENGUAJE. Los nuri (extraterrestres amables) muestran cosas y las nombran en
    /// su idioma; nadie dice qué significa cada palabra: se deduce de escena en escena (ver <see cref="ContactContract"/>).
    /// <list type="bullet">
    /// <item><b>Repaso</b> (si hay palabras de otros días): el nuri dice una palabra que ya sabías y eliges la cosa, con
    /// respuesta al instante. La que se olvidó vuelve a la lección de hoy.</item>
    /// <item><b>Lección</b>: escenas con 2-5 cosas sobre plataformas de luz; el nuri dice una frase (voz sintetizada, la
    /// palabra que suena se marca en el globo) y se toca la cosa. No hay "bien/mal" en el momento: cuando una palabra se
    /// acierta dos veces seguidas (sin contar la primera vez que se oye) queda DESCIFRADA y vuela al diccionario de abajo.
    /// Tocar al nuri repite la frase.</item>
    /// <item><b>Conversación</b> (si ya se sabe todo el idioma): frases completas con respuesta al instante.</item>
    /// </list>
    /// </summary>
    public class ContactGameController : GameControllerBase
    {
        public const string GameId = ContactContract.GameId;

        private const float UnitsPerDp = 3f;
        private const float MarginU = 40f;
        private const float AlienSize = 250f, ThingSize = 210f, SlotSize = 104f;
        private const float RepeatAfter = 9f;

        private static readonly Color Deep = NeuroStyle.Hex(0x1B2466);

        private sealed class ThingView
        {
            public RectTransform Rect;
            public Image Glow, Base, Mark, Ring;
            public readonly Image[] Copies = new Image[3];
        }

        private sealed class SlotView
        {
            public RectTransform Rect;
            public Image Bg, Ring, Icon;
            public Text Mystery, Word, Note, Digit;
            /// <summary>Dos puntos sobre el hueco: cuántos aciertos seguidos lleva la palabra (2 = descifrada).</summary>
            public readonly Image[] Pips = new Image[2];
        }

        private System.Random _rng;
        private AdaptiveDifficulty _dda;
        private ContactLesson _lesson;
        private readonly List<int> _known = new List<int>();
        private readonly List<int> _faded = new List<int>();
        private readonly List<int> _reviewWords = new List<int>();
        private readonly List<int> _reviewOk = new List<int>();
        private int _meTotal, _meOk, _practiceOk, _practiceTotal, _previousFrameRate;
        private bool _done, _hintAnnounced;
        /// <summary>Lo último que se tocó para cada palabra de la lección ("tu idea", se ve tenue en el diccionario).</summary>
        private readonly Dictionary<int, Thing> _guess = new Dictionary<int, Thing>();
        /// <summary>Las cosas que había la última vez que sonó cada palabra (la pista "la vez anterior").</summary>
        private readonly Dictionary<int, Thing[]> _lastSeen = new Dictionary<int, Thing[]>();

        /// <summary>La pista "la vez anterior" acompaña los primeros niveles; desde el 5 hay que recordarla.</summary>
        private const int HintMaxLevel = 4;
        private bool HintOn => _dda.Level <= HintMaxLevel;

        // escena en curso
        private ContactScene _scene;
        private NuriVoice.Plan _plan;
        private bool _waitTap;
        private int _tapped = -1;
        private bool _speaking, _repeatAsked;
        private Coroutine _speech;

        // UI
        private RectTransform _safe, _play, _fx, _alien, _bubble, _thingsRoot, _dictRoot;
        private Image _alienImg, _alienGlow;
        private Text _bubbleText, _caption, _sub, _dictLabel, _hintLabel;
        private RectTransform _hintRoot;
        private readonly List<Image> _hintIcons = new List<Image>();
        private readonly List<Text> _hintCounts = new List<Text>();
        private float _hintY;
        private readonly List<ThingView> _things = new List<ThingView>();
        private readonly List<SlotView> _slots = new List<SlotView>();
        private readonly List<Vector2> _thingPos = new List<Vector2>();
        private float _alienY, _bubbleY, _thingsY, _dictY;
        private Toast _toast;
        private ExitButton _exit;
        private GameHud _hud;
        private CountdownScreen _countdown;

        // ------------------------------------------------------------------ sesión

        public void StartSession(SequenceInitConfig config)
        {
            _config = config;
            _rng = new System.Random();
            var c = config.config;
            var age = DdaUserProfileConfig.ParseAgeBand(c.age_band);
            float start = AdaptiveDifficulty.StartRating(c, ContactContract.MaxLevel);
            // Un registro por escena desde la segunda vez que suena la palabra: pocos por partida, pasos grandes.
            _dda = new AdaptiveDifficulty(ContactContract.MaxLevel, age, start, stepUp: 0.5f, useReaction: false);

            _known.Clear();
            if (c.contact_known != null) foreach (int w in c.contact_known) if (w >= 0 && w < ContactContract.WordCount && !_known.Contains(w)) _known.Add(w);
            _reviewWords.Clear();
            _reviewOk.Clear();
            _faded.Clear();
            if (c.contact_review != null)
                foreach (int w in c.contact_review)
                    if (_known.Contains(w) && !_reviewWords.Contains(w) && _reviewWords.Count < ContactContract.MaxReview) _reviewWords.Add(w);
            _meTotal = _meOk = _practiceOk = _practiceTotal = 0;
            _done = false;
            _waitTap = false;
            _lesson = null;
            _hintAnnounced = false;
            _guess.Clear();
            _lastSeen.Clear();

            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 60;

            _resultRoot.gameObject.SetActive(false);
            _exit.Hide();
            StopAllCoroutines();
            StartCoroutine(GameLoop());
        }

        private void OnDisable()
        {
            if (_previousFrameRate != 0) Application.targetFrameRate = _previousFrameRate;
        }

        private IEnumerator GameLoop()
        {
            _safe.gameObject.SetActive(false);
            yield return StartCoroutine(_countdown.Play("Primer Contacto", Assessment.Subtitle("Descifra el idioma nuri"), () => _safe.gameObject.SetActive(true)));
            _safe.gameObject.SetActive(true);
            yield return null;
            ApplySafeArea(_safe);
            Canvas.ForceUpdateCanvases();
            Layout();
            _hud.SetLevel(_dda.Level);
            _hud.SetInfo(_known.Count > 0 ? $"Diccionario {_known.Count}" : "¡Hola!");

            StartCoroutine(Prewarm());
            yield return StartCoroutine(Intro());
            if (_reviewWords.Count > 0) yield return StartCoroutine(Review());

            _lesson = ContactContract.BuildLesson(_known, _faded, _dda.Level);
            if (_lesson.Practice) yield return StartCoroutine(Conversation());
            else yield return StartCoroutine(Lesson());
            Finish();
        }

        // ------------------------------------------------------------------ fases

        /// <summary>
        /// Hornea los dibujos que pueden aparecer (uno por cuadro, durante la presentación): en el teléfono cada ícono de
        /// color tarda unas centésimas y, hecho al mostrar la escena, se notaría un tirón.
        /// </summary>
        private IEnumerator Prewarm()
        {
            var probe = ContactContract.BuildLesson(_known, null, _dda.Level);
            for (int o = 0; o < ContactContract.ObjectCount; o++)
            {
                ContactSprites.Object(new Thing(o));
                yield return null;
            }
            if (!probe.ColorOn) yield break;
            for (int o = 0; o < ContactContract.SkyObjects; o++)
            {
                if (!ContactContract.Colorable(o)) continue;
                for (int c = 0; c < ContactContract.ColorCount; c++)
                {
                    ContactSprites.Object(new Thing(o, c));
                    yield return null;
                }
            }
        }

        private IEnumerator Intro()
        {
            _alien.gameObject.SetActive(true);
            StartCoroutine(PopIn(_alien, 0.35f));
            Sfx(ContactSounds.Arrival(), 0.5f);
            bool first = _known.Count == 0;
            _toast.Show("Primer contacto", first ? "Aprende el idioma de los nuri" : "Los nuri vuelven a saludarte", NeuroStyle.Lime, 2.2f);
            SetCaption(first ? "Los nuri nombran cosas en su idioma" : "Recuerda: busca la cosa que se repite",
                first ? "Tú descubres qué significa cada palabra" : "");
            yield return StartCoroutine(Pause(2.5f));
        }

        /// <summary>Repaso de palabras de otros días, con respuesta al instante. Lo olvidado vuelve a la lección de hoy.</summary>
        private IEnumerator Review()
        {
            var known = ContactContract.BuildLesson(_known, null, _dda.Level);
            _dictRoot.gameObject.SetActive(false);
            _toast.Show("Repaso", _reviewWords.Count == 1 ? "Una palabra de otro día" : $"{_reviewWords.Count} palabras de otros días", NeuroStyle.Grape, 1.8f);
            yield return StartCoroutine(Pause(1.4f));
            int streak = 0;
            for (int i = 0; i < _reviewWords.Count; i++)
            {
                int w = _reviewWords[i];
                _hud.SetInfo($"Repaso {i + 1} de {_reviewWords.Count}");
                var scene = ContactContract.ReviewScene(known, w, _rng);
                ContactSounds.Prepare(scene.Phrase);
                SetCaption(DaysText(i), "Toca lo que nombró");
                yield return StartCoroutine(ShowScene(scene));
                yield return StartCoroutine(WaitTap());
                int tap = _tapped;
                bool ok = tap == scene.Target;
                _reviewOk.Add(ok ? 1 : 0);
                ShowMark(_things[tap], ok);
                if (ok)
                {
                    streak++;
                    GameFeel.Correct(streak);
                    StartCoroutine(UiFx.SparkBurst(_fx, LocalIn(_fx, _things[tap].Rect), NeuroStyle.Lime, 8, 110f, 22f));
                    SetCaption($"¡Lo recordaste! {ContactContract.Words[w]} es {ContactContract.Meanings[w]}", "");
                    yield return StartCoroutine(Pause(1.2f));
                }
                else
                {
                    streak = 0;
                    _faded.Add(w);
                    _known.Remove(w);
                    GameFeel.Wrong();
                    yield return StartCoroutine(Pause(0.35f));
                    ShowMark(_things[scene.Target], true);
                    StartCoroutine(PopRect(_things[scene.Target].Rect, 1.15f, 0.25f));
                    SetCaption($"{ContactContract.Words[w]} era {ContactContract.Meanings[w]}", "Vuelve a tu lista de hoy");
                    yield return StartCoroutine(Pause(1.8f));
                }
                yield return StartCoroutine(ClearScene());
            }
            int kept = 0;
            foreach (int k in _reviewOk) kept += k;
            SetCaption(kept == _reviewWords.Count ? "Tu nuri sigue ahí" : $"Recordaste {kept} de {_reviewWords.Count}", "");
            yield return StartCoroutine(Pause(1.2f));
        }

        private string DaysText(int reviewIndex)
        {
            var days = _config.config.contact_review_days;
            int d = days != null && reviewIndex < days.Length ? days[reviewIndex] : -1;
            if (d <= 0) return "Repaso";
            return d == 1 ? "Repaso · de ayer" : $"Repaso · de hace {d} días";
        }

        /// <summary>La lección: escenas hasta descifrar las palabras nuevas (o dejarlas para otro día).</summary>
        private IEnumerator Lesson()
        {
            BuildDictionary();
            int n = _lesson.Targets.Count;
            if (ContactContract.NeedsGuide(_lesson))
            {
                yield return StartCoroutine(Guide());
            }
            else
            {
                _toast.Show("Palabras nuevas", n == 1 ? "Una por descifrar" : $"{n} por descifrar", NeuroStyle.Sky, 1.8f);
                SetCaption("", "");
                yield return StartCoroutine(Pause(1.2f));
            }

            int focus = -1;
            int max = ContactContract.MaxScenes(n);
            for (int s = 0; s < max && !_lesson.Finished; s++)
            {
                focus = ContactContract.NextFocus(_lesson, focus, _rng);
                var scene = ContactContract.MakeScene(_lesson, focus, ContactContract.ObjectsPerScene(_dda.Level), _rng);
                ContactSounds.Prepare(scene.Phrase);
                bool chance = ContactContract.IsExclusionChance(_lesson, scene);
                int heardBefore = _lesson.Progress[focus].Hearings;
                UpdateProgressInfo();
                if (!HintOn && !_hintAnnounced && _lastSeen.Count > 0)
                {
                    _hintAnnounced = true;
                    _toast.Show("Sin pista", "Ahora recuerda tú la vez anterior", NeuroStyle.Grape, 2.2f);
                }
                SetCaption($"¿Qué es {ContactContract.PhraseText(scene.Phrase)}?", s < 3 ? "Toca al nuri para oírlo otra vez" : "");
                ShowHint(_lastSeen.TryGetValue(focus, out var prev) ? prev : null, HintOn);
                HighlightSlots(scene.Phrase, true);
                yield return StartCoroutine(ShowScene(scene));
                yield return StartCoroutine(WaitTap());
                int tap = _tapped;
                HighlightSlots(scene.Phrase, false);
                ShowHint(null, false);

                if (chance)
                {
                    _meTotal++;
                    if (ContactContract.Excluded(_lesson, scene, tap)) _meOk++;
                }
                if (heardBefore >= 1)
                {
                    _dda.Register(ContactContract.Matches(focus, scene.Things[tap]));
                    _hud.SetLevel(_dda.Level);
                }
                var parkedBefore = ParkedSet();
                var decoded = ContactContract.Answer(_lesson, scene, tap, s);
                Remember(scene, tap);
                if (decoded.Count > 0)
                {
                    foreach (int w in decoded) yield return StartCoroutine(Celebrate(w, scene));
                }
                else
                {
                    yield return StartCoroutine(Feedback(focus));
                }
                foreach (int w in _lesson.Targets)
                    if (_lesson.Progress[w].Parked && !parkedBefore.Contains(w)) ShowParked(w);
                yield return StartCoroutine(ClearScene());
            }
            // Lo que no salió queda para otro día (sin culpa).
            foreach (int w in _lesson.Targets) if (_lesson.Progress[w].Open) ShowParked(w, silent: true);
            UpdateProgressInfo();
            int d = _lesson.DecodedCount;
            if (d == n)
            {
                Sfx(ContactSounds.Decoded(), 0.5f);
                GameFeel.LevelUp();
                _toast.Show("¡Hablas más nuri!", n == 1 ? "Palabra descifrada" : $"{n} palabras descifradas", NeuroStyle.Sun, 2f);
            }
            SetCaption(d == n ? "¡Descifraste todas!" : d > 0 ? $"Descifraste {d} de {n}" : "Las palabras quedan para otro día",
                d < n ? "Las que faltan vuelven la próxima vez" : "");
            yield return StartCoroutine(Pause(2.2f));
        }

        /// <summary>
        /// Qué pasó con la palabra de la escena, dicho claro: la primera vez nadie lo sabe; desde la segunda, "¡Vas bien!"
        /// (un punto de dos) o "No era esa" (sin decir cuál era: eso se deduce).
        /// </summary>
        private IEnumerator Feedback(int focus)
        {
            var p = _lesson.Progress[focus];
            StartCoroutine(Nod());
            if (p.Hearings <= 1)
            {
                Sfx(ContactSounds.Ack(), 0.5f);
                SetCaption("Anotado", "La primera vez nadie lo sabe: fíjate en la próxima");
            }
            else if (p.Streak >= 1)
            {
                GameFeel.Correct(1);
                int slot = _lesson.Targets.IndexOf(focus);
                if (slot >= 0 && slot < _slots.Count) StartCoroutine(PopRect(_slots[slot].Rect, 1.25f, 0.22f));
                SetCaption("¡Vas bien!", "Una vez más y queda descifrada");
            }
            else
            {
                Sfx(ContactSounds.Ack(), 0.5f);
                SetCaption("No era esa", HintOn ? "Mira la pista: ¿qué cosa se repite?" : "Busca la cosa que se repite");
            }
            yield return StartCoroutine(Pause(1.1f));
        }

        /// <summary>Guarda lo que se tocó (tu idea) y lo que había (la pista de la próxima vez) de cada palabra de la frase.</summary>
        private void Remember(ContactScene scene, int tap)
        {
            foreach (int w in scene.Phrase)
            {
                if (!_lesson.Progress.ContainsKey(w)) continue;
                _lastSeen[w] = scene.Things;
                _guess[w] = scene.Things[tap];
                int slot = _lesson.Targets.IndexOf(w);
                if (slot >= 0 && slot < _slots.Count && !_lesson.Progress[w].Decoded) UpdateSlot(slot);
            }
        }

        /// <summary>
        /// Guía de la primera vez (solo si no hay diccionario): tres escenas con la primera palabra, explicando qué mirar.
        /// La palabra entra al diccionario pero no cuenta para las medidas (<see cref="ContactContract.MarkTaught"/>).
        /// </summary>
        private IEnumerator Guide()
        {
            int w = _lesson.Targets[0];
            string word = ContactContract.Words[w];
            var target = new Thing(ContactContract.ValueOf(w));
            var pool = new List<int>();
            for (int o = 0; o < ContactContract.ObjectCount; o++)
                if (o != target.Obj && !_lesson.Targets.Contains(ContactContract.NounWord(o))) pool.Add(o);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            var s1 = GuideScene(target, 1, pool[0]);
            var s2 = GuideScene(target, 0, pool[1], pool[2]);
            var s3 = GuideScene(target, 2, pool[3], pool[4]);
            foreach (var sc in new[] { s1, s2, s3 }) sc.Focus = w;
            ContactSounds.Prepare(s1.Phrase);

            _toast.Show("Cómo se juega", "Te muestro con una palabra", NeuroStyle.Sky, 2f);
            yield return StartCoroutine(Pause(1.2f));

            // 1) La primera vez: adivinar.
            SetCaption($"El nuri dijo {word}. ¿Qué será?", "La primera vez nadie lo sabe: elige una");
            ShowHint(null, false);
            yield return StartCoroutine(ShowScene(s1));
            yield return StartCoroutine(WaitTap());
            _guess[w] = s1.Things[_tapped];
            _lastSeen[w] = s1.Things;
            UpdateSlot(0);
            Sfx(ContactSounds.Ack(), 0.5f);
            SetCaption("Anotado", "Ahora mira qué pasa la próxima vez");
            yield return StartCoroutine(Pause(1.6f));
            yield return StartCoroutine(ClearScene());

            // 2) La segunda: lo que se repite.
            SetCaption($"Otra vez {word}. ¿Qué cosa ya estaba antes?", "Arriba ves lo que había la vez anterior");
            ShowHint(s1.Things, true);
            yield return StartCoroutine(ShowScene(s2));
            yield return StartCoroutine(GuideTap(s2));
            _guess[w] = target;
            _lastSeen[w] = s2.Things;
            UpdateSlot(0);
            SetPips(_slots[0], 1);
            GameFeel.Correct(1);
            ShowMark(_things[s2.Target], true);
            SetCaption($"¡Eso! Estaba las dos veces: {word} debe ser eso", "Si aciertas dos veces seguidas, queda descifrada");
            yield return StartCoroutine(Pause(2.4f));
            ShowHint(null, false);
            yield return StartCoroutine(ClearScene());

            // 3) Otra vez: descifrada.
            SetCaption($"¿Qué es {word}?", "Una vez más");
            ShowHint(s2.Things, true);
            yield return StartCoroutine(ShowScene(s3));
            yield return StartCoroutine(GuideTap(s3));
            ShowHint(null, false);
            ContactContract.MarkTaught(_lesson, w);
            yield return StartCoroutine(Celebrate(w, s3));
            yield return StartCoroutine(ClearScene());
            _toast.Show("¡Así se juega!", "Dos aciertos seguidos = descifrada", NeuroStyle.Lime, 2.4f);
            SetCaption("Ahora, las demás palabras", "Nadie te dirá qué significan: dedúcelo");
            yield return StartCoroutine(Pause(2.6f));
        }

        private ContactScene GuideScene(Thing target, int targetIndex, params int[] others)
        {
            var things = new List<Thing>();
            foreach (int o in others) things.Add(new Thing(o));
            targetIndex = Mathf.Clamp(targetIndex, 0, things.Count);
            things.Insert(targetIndex, target);
            return new ContactScene { Things = things.ToArray(), Target = targetIndex, Phrase = new[] { ContactContract.NounWord(target.Obj) } };
        }

        /// <summary>En la guía se espera la cosa correcta; si se toca otra, se muestra cuál se repite (brilla) y se vuelve a tocar.</summary>
        private IEnumerator GuideTap(ContactScene scene)
        {
            while (true)
            {
                yield return StartCoroutine(WaitTap());
                if (_tapped == scene.Target) yield break;
                Sfx(ContactSounds.Ack(), 0.5f);
                for (int i = 0; i < scene.Things.Length; i++) Undim(_things[i]);
                _things[_tapped].Ring.gameObject.SetActive(false);
                var t = _things[scene.Target];
                t.Ring.gameObject.SetActive(true);
                t.Ring.color = NeuroStyle.Lime;
                StartCoroutine(PopRect(t.Rect, 1.2f, 0.3f));
                SetCaption("Mira la que brilla: estaba la vez anterior", "Tócala");
                _tapped = -1;
            }
        }

        private HashSet<int> ParkedSet()
        {
            var set = new HashSet<int>();
            foreach (int w in _lesson.Targets) if (_lesson.Progress[w].Parked) set.Add(w);
            return set;
        }

        /// <summary>Conversación: ya se sabe todo el idioma; frases completas con respuesta al instante.</summary>
        private IEnumerator Conversation()
        {
            _dictRoot.gameObject.SetActive(false);
            _toast.Show("Conversación", "Ya sabes todo el nuri: a charlar", NeuroStyle.Lime, 2f);
            yield return StartCoroutine(Pause(1.4f));
            int streak = 0;
            for (int s = 0; s < ContactContract.PracticeScenes; s++)
            {
                int focus = _known[_rng.Next(_known.Count)];
                var scene = ContactContract.MakeScene(_lesson, focus, ContactContract.ObjectsPerScene(_dda.Level), _rng);
                ContactSounds.Prepare(scene.Phrase);
                _hud.SetInfo($"Frase {s + 1} de {ContactContract.PracticeScenes}");
                SetCaption("", "");
                yield return StartCoroutine(ShowScene(scene));
                yield return StartCoroutine(WaitTap());
                int tap = _tapped;
                bool ok = tap == scene.Target;
                _practiceTotal++;
                _dda.Register(ok);
                _hud.SetLevel(_dda.Level);
                ShowMark(_things[tap], ok);
                if (ok)
                {
                    _practiceOk++;
                    streak++;
                    GameFeel.Correct(streak);
                    yield return StartCoroutine(Pause(0.6f));
                }
                else
                {
                    streak = 0;
                    GameFeel.Wrong();
                    yield return StartCoroutine(Pause(0.3f));
                    ShowMark(_things[scene.Target], true);
                    SetCaption(ContactContract.PhraseText(scene.Phrase), Translate(scene.Phrase));
                    yield return StartCoroutine(Pause(1.6f));
                }
                yield return StartCoroutine(ClearScene());
            }
        }

        // ------------------------------------------------------------------ escena

        private IEnumerator ShowScene(ContactScene scene)
        {
            _scene = scene;
            int n = scene.Things.Length;
            PlaceThings(n);
            for (int i = 0; i < _things.Count; i++)
            {
                var v = _things[i];
                bool on = i < n;
                v.Rect.gameObject.SetActive(on);
                if (!on) continue;
                SetThing(v, scene.Things[i]);
                v.Rect.anchoredPosition = _thingPos[i];
                v.Mark.gameObject.SetActive(false);
                v.Ring.gameObject.SetActive(false);
                StartCoroutine(Materialize(v, 0.06f * i));
            }
            Sfx(ContactSounds.Materialize(), 0.35f);
            yield return StartCoroutine(Pause(0.3f + 0.06f * n));
            // Se puede tocar apenas el nuri empieza a hablar (no hay que esperar a que termine).
            _tapped = -1;
            _waitTap = true;
            _speech = StartCoroutine(Speak(scene.Phrase));
            while (_speaking && _tapped < 0) yield return null;
        }

        private void StopSpeaking()
        {
            if (_speech != null) StopCoroutine(_speech);
            _speech = null;
            _speaking = false;
            _alienImg.sprite = ContactSprites.Alien(false);
            _alien.localScale = Vector3.one;
        }

        private IEnumerator Materialize(ThingView v, float delay)
        {
            v.Rect.localScale = Vector3.zero;
            yield return StartCoroutine(Pause(delay));
            StartCoroutine(UiFx.RingBurst(_fx, LocalIn(_fx, v.Rect), NeuroStyle.Sky, 80f, 240f, 0.4f));
            yield return StartCoroutine(PopIn(v.Rect, 0.3f));
        }

        private IEnumerator ClearScene()
        {
            float t = 0f;
            const float seconds = 0.28f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                foreach (var v in _things)
                    if (v.Rect.gameObject.activeSelf) v.Rect.localScale = new Vector3(1f - 0.6f * k, 1f - k, 1f);
                yield return null;
            }
            foreach (var v in _things) v.Rect.gameObject.SetActive(false);
            _bubble.gameObject.SetActive(false);
            _scene = null;
        }

        /// <summary>El nuri dice la frase: voz, boca que se mueve y la palabra que suena marcada en el globo.</summary>
        private IEnumerator Speak(IReadOnlyList<int> phrase)
        {
            _speaking = true;
            float wait = 0f;
            while (!ContactSounds.Ready(phrase) && wait < 1.2f)
            {
                wait += GameClock.DeltaTime;   // la voz se prepara en otro hilo (unas décimas en el teléfono)
                yield return null;
            }
            _plan = ContactSounds.PlanFor(phrase);
            ShowBubble(phrase, -1);
            Sfx(ContactSounds.Voice(phrase), 0.9f);
            float t = 0f;
            int shownWord = -2;
            while (t < _plan.Duration - 0.3f)
            {
                t += GameClock.DeltaTime;
                _alienImg.sprite = ContactSprites.Alien(_plan.MouthOpen(t));
                int w = _plan.WordAt(t);
                if (w != shownWord)
                {
                    ShowBubble(phrase, w);
                    shownWord = w;
                }
                float bob = _plan.MouthOpen(t) ? 1.04f : 1f;
                _alien.localScale = Vector3.Lerp(_alien.localScale, new Vector3(1f / bob, bob, 1f), 0.4f);
                yield return null;
            }
            _alienImg.sprite = ContactSprites.Alien(false);
            _alien.localScale = Vector3.one;
            ShowBubble(phrase, -1);
            _speaking = false;
            _speech = null;
        }

        /// <summary>Espera el toque (ya habilitado en <see cref="ShowScene"/>). Si pasa un rato, o se toca al nuri, repite.</summary>
        private IEnumerator WaitTap()
        {
            _repeatAsked = false;
            _waitTap = true;
            float idle = 0f;
            while (_tapped < 0)
            {
                if (!_speaking) idle += GameClock.DeltaTime;
                if ((_repeatAsked || idle > RepeatAfter) && !_speaking && _scene != null)
                {
                    _repeatAsked = false;
                    idle = 0f;
                    _speech = StartCoroutine(Speak(_scene.Phrase));
                }
                yield return null;
            }
            _waitTap = false;
            StopSpeaking();
            var v = _things[_tapped];
            Sfx(ContactSounds.Pick(), 0.5f);
            v.Ring.gameObject.SetActive(true);
            v.Ring.color = NeuroStyle.Sun;
            StartCoroutine(PopRect(v.Rect, 1.12f, 0.18f));
            // Las demás se apagan un poco: se ve cuál elegiste.
            for (int i = 0; i < _things.Count; i++)
                if (i != _tapped && _things[i].Rect.gameObject.activeSelf) Dim(_things[i], 0.45f);
        }

        private IEnumerator Nod()
        {
            float t = 0f;
            const float seconds = 0.35f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _alien.localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(k * Mathf.PI * 2f));
                yield return null;
            }
            _alien.localRotation = Quaternion.identity;
        }

        /// <summary>¡Palabra descifrada! La cosa vuela al diccionario y el nuri festeja.</summary>
        private IEnumerator Celebrate(int word, ContactScene scene)
        {
            int slot = _lesson.Targets.IndexOf(word);
            var target = scene.Things[scene.Target];
            var from = _things[scene.Target];
            int hearings = _lesson.Progress[word].HearingsToDecode;
            Sfx(ContactSounds.Decoded(), 0.55f);
            ShowMark(from, true);
            StartCoroutine(UiFx.SparkBurst(_fx, LocalIn(_fx, from.Rect), NeuroStyle.Sun, 12, 140f, 26f));
            _toast.Show("¡Descifrada!", $"{ContactContract.Words[word]} = {ContactContract.Meanings[word]}", NeuroStyle.Lime, 1.8f);
            SetCaption($"{ContactContract.Words[word]} es {ContactContract.Meanings[word]}", hearings > 0 && hearings <= ContactContract.MinHearings ? "¡A la primera deducción!" : "");
            StartCoroutine(JumpAlien());

            if (slot >= 0 && slot < _slots.Count)
            {
                var fly = NewImage(_fx, "Fly", null);
                SetSlotIcon(fly, null, word, target);
                fly.gameObject.SetActive(true);
                var r = fly.rectTransform;
                r.sizeDelta = Vector2.one * ThingSize * 0.7f;
                Vector2 a = LocalIn(_fx, from.Rect), b = LocalIn(_fx, _slots[slot].Rect);
                float t = 0f;
                const float seconds = 0.55f;
                while (t < seconds)
                {
                    t += GameClock.DeltaTime;
                    float k = UiFx.EaseOutCubic(Mathf.Clamp01(t / seconds));
                    r.anchoredPosition = Vector2.Lerp(a, b, k) + new Vector2(0f, 110f * Mathf.Sin(Mathf.PI * k));
                    r.localScale = Vector3.one * Mathf.Lerp(1f, SlotSize * 0.8f / (ThingSize * 0.7f), k);
                    yield return null;
                }
                Destroy(fly.gameObject);
                RevealSlot(slot, word, target);
            }
            yield return StartCoroutine(Pause(0.9f));
        }

        private IEnumerator JumpAlien()
        {
            float t = 0f;
            const float seconds = 0.45f;
            Vector2 basePos = new Vector2(0f, _alienY);
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                _alien.anchoredPosition = basePos + new Vector2(0f, 46f * Mathf.Sin(k * Mathf.PI));
                _alienImg.sprite = ContactSprites.Alien(k < 0.8f);
                yield return null;
            }
            _alien.anchoredPosition = basePos;
            _alienImg.sprite = ContactSprites.Alien(false);
        }

        // ------------------------------------------------------------------ fin

        private void Finish()
        {
            _done = true;
            int n = _lesson.Practice ? _practiceTotal : _lesson.Targets.Count;
            int d = _lesson.Practice ? _practiceOk : _lesson.DecodedCount;
            float mean = _lesson.Practice ? -1f : ContactContract.MeanHearings(_lesson);
            int score = _lesson.Practice
                ? Mathf.RoundToInt(100f * _practiceOk / Mathf.Max(1, _practiceTotal))
                : ContactContract.Score(d, n, mean, _dda.PeakLevel);
            _exit.Show();
            string title = _lesson.Practice ? "¡Buena charla!" : d == n ? "¡Descifraste todo!" : d > 0 ? "¡Buen contacto!" : "Primer contacto";
            _resultRoot.Find("Title").GetComponent<Text>().text = title;
            _resultRoot.Find("Detail").GetComponent<Text>().text = _lesson.Practice
                ? $"Entendiste {d} de {n} frases"
                : $"Descifraste {d} de {n} palabras";
            _resultRoot.Find("Extra").GetComponent<Text>().text = mean > 0f
                ? $"En {mean.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',')} escenas por palabra"
                : "";
            _resultRoot.gameObject.SetActive(true);
            StartCoroutine(AnimateResult(score));
            SendTelemetry(score, d, n, mean);
        }

        private void SendTelemetry(int score, int correct, int total, float mean)
        {
            var decoded = new List<int>();
            var hearings = new List<int>();
            if (!_lesson.Practice)
                foreach (int w in _lesson.Targets)
                {
                    var p = _lesson.Progress[w];
                    if (!p.Decoded) continue;
                    decoded.Add(w);
                    hearings.Add(p.HearingsToDecode);
                }
            var metrics = new StroopSessionMetrics
            {
                correct_trials = correct,
                total_trials = total,
                calculated_score = score,
                average_response_time_ms = 0,
                level = _config.config.level,
                timed = _config.config.timed,
                end_rating = _dda.RatingNormalized,
                peak_level = _dda.PeakLevel,
                contact_lesson = _lesson.Targets.ToArray(),
                contact_decoded = decoded.ToArray(),
                contact_hearings = hearings.ToArray(),
                contact_review = _reviewWords.GetRange(0, _reviewOk.Count).ToArray(),
                contact_review_ok = _reviewOk.ToArray(),
                contact_mean_hearings = mean,
                contact_me_total = _lesson.Practice ? -1 : _meTotal,
                contact_me_ok = _lesson.Practice ? -1 : _meOk,
                contact_practice = _lesson.Practice ? 1 : 0,
            };
            var telemetry = new StroopTelemetry { user_id = _config.user_id, game_id = _config.game_id, session_metrics = metrics };
            NativeBridge.ForwardTelemetryToPlatform(JsonUtility.ToJson(telemetry));
        }

        // ------------------------------------------------------------------ entrada

        private void Update()
        {
            if (_done || GameClock.DeltaTime <= 0f) return;
            if (!Input.GetMouseButtonDown(0)) return;
            Vector2 screen = Input.mousePosition;
            if (_alien != null && _alien.gameObject.activeSelf && Hit(_alien, screen, AlienSize * 0.55f))
            {
                if (_scene != null && !_speaking) _repeatAsked = true;
                return;
            }
            if (!_waitTap || _scene == null) return;
            for (int i = 0; i < _scene.Things.Length && i < _things.Count; i++)
                if (Hit(_things[i].Rect, screen, ThingSize * 0.55f)) { _tapped = i; return; }
        }

        private static bool Hit(RectTransform r, Vector2 screen, float radius) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(r, screen, null, out var local) && local.magnitude <= radius;

        // ------------------------------------------------------------------ ayudas

        private void SetThing(ThingView v, Thing t)
        {
            var sprite = ContactSprites.Object(t);
            int count = Mathf.Clamp(t.Count, 1, 3);
            for (int k = 0; k < 3; k++)
            {
                var img = v.Copies[k];
                bool on = k < count;
                img.gameObject.SetActive(on);
                if (!on) continue;
                img.sprite = sprite;
                img.color = Color.white;
                float size = count == 1 ? 170f : count == 2 ? 120f : 104f;
                img.rectTransform.sizeDelta = new Vector2(size, size);
                Vector2 pos;
                if (count == 1) pos = new Vector2(0f, 14f);
                else if (count == 2) pos = new Vector2(k == 0 ? -52f : 52f, 10f);
                else pos = k == 2 ? new Vector2(0f, 62f) : new Vector2(k == 0 ? -54f : 54f, -6f);
                img.rectTransform.anchoredPosition = pos;
            }
            v.Base.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.8f);
            v.Glow.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.3f);
        }

        private static void Undim(ThingView v)
        {
            foreach (var c in v.Copies) if (c.gameObject.activeSelf) c.color = Color.white;
            v.Glow.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.3f);
        }

        /// <summary>
        /// La pista "la vez anterior" bajo el globo: las cosas que había la última vez que sonó la palabra (la que se
        /// repite es la respuesta). Primera vez: lo dice. Apagada desde el nivel 5 (hay que recordarlo).
        /// </summary>
        private void ShowHint(Thing[] previous, bool on)
        {
            _hintRoot.gameObject.SetActive(on);
            if (!on) return;
            int n = previous == null ? 0 : Mathf.Min(previous.Length, _hintIcons.Count);
            _hintLabel.text = n == 0 ? "Primera vez que suena: elige la que quieras" : "La vez anterior:";
            float labelW = Mathf.Min(_hintLabel.preferredWidth, _play.rect.width - 2f * MarginU);
            const float item = 112f;
            float total = labelW + (n > 0 ? 18f + n * item : 0f);
            float x = -total * 0.5f;
            _hintLabel.rectTransform.sizeDelta = new Vector2(labelW + 4f, 60f);
            _hintLabel.rectTransform.anchoredPosition = new Vector2(x + labelW * 0.5f, 0f);
            x += labelW + 18f;
            for (int i = 0; i < _hintIcons.Count; i++)
            {
                var img = _hintIcons[i];
                bool show = i < n;
                img.transform.parent.gameObject.SetActive(show);
                if (!show) continue;
                var t = previous[i];
                img.sprite = ContactSprites.Object(t);
                ((RectTransform)img.transform.parent).anchoredPosition = new Vector2(x + item * 0.5f, 0f);
                _hintCounts[i].text = t.Count > 1 ? "×" + t.Count : "";
                x += item;
            }
            StartCoroutine(PopIn(_hintRoot, 0.2f));
        }

        private static void Dim(ThingView v, float alpha)
        {
            foreach (var c in v.Copies) if (c.gameObject.activeSelf) c.color = new Color(1f, 1f, 1f, alpha);
            v.Glow.color = NeuroStyle.WithAlpha(NeuroStyle.Sky, 0.08f);
        }

        private void ShowMark(ThingView v, bool ok)
        {
            v.Mark.sprite = ok ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            v.Mark.gameObject.SetActive(true);
            foreach (var c in v.Copies) if (c.gameObject.activeSelf) c.color = Color.white;
            StartCoroutine(PopIn(v.Mark.rectTransform, 0.2f));
        }

        /// <summary>Globo del nuri: la frase en letras grandes; la palabra que suena, en coral.</summary>
        private void ShowBubble(IReadOnlyList<int> phrase, int current)
        {
            var parts = new string[phrase.Count];
            for (int i = 0; i < phrase.Count; i++)
            {
                string word = ContactContract.Words[phrase[i]];
                parts[i] = i == current ? $"<color=#{ColorUtility.ToHtmlStringRGB(NeuroStyle.Coral)}>{word}</color>" : word;
            }
            _bubbleText.text = string.Join("  ", parts);
            float w = Mathf.Clamp(_bubbleText.preferredWidth + 90f, 260f, _play.rect.width - 2f * MarginU);
            _bubble.sizeDelta = new Vector2(w, 128f);
            if (!_bubble.gameObject.activeSelf)
            {
                _bubble.gameObject.SetActive(true);
                StartCoroutine(PopIn(_bubble, 0.2f));
            }
        }

        private static string Translate(IReadOnlyList<int> phrase)
        {
            var parts = new string[phrase.Count];
            for (int i = 0; i < phrase.Count; i++) parts[i] = ContactContract.Meanings[phrase[i]];
            return string.Join(" · ", parts);
        }

        private void SetCaption(string main, string sub)
        {
            _caption.text = main;
            _sub.text = sub;
        }

        private void UpdateProgressInfo()
        {
            if (_lesson == null || _lesson.Practice) return;
            _hud.SetInfo($"Descifradas {_lesson.DecodedCount} de {_lesson.Targets.Count}");
        }

        private void Sfx(AudioClip clip, float volume)
        {
            if (clip != null && GameFeel.SoundOn && (_config == null || _config.config == null || _config.config.sound_enabled))
                _audioSource.PlayOneShot(clip, volume);
        }

        private static IEnumerator Pause(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += GameClock.DeltaTime;
                yield return null;
            }
        }

        // ------------------------------------------------------------------ diccionario

        private void BuildDictionary()
        {
            int n = _lesson.Targets.Count;
            _dictRoot.gameObject.SetActive(true);
            int perRow = n <= 5 ? n : (n + 1) / 2;
            float gapX = SlotSize + 34f, rowH = SlotSize + 66f;
            int rows = (n + perRow - 1) / perRow;
            for (int i = 0; i < _slots.Count; i++)
            {
                var v = _slots[i];
                bool on = i < n;
                v.Rect.gameObject.SetActive(on);
                if (!on) continue;
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, n - row * perRow);
                v.Rect.anchoredPosition = new Vector2((col - (inRow - 1) * 0.5f) * gapX, ((rows - 1) * 0.5f - row) * rowH - 10f);
                int w = _lesson.Targets[i];
                v.Word.text = ContactContract.Words[w];
                v.Icon.gameObject.SetActive(false);
                v.Digit.gameObject.SetActive(false);
                v.Mystery.gameObject.SetActive(true);
                v.Note.text = "";
                v.Ring.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.3f);
                SetPips(v, 0);
                StartCoroutine(PopInDelayed(v.Rect, 0.05f * i));
            }
            _dictLabel.rectTransform.anchoredPosition = new Vector2(0f, rows * rowH * 0.5f + 12f);
        }

        private void HighlightSlots(IReadOnlyList<int> phrase, bool on)
        {
            for (int i = 0; i < _lesson.Targets.Count && i < _slots.Count; i++)
            {
                int w = _lesson.Targets[i];
                var p = _lesson.Progress[w];
                bool lit = on && p.Open && ContainsWord(phrase, w);
                _slots[i].Ring.color = p.Decoded ? NeuroStyle.Lime : lit ? NeuroStyle.Sun : NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.3f);
                if (lit) StartCoroutine(PopRect(_slots[i].Rect, 1.12f, 0.2f));
            }
        }

        private static bool ContainsWord(IReadOnlyList<int> phrase, int w)
        {
            foreach (int x in phrase) if (x == w) return true;
            return false;
        }

        private void RevealSlot(int slot, int word, Thing seen)
        {
            var v = _slots[slot];
            v.Mystery.gameObject.SetActive(false);
            SetSlotIcon(v.Icon, v.Digit, word, seen);
            v.Ring.color = NeuroStyle.Lime;
            v.Note.text = "";
            SetPips(v, 2);
            StartCoroutine(PopRect(v.Rect, 1.3f, 0.25f));
            StartCoroutine(UiFx.SparkBurst(_fx, LocalIn(_fx, v.Rect), NeuroStyle.Sun, 8, 80f, 16f));
            UpdateProgressInfo();
        }

        /// <summary>Hueco de una palabra que falta: tu idea (lo último que tocaste, tenue, con el "?" encima) y los puntos.</summary>
        private void UpdateSlot(int slot)
        {
            int w = _lesson.Targets[slot];
            var p = _lesson.Progress[w];
            var v = _slots[slot];
            SetPips(v, Mathf.Clamp(p.Streak, 0, 2));
            if (!_guess.TryGetValue(w, out var g)) return;
            int shown;
            switch (ContactContract.KindOf(w))
            {
                case WordKind.Color:
                    if (g.Color < 0) return;
                    shown = ContactContract.ColorWord(g.Color);
                    break;
                case WordKind.Count: shown = ContactContract.CountWord(g.Count); break;
                default: shown = ContactContract.NounWord(g.Obj); break;
            }
            SetSlotIcon(v.Icon, v.Digit, shown, g);
            v.Icon.color = NeuroStyle.WithAlpha(v.Icon.color, 0.35f);
            v.Digit.color = NeuroStyle.WithAlpha(NeuroStyle.Sun, 0.4f);
            v.Mystery.gameObject.SetActive(true);
        }

        private static void SetPips(SlotView v, int on)
        {
            for (int k = 0; k < v.Pips.Length; k++)
                v.Pips[k].color = k < on ? NeuroStyle.Sun : NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.22f);
        }

        /// <summary>Qué muestra el diccionario: la cosa (sin color), el color (una gota de arcilla) o el número.</summary>
        private static void SetSlotIcon(Image icon, Text digit, int word, Thing seen)
        {
            int value = ContactContract.ValueOf(word);
            switch (ContactContract.KindOf(word))
            {
                case WordKind.Color:
                    icon.sprite = DiscSprite.Get();
                    icon.color = ContactSprites.Palette[value];
                    icon.gameObject.SetActive(true);
                    break;
                case WordKind.Count:
                    if (digit != null)
                    {
                        digit.text = value.ToString();
                        digit.color = NeuroStyle.Sun;
                        digit.gameObject.SetActive(true);
                        icon.gameObject.SetActive(false);
                    }
                    else
                    {
                        icon.sprite = ContactSprites.Object(new Thing(seen.Obj, seen.Color));
                        icon.color = Color.white;
                        icon.gameObject.SetActive(true);
                    }
                    break;
                default:
                    icon.sprite = ContactSprites.Object(new Thing(value));
                    icon.color = Color.white;
                    icon.gameObject.SetActive(true);
                    break;
            }
        }

        private void ShowParked(int word, bool silent = false)
        {
            int slot = _lesson.Targets.IndexOf(word);
            if (slot < 0 || slot >= _slots.Count) return;
            var v = _slots[slot];
            v.Note.text = "otro día";
            v.Ring.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.15f);
            if (!silent)
            {
                Sfx(ContactSounds.Parked(), 0.45f);
                _toast.Show($"{ContactContract.Words[word]} queda para otro día", "Se aprende mejor con una pausa", NeuroStyle.Grape, 1.8f);
            }
        }

        private IEnumerator PopInDelayed(RectTransform r, float delay)
        {
            r.localScale = Vector3.zero;
            yield return StartCoroutine(Pause(delay));
            yield return StartCoroutine(PopIn(r, 0.28f));
        }

        // ------------------------------------------------------------------ construcción de UI

        protected override void BuildUi()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var canvasGo = new GameObject("ContactCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            Stretch(bgRect);
            WorldBackdrop.Build(bgRect, GameWorld.FirstContact);

            var safeGo = new GameObject("SafeAreaContent");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _hud = new GameHud(_safe, "Primer Contacto", MarginU + 20f, this, withStreak: false);

            _play = Layer(_safe, "Play");

            // El nuri, con un resplandor lima detrás.
            var alienGo = new GameObject("Nuri");
            alienGo.transform.SetParent(_play, false);
            _alien = alienGo.AddComponent<RectTransform>();
            _alien.anchorMin = _alien.anchorMax = new Vector2(0.5f, 0.5f);
            _alien.sizeDelta = new Vector2(AlienSize, AlienSize);
            _alienGlow = NewImage(_alien, "Glow", RadialGlowSprite.Get());
            _alienGlow.rectTransform.sizeDelta = Vector2.one * AlienSize * 2.2f;
            _alienGlow.color = NeuroStyle.WithAlpha(NeuroStyle.Lime, 0.22f);
            _alienGlow.gameObject.SetActive(true);
            _alienImg = NewImage(_alien, "Body", ContactSprites.Alien(false));
            Stretch(_alienImg.rectTransform);
            _alienImg.gameObject.SetActive(true);
            alienGo.SetActive(false);

            // Globo de diálogo: sombra dura, borde tinta, punta y relleno crema.
            var bubbleGo = new GameObject("Bubble");
            bubbleGo.transform.SetParent(_play, false);
            _bubble = bubbleGo.AddComponent<RectTransform>();
            _bubble.anchorMin = _bubble.anchorMax = new Vector2(0.5f, 0.5f);
            var shadow = NewImage(_bubble, "Shadow", RoundedRectSprite.Get(56));
            shadow.type = Image.Type.Sliced;
            Stretch(shadow.rectTransform);
            shadow.rectTransform.offsetMin = new Vector2(-6f, -16f);
            shadow.rectTransform.offsetMax = new Vector2(6f, -4f);
            shadow.color = NeuroStyle.Ink;
            shadow.gameObject.SetActive(true);
            var border = NewImage(_bubble, "Border", RoundedRectSprite.Get(56));
            border.type = Image.Type.Sliced;
            Stretch(border.rectTransform);
            border.rectTransform.offsetMin = new Vector2(-6f, -6f);
            border.rectTransform.offsetMax = new Vector2(6f, 6f);
            border.color = NeuroStyle.Ink;
            border.gameObject.SetActive(true);
            var tail = NewImage(_bubble, "Tail", ContactSprites.Tail());
            tail.rectTransform.anchorMin = tail.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            tail.rectTransform.sizeDelta = new Vector2(64f, 64f);
            tail.rectTransform.anchoredPosition = new Vector2(0f, 18f);
            tail.gameObject.SetActive(true);
            var fill = NewImage(_bubble, "Fill", RoundedRectSprite.Get(56));
            fill.type = Image.Type.Sliced;
            Stretch(fill.rectTransform);
            fill.color = NeuroStyle.Cream;
            fill.gameObject.SetActive(true);
            _bubbleText = MakeText(_bubble, "Phrase", 76, TextAnchor.MiddleCenter, NeuroStyle.Ink, 0f, 0f);
            Stretch(_bubbleText.rectTransform);
            _bubbleText.supportRichText = true;
            _bubbleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            bubbleGo.SetActive(false);

            // Pista "la vez anterior": rótulo suelto y las cosas en miniatura (sin recuadro).
            var hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(_play, false);
            _hintRoot = hintGo.AddComponent<RectTransform>();
            _hintRoot.anchorMin = _hintRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _hintRoot.sizeDelta = new Vector2(900f, 110f);
            _hintLabel = MakeText(_hintRoot, "Label", 34, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.85f), 0f, 0f);
            _hintLabel.rectTransform.anchorMin = _hintLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _hintLabel.gameObject.SetActive(true);
            for (int i = 0; i < 5; i++)
            {
                var slotGo = new GameObject("Mini");
                slotGo.transform.SetParent(_hintRoot, false);
                var sr = slotGo.AddComponent<RectTransform>();
                sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
                sr.sizeDelta = new Vector2(104f, 104f);
                var disc = NewImage(sr, "Disc", DiscSprite.Get());
                Stretch(disc.rectTransform);
                disc.color = NeuroStyle.WithAlpha(Deep, 0.7f);
                disc.gameObject.SetActive(true);
                var icon = NewImage(sr, "Icon", null);
                icon.rectTransform.sizeDelta = new Vector2(88f, 88f);
                icon.gameObject.SetActive(true);
                var count = MakeText(sr, "Count", 26, TextAnchor.LowerRight, NeuroStyle.Sun, 0f, 0f);
                Stretch(count.rectTransform);
                NeuroStyle.ClayText(count, 2f, 3f);
                slotGo.SetActive(false);
                _hintIcons.Add(icon);
                _hintCounts.Add(count);
            }
            hintGo.SetActive(false);

            _thingsRoot = Layer(_play, "Things");
            for (int i = 0; i < 5; i++) _things.Add(BuildThing(_thingsRoot));

            _caption = MakeText(_play, "Caption", 46, TextAnchor.MiddleCenter, Color.white, 0f, 0f);
            NeuroStyle.ClayText(_caption, 3.5f, 5f);
            _caption.rectTransform.anchorMin = _caption.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            BestFit(_caption, 30);
            _sub = MakeText(_play, "Sub", 34, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.8f), 0f, 0f);
            _sub.rectTransform.anchorMin = _sub.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            BestFit(_sub, 24);

            // Diccionario de la partida: rótulo suelto y huecos (sin recuadro).
            _dictRoot = Layer(_play, "Dictionary");
            _dictRoot.anchorMin = _dictRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _dictLabel = MakeText(_dictRoot, "Label", 30, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.7f), 0f, 0f);
            _dictLabel.rectTransform.anchorMin = _dictLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _dictLabel.rectTransform.sizeDelta = new Vector2(500f, 44f);
            _dictLabel.text = "diccionario nuri";
            _dictLabel.gameObject.SetActive(true);
            for (int i = 0; i < 8; i++) _slots.Add(BuildSlot(_dictRoot));
            _dictRoot.gameObject.SetActive(false);

            _fx = Layer(_play, "Fx");

            BuildResultPanel();
            _exit = new ExitButton(_safe, this, UnitsPerDp);
            _toast = new Toast(_safe, this, UnitsPerDp);
            _toast.SetTopOffset(0f);

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(canvasGo.transform, false);
            Stretch(flashGo.AddComponent<RectTransform>());
            _flash = flashGo.AddComponent<Image>();
            _flash.raycastTarget = false;
            _flash.color = new Color(0f, 0f, 0f, 0f);

            _countdown = new CountdownScreen(canvasGo.transform, UnitsPerDp);
        }

        private ThingView BuildThing(Transform parent)
        {
            var go = new GameObject("Thing");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(ThingSize, ThingSize);
            var v = new ThingView { Rect = r };
            // Plataforma de luz (holograma): resplandor y un aro aplastado debajo.
            v.Glow = NewImage(r, "Glow", RadialGlowSprite.Get());
            v.Glow.rectTransform.sizeDelta = new Vector2(ThingSize * 1.5f, ThingSize * 0.8f);
            v.Glow.rectTransform.anchoredPosition = new Vector2(0f, -ThingSize * 0.4f);
            v.Glow.gameObject.SetActive(true);
            v.Base = NewImage(r, "Base", RingSprite.Get());
            v.Base.rectTransform.sizeDelta = new Vector2(ThingSize * 0.9f, ThingSize * 0.26f);
            v.Base.rectTransform.anchoredPosition = new Vector2(0f, -ThingSize * 0.4f);
            v.Base.gameObject.SetActive(true);
            v.Ring = NewImage(r, "Chosen", RingSprite.Get());
            v.Ring.rectTransform.sizeDelta = new Vector2(ThingSize * 1.08f, ThingSize * 1.08f);
            v.Ring.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            for (int k = 0; k < 3; k++)
            {
                v.Copies[k] = NewImage(r, "Copy", null);
                v.Copies[k].gameObject.SetActive(false);
            }
            v.Mark = NewImage(r, "Mark", null);
            v.Mark.rectTransform.sizeDelta = new Vector2(66f, 66f);
            v.Mark.rectTransform.anchoredPosition = new Vector2(ThingSize * 0.4f, ThingSize * 0.4f);
            go.SetActive(false);
            return v;
        }

        private SlotView BuildSlot(Transform parent)
        {
            var go = new GameObject("Slot");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(SlotSize, SlotSize);
            var v = new SlotView { Rect = r };
            v.Bg = NewImage(r, "Bg", DiscSprite.Get());
            Stretch(v.Bg.rectTransform);
            v.Bg.color = Deep;
            v.Bg.gameObject.SetActive(true);
            v.Ring = NewImage(r, "Ring", RingSprite.Get());
            Stretch(v.Ring.rectTransform);
            v.Ring.color = NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.3f);
            v.Ring.gameObject.SetActive(true);
            v.Icon = NewImage(r, "Icon", null);
            v.Icon.rectTransform.sizeDelta = new Vector2(SlotSize * 0.78f, SlotSize * 0.78f);
            for (int k = 0; k < 2; k++)
            {
                var pip = NewImage(r, "Pip", DiscSprite.Get());
                pip.rectTransform.sizeDelta = new Vector2(20f, 20f);
                pip.rectTransform.anchoredPosition = new Vector2(k == 0 ? -14f : 14f, SlotSize * 0.5f + 16f);
                pip.gameObject.SetActive(true);
                v.Pips[k] = pip;
            }
            v.Mystery = MakeText(r, "Mystery", 56, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.55f), 0f, 0f);
            Stretch(v.Mystery.rectTransform);
            v.Mystery.text = "?";
            v.Digit = MakeText(r, "Digit", 60, TextAnchor.MiddleCenter, NeuroStyle.Sun, 0f, 0f);
            Stretch(v.Digit.rectTransform);
            NeuroStyle.ClayText(v.Digit, 3f, 4f);
            v.Digit.gameObject.SetActive(false);
            v.Word = MakeText(r, "Word", 32, TextAnchor.MiddleCenter, NeuroStyle.Cream, 0f, 0f);
            v.Word.rectTransform.anchorMin = v.Word.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            v.Word.rectTransform.sizeDelta = new Vector2(SlotSize + 40f, 40f);
            v.Word.rectTransform.anchoredPosition = new Vector2(0f, -SlotSize * 0.5f - 24f);
            v.Note = MakeText(r, "Note", 22, TextAnchor.MiddleCenter, NeuroStyle.WithAlpha(NeuroStyle.Cream, 0.6f), 0f, 0f);
            v.Note.rectTransform.anchorMin = v.Note.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            v.Note.rectTransform.sizeDelta = new Vector2(SlotSize + 40f, 30f);
            v.Note.rectTransform.anchoredPosition = new Vector2(0f, -SlotSize * 0.5f - 52f);
            go.SetActive(false);
            return v;
        }

        private void BuildResultPanel()
        {
            var go = new GameObject("Result");
            go.transform.SetParent(_safe, false);
            _resultRoot = go.AddComponent<RectTransform>();
            _resultRoot.anchorMin = _resultRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _resultRoot.pivot = new Vector2(0.5f, 0.5f);
            _resultRoot.sizeDelta = new Vector2(880f, 760f);
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite.Get(64);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.08f, 0.12f, 0.22f, 0.96f);
            AddResultText("Title", 76, new Vector2(0f, 250f), Color.white);
            AddResultText("Score", 220, new Vector2(0f, 60f), Color.white);
            AddResultText("Detail", 46, new Vector2(0f, -150f), new Color(1f, 1f, 1f, 0.85f));
            AddResultText("Extra", 42, new Vector2(0f, -250f), new Color(1f, 1f, 1f, 0.65f));
            go.SetActive(false);
        }

        private static RectTransform Layer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            Stretch(r);
            return r;
        }

        private static Image NewImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            go.SetActive(false);
            return img;
        }

        // ------------------------------------------------------------------ layout

        private void Layout()
        {
            Canvas.ForceUpdateCanvases();
            float w = _play.rect.width, h = _play.rect.height;
            float top = h * 0.5f - GameHud.Height - 10f;
            _alienY = top - AlienSize * 0.5f;
            _alien.anchoredPosition = new Vector2(0f, _alienY);
            _bubbleY = _alienY - AlienSize * 0.5f - 90f;
            _bubble.anchoredPosition = new Vector2(0f, _bubbleY);

            float bottom = -h * 0.5f + 40f;
            float dictH = 2f * (SlotSize + 66f);
            _dictY = bottom + dictH * 0.5f;
            _dictRoot.anchoredPosition = new Vector2(0f, _dictY);
            float dictTop = _dictY + dictH * 0.5f + 40f;

            float captionY = dictTop + 70f;
            _caption.rectTransform.sizeDelta = new Vector2(w - 2f * MarginU, 70f);
            _caption.rectTransform.anchoredPosition = new Vector2(0f, captionY + 26f);
            _sub.rectTransform.sizeDelta = new Vector2(w - 2f * MarginU, 50f);
            _sub.rectTransform.anchoredPosition = new Vector2(0f, captionY - 34f);

            _hintY = _bubbleY - 64f - 76f;
            _hintRoot.anchoredPosition = new Vector2(0f, _hintY);
            float zoneTop = _hintY - 55f - 15f;
            float zoneBottom = captionY + 80f;
            _thingsY = (zoneTop + zoneBottom) * 0.5f;
        }

        /// <summary>Lugares de las cosas: una fila hasta 3; 2 + 2; 3 + 2.</summary>
        private void PlaceThings(int n)
        {
            _thingPos.Clear();
            float w = _play.rect.width - 2f * MarginU;
            float gap = Mathf.Min(ThingSize + 60f, w / 3f);
            float rowGap = ThingSize + 40f;
            if (n <= 3)
            {
                for (int i = 0; i < n; i++) _thingPos.Add(new Vector2((i - (n - 1) * 0.5f) * gap, _thingsY));
                return;
            }
            int firstRow = n == 4 ? 2 : 3;
            for (int i = 0; i < n; i++)
            {
                int row = i < firstRow ? 0 : 1;
                int col = row == 0 ? i : i - firstRow;
                int inRow = row == 0 ? firstRow : n - firstRow;
                float x = (col - (inRow - 1) * 0.5f) * gap;
                float y = _thingsY + (row == 0 ? rowGap * 0.5f : -rowGap * 0.5f);
                _thingPos.Add(new Vector2(x, y));
            }
        }
    }
}
