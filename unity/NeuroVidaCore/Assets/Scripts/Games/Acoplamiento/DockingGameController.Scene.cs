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
        private float _noticeAt = -10f, _noticeLife = 1.3f, _landAt = -10f, _endAt = -10f, _confettiAt, _endBarMaxH = 80f;
        private bool _noticeBig, _confettiOn;
        private int[] _curve = { -1, -1, -1, -1, -1 };
        private readonly Vector2[] _confettiPos = new Vector2[ConfettiPool], _confettiVel = new Vector2[ConfettiPool];
        private readonly float[] _confettiLife = new float[ConfettiPool];
        private Image _timerTrack, _timerFill;

        /// <summary>Las piezas que acopló la persona en la partida (la práctica del tutorial no cuenta): con ellas se arma la escultura de la pantalla final.</summary>
        private readonly List<Cell[]> _dockedShapes = new List<Cell[]>();

        // ------------------------------------------------------------------ reposo y módulo

        /// <summary>El estado de reposo: sin módulo, sin aviso, sin barra, puerto quieto, fila de luces según lo acoplado y sin pantalla final.</summary>
        private void ResetViews()
        {
            DiscardPrebake();
            ReleasePiece();
            ReleaseSculpture();
            _trial = null;
            _moduleRoot.gameObject.SetActive(false);
            _portEdge.gameObject.SetActive(false);
            _portHole.gameObject.SetActive(false);
            _portMark.gameObject.SetActive(false);
            _portState = PortState.Idle;
            _shownAnswer = NoAnswer;
            SetNotice("", Lime, false);
            SetFuelVisible(false);
            _hint.gameObject.SetActive(false);
            _endLayer.gameObject.SetActive(false);
            foreach (var c in _confetti) c.gameObject.SetActive(false);
            _confettiOn = false;
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
            SetNotice("", Lime, false);
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
                while (it.MoveNext())
                {
                    yield return null;
                    if (_trial != trial) yield break;
                }
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
            var trial = DockingContract.NextTrial(level, _rng);
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
            var trial = NewTrial(_dda.PresentedLevel);
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
            bool ra = true, rb = true;
            while (ra || rb)
            {
                if (ra) ra = a.MoveNext();
                if (rb) rb = b.MoveNext();
                yield return null;
                if (_preTrial != trial) yield break;                                             // se descartó (el tutorial desde la pausa, un reinicio)
            }
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
            AnimateNotice(now);
            RefreshLights(now);
            if (_phase == Phase.Done) AnimateEnd(now, dt);
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
            bool live = _phase == Phase.Decide || _phase == Phase.Reveal;
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

        // ------------------------------------------------------------------ el aviso (la franja de y 118-150: NUNCA sobre el módulo, el puerto ni los botones)

        private void SetNotice(string text, Color color, bool big)
        {
            bool has = !string.IsNullOrEmpty(text);
            _noticeText.text = text ?? "";
            _noticeText.color = _noticeSlab != null ? Ink : color;
            if (_noticeSlab != null) PlasticSlab.Set(_noticeSlab, SlabSize.Chip, color == Gold ? SlabTone.Cream : SlabTone.Sun);          // dorada si acertó (o completó el anillo), crema si no
            _noticeBig = big;
            _noticeAt = Now;
            _noticeLife = big ? 1.7f : 1.3f;
            SetNoticeVisible(has);
            if (has) LayoutNotice();
        }

        private void SetNoticeVisible(bool on)
        {
            _noticeText.gameObject.SetActive(on);
            _noticePill.gameObject.SetActive(on);
            _noticeShadow.gameObject.SetActive(on && _noticeSlab == null);
            _noticeBorder.gameObject.SetActive(on && _noticeSlab == null);
        }

        /// <summary>Acomoda el aviso al texto (17 dp, 18 en «¡Anillo completo!»; si no cabe en el ancho, achica hasta 14 dp).</summary>
        private void LayoutNotice()
        {
            float cx = DockingPlan.Width * 0.5f, cy = _plan.StripCy;
            float fs = _noticeBig ? 18f : 17f;
            _noticeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            do
            {
                _noticeText.fontSize = Mathf.RoundToInt(fs * _s);
                if (_noticeText.preferredWidth / _s <= DockingPlan.Width - 56f - 30f) break;
                fs -= 1f;
            } while (fs > 14f);
            _noticeText.fontSize = Mathf.RoundToInt(Mathf.Max(14f, fs) * _s);
            float w = Mathf.Min(DockingPlan.Width - 56f, _noticeText.preferredWidth / _s + 30f);
            SetRect(_noticeText.rectTransform, cx, cy + 1f, w, 30f);
            SetRect(_noticePill.rectTransform, cx, cy, w, _noticeSlab != null ? 30f : 32f);
            SetRect(_noticeBorder.rectTransform, cx, cy, w + 6f, 38f);
            SetRect(_noticeShadow.rectTransform, cx, cy + 4f, w + 6f, 38f);
        }

        private void AnimateNotice(float now)
        {
            if (!_noticeText.gameObject.activeSelf) return;
            float k = (now - _noticeAt) / _noticeLife;
            if (k >= 1f) { SetNoticeVisible(false); return; }
            float a = Mathf.Clamp01(Mathf.Min(1f, k * 7f, (1f - k) * 3f));
            _noticeText.color = new Color(_noticeText.color.r, _noticeText.color.g, _noticeText.color.b, a);
            if (_noticeSlab != null) _noticeSlab.color = new Color(1f, 1f, 1f, a);
            _noticePill.color = _noticeSlab != null ? _noticePill.color : new Color(NoticeFill.r, NoticeFill.g, NoticeFill.b, a);
            _noticeBorder.color = new Color(Ink.r, Ink.g, Ink.b, a);
            _noticeShadow.color = new Color(Shadow.r, Shadow.g, Shadow.b, Shadow.a * a);
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
            _hud.SetLevel(_dda.PresentedLevel);
            _hud.SetInfo(DockingContract.RoundChip(_moduleNo, Endless ? 0 : _trialsTotal));
            if (!_guided) _hud.SetPromptFit(null, DockingContract.Rule, DockingContract.RuleShort);
            UpdateHudExtras();
        }

        private void UpdateHudExtras()
        {
            _hudCount.text = _docked.ToString();
            bool hot = !_guided && _streak >= 2;
            _streakBg.gameObject.SetActive(hot);
            _streakText.gameObject.SetActive(hot);
            if (hot) _streakText.text = DockingContract.StreakChip(_streak);
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

        // ------------------------------------------------------------------ la pantalla final

        /// <summary>Las 5 columnas de «Tu curva de giro» (el tiempo medio de los aciertos por ángulo, en la escala del máximo de la curva y 1,5 s como mínimo).</summary>
        private void PlaceCurve()
        {
            float baseY = EndY(582f);
            int maxv = 1500;
            foreach (int v in _curve) maxv = Mathf.Max(maxv, v);
            for (int i = 0; i < CurveBars; i++)
            {
                float x = 56f + i * 62f;
                int v = _curve[i];
                bool has = v >= 0;
                _curveBar[i].gameObject.SetActive(has);
                _curveBarInk[i].gameObject.SetActive(has);
                _curveValue[i].text = DockingContract.CurveValue(v);
                _curveValue[i].color = has ? Color.white : Dim;
                if (has)
                {
                    float h = Mathf.Max(8f, _endBarMaxH * v / maxv);
                    SetRect(_curveBar[i].rectTransform, x, baseY - h * 0.5f, 28f, h);
                    SetRect(_curveBarInk[i].rectTransform, x, baseY - h * 0.5f, 33.2f, h + 5.2f);
                    SetRect(_curveValue[i].rectTransform, x, baseY - h - 12f, 56f, 20f);
                }
                else SetRect(_curveValue[i].rectTransform, x, baseY - 12f, 56f, 20f);
            }
        }

        // ------------------------------------------------------------------ la escultura de «tu estación»

        private readonly List<Sprite> _sculptSprites = new List<Sprite>();
        private Sprite _coreSprite, _trackBackSprite, _trackFrontSprite;
        private float _trackWDp, _trackHDp, _coreHDp, _coreTopDp, _coreBotDp;
        private readonly float[,] _sculptSide = new float[SculptureRings, SlotsPerRing];

        /// <summary>Suelta las texturas de la escultura (se hornean al terminar la partida).</summary>
        private void ReleaseSculpture()
        {
            foreach (var s in _sculptSprites) DockingArt.Release(s);
            _sculptSprites.Clear();
            DockingArt.Release(_coreSprite);
            DockingArt.Release(_trackBackSprite);
            DockingArt.Release(_trackFrontSprite);
            _coreSprite = _trackBackSprite = _trackFrontSprite = null;
            if (_sculptCore == null) return;
            _sculptCore.sprite = null;
            for (int r = 0; r < SculptureRings; r++)
            {
                _trackBack[r].sprite = null;
                _trackFront[r].sprite = null;
                for (int k = 0; k < SlotsPerRing; k++) _sculptPieces[r, k].sprite = null;
            }
        }

        /// <summary>
        /// Arma la escultura con las piezas que acopló la persona: hasta 3 anillos de 8 (los más nuevos) alrededor de un núcleo, con la misma lámpara y el mismo celeste de la pieza de juego; las pistas de los anillos completos van lima y la del que sigue en curso, lavanda. Las piezas se hornean de a pocas por cuadro
        /// (24 piezas chicas de 10 dp por bloque) mientras la pantalla final entra.
        /// </summary>
        private IEnumerator SculptureRoutine()
        {
            ReleaseSculpture();
            var (first, count) = DockingPlan.VisibleRange(_docked);
            const float pxDp = 3f;
            _trackBackSprite = DockingArt.TrackSprite(DockingPlan.SculptureRx, DockingPlan.SculptureRy, 3.4f, pxDp, true, out _trackWDp, out _trackHDp);
            _trackFrontSprite = DockingArt.TrackSprite(DockingPlan.SculptureRx, DockingPlan.SculptureRy, 3.4f, pxDp, false, out _, out _);
            _coreTopDp = DockingPlan.SculptureRingY[count - 1] - DockingPlan.SculptureRy - 10f;
            _coreBotDp = DockingPlan.SculptureRingY[0] + DockingPlan.SculptureRy + 12f;
            _coreSprite = DockingArt.CoreSprite(46f, (_coreBotDp - _coreTopDp) - 18f, 9f, pxDp, out _coreHDp);
            _sculptCore.sprite = _coreSprite;
            for (int r = 0; r < SculptureRings; r++)
            {
                bool on = r < count;
                bool full = on && (first + r + 1) * SlotsPerRing <= _docked;
                var tint = full ? Lime : RingViolet;
                _trackBack[r].sprite = on ? _trackBackSprite : null; _trackFront[r].sprite = on ? _trackFrontSprite : null;
                _trackBack[r].color = _trackFront[r].color = tint;
                _trackBack[r].gameObject.SetActive(on); _trackFront[r].gameObject.SetActive(on);
            }
            LayoutSculpture();
            int baked = 0;
            for (int r = 0; r < count; r++)
                for (int k = 0; k < SlotsPerRing; k++)
                {
                    int idx = (first + r) * SlotsPerRing + k;
                    if (idx >= _docked) { _sculptPieces[r, k].gameObject.SetActive(false); continue; }
                    var shape = idx < _dockedShapes.Count ? _dockedShapes[idx] : PracticeShape();
                    var sprite = DockingArt.BakeSculpturePiece(shape, 30, 4, 3 + idx, out _sculptSide[r, k]);
                    _sculptSprites.Add(sprite);
                    _sculptPieces[r, k].sprite = sprite;
                    _sculptPieces[r, k].color = DockingPlan.IsFront(k) ? Color.white : new Color(0.84f, 0.88f, 0.98f, 1f);        // las de atrás van más oscuras y más chicas
                    _sculptPieces[r, k].gameObject.SetActive(true);
                    PlaceSculpturePiece(r, k);
                    if (++baked % 4 == 0) yield return null;
                }
            for (int r = count; r < SculptureRings; r++)
                for (int k = 0; k < SlotsPerRing; k++) _sculptPieces[r, k].gameObject.SetActive(false);
        }

        private void PlaceSculpturePiece(int r, int k)
        {
            var p = DockingPlan.SlotPoint(DockingPlan.SculptureRingY[r], k);
            float side = _sculptSide[r, k] * DockingPlan.SculptureBlock * (DockingPlan.IsFront(k) ? 1f : 0.88f);
            SetRect(_sculptPieces[r, k].rectTransform, p.x, p.y, side, side);
        }

        private void LayoutSculpture()
        {
            if (_sculptCore == null) return;
            if (_coreSprite != null) SetRect(_sculptCore.rectTransform, DockingPlan.SculptureCx, (_coreTopDp + _coreBotDp) * 0.5f, 46f, _coreHDp);
            for (int r = 0; r < SculptureRings; r++)
            {
                if (_trackBackSprite != null)
                {
                    SetRect(_trackBack[r].rectTransform, DockingPlan.SculptureCx, DockingPlan.SculptureRingY[r], _trackWDp, _trackHDp);
                    SetRect(_trackFront[r].rectTransform, DockingPlan.SculptureCx, DockingPlan.SculptureRingY[r], _trackWDp, _trackHDp);
                }
                for (int k = 0; k < SlotsPerRing; k++) if (_sculptPieces[r, k].sprite != null) PlaceSculpturePiece(r, k);
            }
        }

        private void ShowEnd(int record, bool broke, int speed, int[] curve)
        {
            _hud.Rect.gameObject.SetActive(false);
            _hudLayer.gameObject.SetActive(false);
            _portLayer.gameObject.SetActive(false);
            _moduleLayer.gameObject.SetActive(false);
            _buttonLayer.gameObject.SetActive(false);
            _timerTrack.gameObject.SetActive(false);
            _timerFill.gameObject.SetActive(false);
            _endTitle.text = DockingContract.EndTitle(_docked);
            _endValue[0].text = DockingContract.HitsValue(_correct, _trials);
            _endValue[1].text = DockingContract.SpeedValue(speed);
            _endValue[2].text = _rings.ToString();
            _endValue[3].text = DockingContract.StreakValue(_bestStreak);
            _endRecord.text = broke ? DockingContract.NewRecord : DockingContract.RecordLine(record);
            _endRecordBg.color = broke ? Gold : new Color(Gold.r, Gold.g, Gold.b, 0f);
            _endRecord.color = broke ? Ink : Lavender;
            _curve = curve != null && curve.Length == CurveBars ? curve : new[] { -1, -1, -1, -1, -1 };
            _endAt = Now;
            _endLayer.gameObject.SetActive(true);
            Layout();
            StartCoroutine(SculptureRoutine());
            if (Motion.Decorative)
            {
                _confettiOn = true;
                _confettiAt = Now;
                var rng = new System.Random();
                var cols = new[] { Lime, Cyan, Gold, DockingSprites.GrapeColor, Coral };
                for (int i = 0; i < ConfettiPool; i++)
                {
                    _confettiPos[i] = new Vector2(DockingPlan.Width * 0.5f + ((float)rng.NextDouble() - 0.5f) * 120f, 170f);
                    _confettiVel[i] = new Vector2(((float)rng.NextDouble() - 0.5f) * 260f, -120f - (float)rng.NextDouble() * 220f);
                    _confettiLife[i] = 1.6f + (float)rng.NextDouble() * 0.7f;
                    _confetti[i].color = cols[i % cols.Length];
                    float sz = 3f + (float)rng.NextDouble() * 3f;
                    _confetti[i].rectTransform.sizeDelta = new Vector2(sz * 2f * _s, sz * 2f * _s);
                    _confetti[i].gameObject.SetActive(true);
                }
            }
        }

        private void AnimateEnd(float now, float dt)
        {
            if (!_confettiOn || dt <= 0f) return;
            float age = now - _confettiAt;
            for (int i = 0; i < ConfettiPool; i++)
            {
                float life = _confettiLife[i];
                bool on = age < life;
                _confetti[i].gameObject.SetActive(on);
                if (!on) continue;
                _confettiVel[i].y += 300f * dt;
                _confettiPos[i] += _confettiVel[i] * dt;
                SetRect(_confetti[i].rectTransform, _confettiPos[i].x, _confettiPos[i].y, _confetti[i].rectTransform.sizeDelta.x / _s, _confetti[i].rectTransform.sizeDelta.y / _s);
                var c = _confetti[i].color;
                _confetti[i].color = new Color(c.r, c.g, c.b, 0.95f * (1f - age / life));
            }
        }
    }
}
