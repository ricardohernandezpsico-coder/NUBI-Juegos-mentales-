using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Acoplamiento
{
    public sealed partial class DockingGameController
    {
        private enum PortState { Idle, Ok, Show }

        private PortState _portState = PortState.Idle;
        /// <summary>La pieza horneada de un módulo: la MOSTRADA (la lámpara en la orientación que se ve), la ALINEADA (0° y sin espejo: la de la revelación), la ASENTADA (a ras del hueco), y el hueco (la cavidad y el cordón).</summary>
        private Sprite _dispSprite, _alignedRaised, _alignedSeated, _cavitySprite, _cordonSprite;
        private float _dispSideBlocks = DockingArt.AlignedFrameBlocks;
        // el módulo SIGUIENTE se hornea mientras se revela el actual (de a poco, sin trabar): así no hay espera entre módulos
        private DockingTrial _preTrial;
        private Sprite _preDisp, _preCavity, _preCordon;
        private float _preSide;
        private bool _preReady;
        private Coroutine _preRoutine;
        private bool _poseAligned, _poseSeated;
        private int _moduleNo;
        private float _fuelFraction = 1f, _timerFraction = 1f;
        private float _landAt = -10f;
        private Image _timerTrack, _timerFill;

        // ------------------------------------------------------------------ reposo y módulo

        /// <summary>El estado de reposo: sin módulo, sin aviso, sin barra, puerto quieto, fila de luces según lo acoplado y sin pantalla final.</summary>
        private void ResetViews()
        {
            DiscardPrebake();
            ReleasePiece();
            _trial = null;
            _moduleRoot.gameObject.SetActive(false);
            _portEdge.gameObject.SetActive(false);
            _portHole.gameObject.SetActive(false);
            _portMark.gameObject.SetActive(false);
            _portState = PortState.Idle;
            _shownAnswer = NoAnswer;
            _hud.CancelAnnounce();
            SetFuelVisible(false);
            _hint.gameObject.SetActive(false);
            _lightGlow.gameObject.SetActive(false);
            _hud.Rect.gameObject.SetActive(true);
            _hudLayer.gameObject.SetActive(true);
            _portLayer.gameObject.SetActive(true);
            _moduleLayer.gameObject.SetActive(true);
            _buttonLayer.gameObject.SetActive(true);
            _stationShown = 0;
            _landAt = -10f;
            _moduleNo = 0;
            RefreshLights(Now);
            RefreshButtons(Now);
            RefreshPort(Now);
            UpdateHudExtras();
        }

        /// <summary>Hornea el módulo (con la lámpara en la orientación que se ve) y el hueco del puerto de un intento y los deja en su lugar; el módulo todavía está escondido (llega en <see cref="DoArrive"/>). La pieza alineada (la de la revelación) se hornea después, mientras se decide.</summary>
        private void BeginTrial(DockingTrial trial)
        {
            _trial = trial;
            _moduleNo = _trials + 1;
            ReleasePiece();
            if (_preTrial == trial && _preReady)
            {
                _dispSprite = _preDisp; _dispSideBlocks = _preSide; _cavitySprite = _preCavity; _cordonSprite = _preCordon;                  // ya horneados mientras se revelaba el módulo anterior
                _preDisp = _preCavity = _preCordon = null; _preTrial = null; _preReady = false; _preRoutine = null;
            }
            else
            {
                if (_preTrial != null && _preTrial != trial) DiscardPrebake();
                _dispSprite = DockingArt.BakeDisplayed(trial.Shape, -trial.AngleDeg, trial.Mirrored, out _dispSideBlocks);                    // en Unity un ángulo positivo gira hacia la izquierda; en el juego, hacia la derecha
                DockingArt.BakeHole(trial.Shape, out _cavitySprite, out _cordonSprite);
            }
            _poseAligned = false;
            _poseSeated = false;
            _moduleBody.sprite = _dispSprite;
            _portHole.sprite = _cavitySprite;
            _portEdge.sprite = _cordonSprite;
            _portEdge.gameObject.SetActive(true);
            _portHole.gameObject.SetActive(true);
            _portMark.gameObject.SetActive(false);
            _answer = NoAnswer;
            _shownAnswer = NoAnswer;
            _portState = PortState.Idle;
            _moduleRoot.gameObject.SetActive(false);
            LayoutModule();
            RefreshButtons(Now);
#if UNITY_EDITOR
            _botPick = NoAnswer;
#endif
        }

        /// <summary>La pieza alineada y la asentada (0° y sin espejo: las que se ven en la revelación). Se hornean de a una mientras se decide (en cuadros distintos, para no trabar el juego); si todavía no están cuando se las pide, se hornean ahí.</summary>
        private void EnsureAligned(bool seated)
        {
            if (_trial == null) return;
            if (_alignedRaised == null) _alignedRaised = DockingArt.BakeAlignedRaised(_trial.Shape);
            if (seated && _alignedSeated == null) _alignedSeated = DockingArt.BakeAlignedSeated(_trial.Shape);
        }

        /// <summary>Hornea la pieza alineada y después la asentada DE A POCO mientras se decide (unos milisegundos por cuadro); si la revelación las pide antes, <see cref="EnsureAligned"/> las hornea de una vez y esto las suelta.</summary>
        private IEnumerator BakeAlignedSoon(DockingTrial trial)
        {
            yield return null;
            for (int pass = 0; pass < 2; pass++)
            {
                bool seated = pass == 1;
                if (_trial != trial || (seated ? _alignedSeated : _alignedRaised) != null) continue;
                int n = DockingArt.AlignedPx;
                var px = new Color32[n * n];
                var it = DockingArt.FaceRoutine(new DockingArt.PieceGeo(trial.Shape), n, DockingArt.Bp, 0f, false, seated ? DockingArt.SeatedStyle() : DockingArt.RaisedStyle(), px);
                var work = Background.RunEnumerator(it);                                         // los pixeles se calculan en un hilo de trabajo (Tarea 190): el principal solo espera cuadro a cuadro
                while (!work.IsDone)
                {
                    yield return null;
                    if (_trial != trial) yield break;
                }
                if (work.Error != null) throw new System.AggregateException(work.Error);
                if (_trial != trial) yield break;
                if (seated) { if (_alignedSeated == null) _alignedSeated = DockingArt.MakeSprite(px, n); }
                else if (_alignedRaised == null) _alignedRaised = DockingArt.MakeSprite(px, n);
                yield return null;
            }
        }

        // ------------------------------------------------------------------ el módulo siguiente, horneado de antemano

        /// <summary>El módulo de un intento nuevo (en el Editor, las capturas pueden forzar el espejo y el giro).</summary>
        private DockingTrial NewTrial(int level)
        {
            var trial = DockingContract.NextTrial(level, _tt != null ? _tt.Rng() : _rng);          // Contrarreloj: el azar de CADA módulo sale de la semilla del día y de su número
#if UNITY_EDITOR
            if (_editorMirror >= 0) trial.Mirrored = _editorMirror == 1;                         // las capturas fuerzan el módulo (espejo o igual, y su giro)
            if (_editorAngle != NoAngle) trial.AngleDeg = _editorAngle;
#endif
            return trial;
        }

        /// <summary>true si el módulo ya elegido sigue valiendo (en el Editor, el guion de las capturas puede pedir otro espejo o otro giro después de elegirlo).</summary>
        private bool PrebakeStillValid()
        {
#if UNITY_EDITOR
            if (_editorMirror >= 0 && _preTrial.Mirrored != (_editorMirror == 1)) return false;
            if (_editorAngle != NoAngle && _preTrial.AngleDeg != _editorAngle) return false;
#endif
            return true;
        }

        /// <summary>
        /// Elige el módulo que sigue y empieza a hornearlo (la pieza mostrada y el hueco) DE A POCO, mientras se revela el actual o corre la cuenta regresiva: el nivel ya está registrado (es el mismo que se leería al empezar el módulo y el azar se gasta en el mismo orden), así que el módulo sale igual
        /// y no hay espera entre módulos ni un cuadro trabado.
        /// </summary>
        private void StartPrebake()
        {
            bool over = Precision ? _trials >= _trialsTotal : _endsAt > 0f && Now >= _endsAt;               // (antes de la cuenta regresiva el Reto todavía no tiene su fin)
            if (_preTrial != null || _dda == null || _rng == null || over) return;
            var trial = NewTrial(_tt != null ? _tt.Level : _dda.PresentedLevel);
            _preTrial = trial;
            _preReady = false;
            _preRoutine = StartCoroutine(PrebakeRoutine(trial));
        }

        private IEnumerator PrebakeRoutine(DockingTrial trial)
        {
            var shown = new DockingArt.PieceGeo(trial.Shape);
            int nD = DockingArt.DisplayedPx(trial.Shape), nA = DockingArt.AlignedPx;
            var disp = new Color32[nD * nD];
            var cavity = new Color32[nA * nA];
            var cordon = new Color32[nA * nA];
            var a = DockingArt.FaceRoutine(shown, nD, DockingArt.Bp, -trial.AngleDeg, trial.Mirrored, DockingArt.RaisedStyle(), disp);
            var b = DockingArt.PortRoutine(shown, nA, DockingArt.Bp, 31, cavity, cordon);
            var workA = Background.RunEnumerator(a);                                           // los pixeles se calculan en hilos de trabajo (Tarea 190): el principal solo espera cuadro a cuadro
            var workB = Background.RunEnumerator(b);
            while (!workA.IsDone || !workB.IsDone)
            {
                yield return null;
                if (_preTrial != trial) yield break;                                             // se descartó (el tutorial desde la pausa, un reinicio): los hilos terminan solos sobre arreglos que nadie usa
            }
            if (workA.Error != null) throw new System.AggregateException(workA.Error);
            if (workB.Error != null) throw new System.AggregateException(workB.Error);
            _preDisp = DockingArt.MakeSprite(disp, nD);
            yield return null;
            if (_preTrial != trial) { DockingArt.Release(_preDisp); _preDisp = null; yield break; }
            _preCavity = DockingArt.MakeSprite(cavity, nA);
            _preCordon = DockingArt.MakeSprite(cordon, nA);
            _preSide = nD / (float)DockingArt.Bp;
            _preReady = true;
        }

        private void DiscardPrebake()
        {
            if (_preRoutine != null) { StopCoroutine(_preRoutine); _preRoutine = null; }
            DockingArt.Release(_preDisp);
            DockingArt.Release(_preCavity);
            DockingArt.Release(_preCordon);
            _preDisp = _preCavity = _preCordon = null;
            _preTrial = null;
            _preReady = false;
        }

        /// <summary>Suelta las texturas de la pieza anterior (cada módulo hornea las suyas).</summary>
        private void ReleasePiece()
        {
            if (_moduleBody != null) _moduleBody.sprite = null;
            if (_portHole != null) _portHole.sprite = null;
            if (_portEdge != null) _portEdge.sprite = null;
            DockingArt.Release(_dispSprite);
            DockingArt.Release(_alignedRaised);
            DockingArt.Release(_alignedSeated);
            DockingArt.Release(_cavitySprite);
            DockingArt.Release(_cordonSprite);
            _dispSprite = _alignedRaised = _alignedSeated = _cavitySprite = _cordonSprite = null;
            _poseAligned = _poseSeated = false;
        }

        private void HideModule()
        {
            if (_moduleRoot != null) _moduleRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ la pose del módulo en cada momento

        private static float EaseInOut(float k) => k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) / 2f;

        /// <summary>El «plop» al llegar al hueco: se hunde un 3,5 % mientras dura <see cref="RevealHold"/> y queda a ras.</summary>
        private const float PlopSquash = 0.035f;

        /// <summary>
        /// Dónde está el módulo y cómo se ve, según lo que pasa: llegando (baja desde arriba, 420 ms), esperando, o en la revelación (giro, vuelta, bajada y, si acertó, el «plop»: se hunde y queda a ras con el cordón lima; si no, se aleja y se apaga). Con «quitar animaciones» cada paso cambia de golpe al empezar su ventana de tiempo.
        /// </summary>
        private void ComputePose(float now, out float x, out float y, out float ang, out float fx, out float scale, out float alpha, out bool seated)
        {
            x = _plan.ModuleCx;
            y = _plan.ModuleCy;
            ang = _trial != null ? _trial.AngleDeg : 0f;
            fx = _trial != null && _trial.Mirrored ? -1f : 1f;
            scale = 1f;
            alpha = 1f;
            seated = false;
            var port = PortState.Idle;
            bool dec = Motion.Decorative;
            if (_trial == null) { _portState = port; return; }
            if (_phase == Phase.Arrive)
            {
                float k = dec ? UiFx.EaseOutCubic(Mathf.Clamp01((now - _arriveAt) / (DockingContract.ArriveMs / 1000f))) : 1f;
                float from = _plan.ModuleCy - 70f * _plan.K;
                y = from + (_plan.ModuleCy - from) * k;
                alpha = k;
            }
            else if (_phase == Phase.Reveal)
            {
                float t = RevealT();
                float kUp = dec ? EaseInOut(Mathf.Clamp01(t / RevealUp)) : 1f;
                ang = _trial.AngleDeg * (1f - kUp);
                float tA = RevealUp;
                if (_trial.Mirrored)
                {
                    float kF = dec ? EaseInOut(Mathf.Clamp01((t - RevealUp) / RevealFlip)) : (t >= RevealUp ? 1f : 0f);
                    fx = -1f + 2f * kF;
                    tA += RevealFlip;
                }
                float kD = dec ? EaseInOut(Mathf.Clamp01((t - tA) / RevealDown)) : (t >= tA ? 1f : 0f);
                y = _plan.ModuleCy + (_plan.HoleCy - _plan.ModuleCy) * kD;
                if (_revealDemo)
                {
                    // la demostración del tutorial: gira, se da vuelta y baja; calza (el borde se pone lima) y se apaga sin sumarse a la fila de luces
                    if (t >= tA + RevealDown) port = PortState.Ok;
                    float kD2 = Mathf.Clamp01((t - tA - RevealDown - 0.55f) / 0.3f);
                    alpha = 1f - kD2;
                }
                else if (_shownCorrect)
                {
                    // el «plop»: llega al hueco, se hunde y queda a ras (con su visto lima y la luz de la fila encendida)
                    if (t >= tA + RevealDown) port = PortState.Ok;
                    float tF = tA + RevealDown + RevealHold;
                    if (dec && t >= tA + RevealDown && t < tF) scale = 1f - PlopSquash * Mathf.Sin(Mathf.Clamp01((t - tA - RevealDown) / RevealHold) * Mathf.PI);
                    if (t >= tF) seated = true;
                }
                else
                {
                    if (t >= tA + RevealDown) port = PortState.Show;
                    float kG = Mathf.Clamp01((t - tA - RevealDown - 0.12f) / RevealGone);
                    if (kG > 0f)
                    {
                        x += dec ? 150f * kG * kG : 0f;
                        alpha = 1f - kG;
                    }
                }
            }
            else if (_phase != Phase.Decide) alpha = 0f;
            _portState = port;
        }

        private void ApplyModulePose()
        {
            if (_trial == null || _moduleRoot == null || !_moduleRoot.gameObject.activeSelf) return;
            ComputePose(Now, out float x, out float y, out float ang, out float fx, out float scale, out float alpha, out bool seated);
            bool aligned = _phase == Phase.Reveal;                                               // al responder se pasa de la pieza MOSTRADA a la ALINEADA (la que gira, se da vuelta y baja)
            if (aligned) EnsureAligned(seated);
            var sprite = aligned ? (seated ? _alignedSeated : _alignedRaised) : _dispSprite;
            if (_moduleBody.sprite != sprite) _moduleBody.sprite = sprite;
            if (_poseAligned != aligned) { _poseAligned = aligned; LayoutModule(); }
            _poseSeated = aligned && seated;
            _moduleRoot.anchoredPosition = P(x, y);
            _moduleRoot.localScale = Vector3.one * Mathf.Max(0.001f, scale);
            _moduleGroup.alpha = alpha;
            if (aligned)
            {
                _bodyRect.localRotation = Quaternion.Euler(0f, 0f, -ang);                       // en Unity el ángulo positivo gira hacia la izquierda; en el juego, hacia la derecha
                _bodyRect.localScale = new Vector3(Mathf.Abs(fx) < 0.001f ? 0.001f : fx, 1f, 1f);
            }
            else
            {
                _bodyRect.localRotation = Quaternion.identity;                                  // la pieza mostrada ya viene girada y espejada: no se toca
                _bodyRect.localScale = Vector3.one;
            }
        }

        // ------------------------------------------------------------------ un cuadro

        private void Animate(float now, float dt)
        {
            if (_phase == Phase.Arrive || _phase == Phase.Decide || _phase == Phase.Reveal) ApplyModulePose();
            else _portState = _phase == Phase.Idle ? PortState.Idle : _portState;
            RefreshPort(now);
            RefreshButtons(now);
            RefreshLights(now);
        }

        /// <summary>El cordón del hueco (verde agua en reposo, lima al encajar, celeste al «mostrar»), los chevrones (lima al encajar: parpadean; con «quitar animaciones», fijos; celestes al mostrar) y el visto lima cuando la pieza queda a ras.</summary>
        private void RefreshPort(float now)
        {
            if (_portEdge == null) return;
            _portEdge.color = _portState == PortState.Ok ? Lime : _portState == PortState.Show ? Cyan : PortEdgeIdle;
            for (int i = 0; i < ChevronCount; i++)
            {
                bool on = _portState == PortState.Ok && (!Motion.Decorative || (Mathf.FloorToInt(now / 0.09f) + i) % 2 == 0);
                string art = _portState == PortState.Ok ? (on ? "chev_lima" : "chev_apagado") : _portState == PortState.Show ? "chev_celeste" : "chev_verde_agua";
                var sprite = Art(art);
                if (sprite != null) { _chevrons[i].sprite = sprite; _chevrons[i].color = Color.white; }
                else
                {
                    _chevrons[i].sprite = DockingSprites.Chevron();
                    var tint = _portState == PortState.Ok ? Lime : _portState == PortState.Show ? Cyan : PortEdgeIdle;
                    _chevrons[i].color = on || _portState != PortState.Ok ? tint : new Color(tint.r, tint.g, tint.b, 0.35f);
                }
            }
            bool mark = _poseSeated && _portState == PortState.Ok && _phase == Phase.Reveal;
            if (mark != _portMark.gameObject.activeSelf) _portMark.gameObject.SetActive(mark);
            if (mark && _portMark.sprite == null) _portMark.sprite = AnswerMarkSprite.Check();
        }

        /// <summary>Los botones: la losa del kit (lima y uva) apagada (45 %) mientras llega el módulo, viva al decidir y en la revelación; al tocarla pasa a su imagen presionada, el texto baja lo del kit y al soltar rebota; y el tocado muestra ✓ o ✗ en la revelación.</summary>
        private void RefreshButtons(float now)
        {
            bool live = (_phase == Phase.Decide && (_tt == null || _guided || _inputOn)) || _phase == Phase.Reveal;          // Contrarreloj: apagados hasta que el puerto se abre
            for (int i = 0; i < 2; i++)
            {
                var v = _buttons[i];
                v.Group.alpha = live ? 1f : 0.45f;
                var (cx, cy) = _plan.ButtonCenter(i);
                if (v.Slab != null && v.Touch != null)
                {
                    PlasticSlab.Set(v.Slab, SlabSize.Button, v.Tone, v.Touch.Down);
                    v.Content.anchoredPosition = new Vector2(0f, -v.Touch.Drop);
                    v.Root.anchoredPosition = P(cx, cy);
                    v.Root.localScale = Vector3.one * (1f + v.Touch.Bounce);
                }
                else
                {
                    float press = now - _pressAt[i] < 0.14f && now >= _pressAt[i] ? 3f : 0f;          // sin el kit: el hundido de siempre
                    v.Root.anchoredPosition = P(cx, cy + press);
                }
                bool badge = _phase == Phase.Reveal && _shownAnswer == i;
                v.Badge.gameObject.SetActive(badge);
                if (badge) v.Badge.sprite = _shownCorrect ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            }
        }

        // ------------------------------------------------------------------ la barra de tiempo y el marcador

        private void SetFuelVisible(bool on)
        {
            _fuelBorder.gameObject.SetActive(on);
            _fuelTrack.gameObject.SetActive(on);
            _fuelFill.gameObject.SetActive(on);
            _fuelLabel.gameObject.SetActive(on);
        }

        /// <summary>La barra de «Tiempo» del módulo (llena = todo el tiempo; baja de sol a coral).</summary>
        private void SetFuel(float fraction)
        {
            _fuelFraction = Mathf.Clamp01(fraction);
            if (_fuelFill == null) return;
            var r = _fuelFill.rectTransform;
            r.sizeDelta = new Vector2(Mathf.Max(0.01f, (_plan.FuelW - 4f * _plan.K) * _fuelFraction) * _s, (_plan.FuelH - 4f * _plan.K) * _s);
            r.anchoredPosition = P(DockingPlan.Width * 0.5f - _plan.FuelW * 0.5f + 2f * _plan.K, _plan.FuelCy);
            _fuelFill.color = _fuelFraction > 0.3f ? Gold : Coral;
        }

        /// <summary>La barra de tiempo del Reto (llena = todo el tiempo; baja de verde a coral): fina, bajo el marcador.</summary>
        private void SetTimer(float fraction)
        {
            _timerFraction = Mathf.Clamp01(fraction);
            var r = _timerFill.rectTransform;
            r.sizeDelta = new Vector2(Mathf.Max(0.01f, (DockingPlan.Width - 24f) * _timerFraction) * _s, 5f * _s);
            r.anchoredPosition = P(12f, 71f);
            _timerFill.color = _timerFraction > 0.5f ? Color.Lerp(Gold, Lime, (_timerFraction - 0.5f) * 2f) : Color.Lerp(Coral, Gold, _timerFraction * 2f);
        }

        private void UpdateHud()
        {
            if (_guided) return;
            _hud.SetLevel(_tt != null ? _tt.Level : _dda.PresentedLevel);
            if (!_guided) _hud.SetPromptFit(null, DockingContract.Rule, DockingContract.RuleShort);
            UpdateHudExtras();
        }

        /// <summary>El chip de avance (módulo + cuántos van a bordo) y el chip común de «seguidos» (Tarea 144: ya no hay contador ni chip propios).</summary>
        private void UpdateHudExtras()
        {
            if (_tt != null) { if (!_guided) _tt.UpdateHud(_hud, _endsAt > 0f ? _endsAt - Now : TimeTrial.TotalSeconds); }          // Contrarreloj: «N aciertos» y el tiempo que queda
            else if (!_guided && _moduleNo > 0) _hud.SetInfoFit(DockingContract.InfoChips(_moduleNo, Endless ? 0 : _trialsTotal, _docked));
            _hud.SetStreak(_guided ? 0 : _streak);
        }

        // ------------------------------------------------------------------ la fila de luces de avance

        /// <summary>Cuánto se queda a la vista la fila completa tras el 8.º módulo y cada cuánto se pasa la onda lima de una luz a la siguiente; lo que dura el halo de la última luz.</summary>
        private const float RingHoldSeconds = 0.9f, RingWaveStep = 0.04f, LightHaloSeconds = 0.45f;

        /// <summary>
        /// La fila de 8 luces: una de oro por cada módulo acoplado de este anillo (el avance de la partida); al 8.º, la fila completa se pone lima de izquierda a derecha (una onda de 40 ms por luz; con «quitar animaciones», de golpe), se queda un momento y empieza de cero; una medalla lima cuenta los anillos
        /// completos. La luz recién encendida lleva un halo suave (450 ms; no con «quitar animaciones»).
        /// </summary>
        private void RefreshLights(float now)
        {
            if (_lights[0] == null) return;
            int rings = _stationShown / SlotsPerRing, inRing = _stationShown % SlotsPerRing;
            bool completed = _stationShown > 0 && inRing == 0;
            float since = now - _landAt;
            bool hold = completed && since >= 0f && since < RingHoldSeconds;
            int lit = hold ? Lights : inRing;
            int lima = hold ? (Motion.Decorative ? Mathf.Min(Lights, Mathf.FloorToInt(since / RingWaveStep) + 1) : Lights) : 0;
            for (int i = 0; i < Lights; i++)
            {
                int state = i < lima ? 2 : i < lit ? 1 : 0;
                var sprite = LightSprite(state, out var tint);
                if (_lights[i].sprite != sprite) _lights[i].sprite = sprite;
                if (_lights[i].color != tint) _lights[i].color = tint;
            }
            bool medal = rings > 0;
            if (medal != _medal.gameObject.activeSelf) { _medal.gameObject.SetActive(medal); _medalText.gameObject.SetActive(medal); }
            if (medal)
            {
                string label = rings.ToString();
                if (_medalText.text != label) _medalText.text = label;
                if (_medal.color != (Art("luz_lima") != null ? Color.white : Lime)) _medal.color = Art("luz_lima") != null ? Color.white : Lime;
            }
            bool halo = Motion.Decorative && _stationShown > 0 && since >= 0f && since < LightHaloSeconds && _phase != Phase.Done;
            if (halo != _lightGlow.gameObject.activeSelf) _lightGlow.gameObject.SetActive(halo);
            if (halo)
            {
                int newest = inRing == 0 ? Lights - 1 : inRing - 1;
                float k = since / LightHaloSeconds;
                SetRect(_lightGlow.rectTransform, DockingPlan.LightX(newest), _plan.LightsY, 46f, 46f);
                _lightGlow.color = new Color(1f, 0.82f, 0.34f, 0.7f * (1f - k));
            }
        }

    }
}
