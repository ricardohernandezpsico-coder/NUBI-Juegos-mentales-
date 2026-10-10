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
        private Sprite _bodySprite, _edgeSprite, _holeSprite;
        private int _moduleNo;
        private float _fuelFraction = 1f, _timerFraction = 1f;
        private float _noticeAt = -10f, _noticeLife = 1.3f, _slotFlashAt = -10f, _endAt = -10f, _confettiAt, _endBarMaxH = 80f;
        private int _slotFlashIndex = -1;
        private bool _noticeBig, _confettiOn;
        private int[] _curve = { -1, -1, -1, -1, -1 };
        private readonly Vector2[] _confettiPos = new Vector2[ConfettiPool], _confettiVel = new Vector2[ConfettiPool];
        private readonly float[] _confettiLife = new float[ConfettiPool];
        private Image _timerTrack, _timerFill;

        // ------------------------------------------------------------------ reposo y módulo

        /// <summary>El estado de reposo: sin módulo, sin aviso, sin barra, puerto quieto, estación vacía y sin pantalla final.</summary>
        private void ResetViews()
        {
            ReleasePiece();
            _trial = null;
            _moduleRoot.gameObject.SetActive(false);
            _portEdge.gameObject.SetActive(false);
            _portHole.gameObject.SetActive(false);
            _portState = PortState.Idle;
            _shownAnswer = NoAnswer;
            SetNotice("", Lime, false);
            SetFuelVisible(false);
            _hint.gameObject.SetActive(false);
            _endLayer.gameObject.SetActive(false);
            foreach (var c in _confetti) c.gameObject.SetActive(false);
            _confettiOn = false;
            _slotFlash.gameObject.SetActive(false);
            _slotFlashIndex = -1;
            _hud.Rect.gameObject.SetActive(true);
            _hudLayer.gameObject.SetActive(true);
            _portLayer.gameObject.SetActive(true);
            _moduleLayer.gameObject.SetActive(true);
            _buttonLayer.gameObject.SetActive(true);
            _stationShown = 0;
            _moduleNo = 0;
            RefreshStation();
            RefreshButtons(Now);
            RefreshPort(Now);
            UpdateHudExtras();
        }

        /// <summary>Hornea el módulo y el hueco del puerto de un intento (la pieza derecha) y los deja en su lugar; el módulo todavía está escondido (llega en <see cref="DoArrive"/>).</summary>
        private void BeginTrial(DockingTrial trial)
        {
            _trial = trial;
            _moduleNo = _trials + 1;
            ReleasePiece();
            DockingSprites.BakePiece(trial.Shape, out _bodySprite, out _edgeSprite, out _holeSprite);
            _moduleBody.sprite = _bodySprite;
            _moduleShadow.sprite = _bodySprite;
            _portEdge.sprite = _edgeSprite;
            _portHole.sprite = _holeSprite;
            _portEdge.gameObject.SetActive(true);
            _portHole.gameObject.SetActive(true);
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

        /// <summary>Suelta las texturas de la pieza anterior (cada módulo hornea las suyas).</summary>
        private void ReleasePiece()
        {
            if (_moduleBody != null) _moduleBody.sprite = null;
            if (_moduleShadow != null) _moduleShadow.sprite = null;
            if (_portEdge != null) _portEdge.sprite = null;
            if (_portHole != null) _portHole.sprite = null;
            DockingSprites.Release(_bodySprite);
            DockingSprites.Release(_edgeSprite);
            DockingSprites.Release(_holeSprite);
            _bodySprite = _edgeSprite = _holeSprite = null;
        }

        private void HideModule()
        {
            if (_moduleRoot != null) _moduleRoot.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ la pose del módulo en cada momento

        private static float EaseInOut(float k) => k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) / 2f;

        /// <summary>
        /// Dónde está el módulo y cómo se ve, según lo que pasa: llegando (baja desde arriba, 420 ms), esperando, o en la revelación (giro, vuelta, bajada, vuelo o fuga). Con «quitar animaciones» cada paso cambia de golpe al empezar su ventana de tiempo, como en el boceto.
        /// </summary>
        private void ComputePose(float now, out float x, out float y, out float ang, out float fx, out float scale, out float alpha)
        {
            x = _plan.ModuleCx;
            y = _plan.ModuleCy;
            ang = _trial != null ? _trial.AngleDeg : 0f;
            fx = _trial != null && _trial.Mirrored ? -1f : 1f;
            scale = 1f;
            alpha = 1f;
            var port = PortState.Idle;
            bool dec = Motion.Decorative;
            if (_trial == null) { _portState = port; return; }
            if (_phase == Phase.Arrive)
            {
                float k = dec ? UiFx.EaseOutCubic(Mathf.Clamp01((now - _arriveAt) / (DockingContract.ArriveMs / 1000f))) : 1f;
                y = 250f + (_plan.ModuleCy - 250f) * k;
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
                y = _plan.ModuleCy + (_plan.PortCy + 6f * _plan.K - _plan.ModuleCy) * kD;
                if (_revealDemo)
                {
                    // la demostración del tutorial: gira, se da vuelta y baja; calza (el borde se pone lima) y se apaga sin sumarse a la estación
                    if (t >= tA + RevealDown) port = PortState.Ok;
                    float kD2 = Mathf.Clamp01((t - tA - RevealDown - 0.55f) / 0.3f);
                    alpha = 1f - kD2;
                }
                else if (_shownCorrect)
                {
                    if (t >= tA + RevealDown) port = PortState.Ok;
                    float tF = tA + RevealDown + RevealHold, kF2 = Mathf.Clamp01((t - tF) / RevealFly);
                    if (t >= tF)
                    {
                        var (tx, ty) = SlotTarget(_stationShown);
                        float e = dec ? EaseInOut(kF2) : 1f;
                        x += (tx - x) * e;
                        y += (ty - y) * e;
                        scale = 1f - 0.78f * e;
                        if (kF2 >= 1f) alpha = 0f;
                    }
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

        /// <summary>Dónde cae el módulo número <paramref name="index"/> de la estación (dp).</summary>
        private (float x, float y) SlotTarget(int index)
        {
            var (first, _) = DockingPlan.VisibleRange(index);
            var (ring, slot) = DockingContract.SlotOf(index);
            var p = DockingPlan.SlotPoint(DockingPlan.RingY(ring - first), slot);
            return (p.x, p.y);
        }

        private void ApplyModulePose()
        {
            if (_trial == null || _moduleRoot == null || !_moduleRoot.gameObject.activeSelf) return;
            ComputePose(Now, out float x, out float y, out float ang, out float fx, out float scale, out float alpha);
            _moduleRoot.anchoredPosition = P(x, y);
            _moduleRoot.localScale = Vector3.one * Mathf.Max(0.001f, scale);
            _moduleGroup.alpha = alpha;
            float th = -ang;                                                           // en Unity el ángulo positivo gira hacia la izquierda; en el boceto, hacia la derecha
            _bodyRect.localRotation = _shadowRect.localRotation = Quaternion.Euler(0f, 0f, th);
            _bodyRect.localScale = _shadowRect.localScale = new Vector3(Mathf.Abs(fx) < 0.001f ? 0.001f : fx, 1f, 1f);
            // la luz y el brillo van dentro del cuerpo (recortados por su silueta) y se contra-giran: quedan FIJOS en la pantalla
            float sgn = fx >= 0f ? 1f : -1f, afx = Mathf.Max(0.05f, Mathf.Abs(fx)) * sgn;
            _lightRect.localScale = new Vector3(sgn, 1f, 1f);
            _lightRect.localRotation = Quaternion.Euler(0f, 0f, sgn > 0f ? -th : th);
            _spotRect.localScale = new Vector3(sgn, 1f, 1f);
            const float spotRho = 0.45f * Mathf.Rad2Deg;                               // el brillo, inclinado 0,45 rad como en el boceto
            _spotRect.localRotation = Quaternion.Euler(0f, 0f, sgn > 0f ? spotRho - th : th - spotRho);
            float rad = ModuleRadius();
            var d = new Vector2(-rad * 0.35f, rad * 0.5f);                             // dónde cae el brillo en la pantalla (arriba a la izquierda del módulo)
            float c = Mathf.Cos(-th * Mathf.Deg2Rad), s = Mathf.Sin(-th * Mathf.Deg2Rad);
            var p = new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
            _spotRect.anchoredPosition = new Vector2(p.x / afx, p.y) * _s;
        }

        // ------------------------------------------------------------------ un cuadro

        private void Animate(float now, float dt)
        {
            if (_phase == Phase.Arrive || _phase == Phase.Decide || _phase == Phase.Reveal) ApplyModulePose();
            else _portState = _phase == Phase.Idle ? PortState.Idle : _portState;
            RefreshPort(now);
            RefreshButtons(now);
            AnimateNotice(now);
            AnimateSlotFlash(now);
            if (_phase == Phase.Done) AnimateEnd(now, dt);
        }

        /// <summary>El borde del hueco (teal tenue, lima al encajar, celeste al «mostrar») y los chevrones (lima: al encajar parpadean; con «quitar animaciones», fijos).</summary>
        private void RefreshPort(float now)
        {
            if (_portEdge == null) return;
            _portEdge.color = _portState == PortState.Ok ? Lime : _portState == PortState.Show ? Cyan : PortEdgeIdle;
            for (int i = 0; i < ChevronCount; i++)
            {
                bool on = _portState == PortState.Ok && (!Motion.Decorative || (Mathf.FloorToInt(now / 0.09f) + i) % 2 == 0);
                _chevrons[i].color = on ? Lime : new Color(Lime.r, Lime.g, Lime.b, 0.35f);
            }
        }

        /// <summary>Los botones: apagados (45 %) mientras llega el módulo, vivos al decidir y en la revelación; se hunden 3 dp al tocarlos; y el tocado muestra ✓ o ✗ en la revelación.</summary>
        private void RefreshButtons(float now)
        {
            bool live = _phase == Phase.Decide || _phase == Phase.Reveal;
            for (int i = 0; i < 2; i++)
            {
                var v = _buttons[i];
                v.Group.alpha = live ? 1f : 0.45f;
                float press = now - _pressAt[i] < 0.14f && now >= _pressAt[i] ? 3f : 0f;
                var (cx, cy) = _plan.ButtonCenter(i);
                v.Root.anchoredPosition = P(cx, cy + press);
                bool badge = _phase == Phase.Reveal && _shownAnswer == i;
                v.Badge.gameObject.SetActive(badge);
                if (badge) v.Badge.sprite = _shownCorrect ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            }
        }

        // ------------------------------------------------------------------ el aviso (la franja de y 246-278: NUNCA sobre el módulo, el puerto ni los botones)

        private void SetNotice(string text, Color color, bool big)
        {
            bool has = !string.IsNullOrEmpty(text);
            _noticeText.text = text ?? "";
            _noticeText.color = color;
            _noticeBig = big;
            _noticeAt = Now;
            _noticeLife = big ? 1.7f : 1.3f;
            SetNoticeVisible(has);
            if (has) LayoutNotice();
        }

        private void SetNoticeVisible(bool on)
        {
            _noticeText.gameObject.SetActive(on);
            _noticeShadow.gameObject.SetActive(on);
            _noticeBorder.gameObject.SetActive(on);
            _noticePill.gameObject.SetActive(on);
        }

        /// <summary>Acomoda la píldora al texto (17 dp, 18 en «¡Anillo completo!»; si no cabe en el ancho, achica hasta 14 dp).</summary>
        private void LayoutNotice()
        {
            float cx = DockingPlan.Width * 0.5f, cy = DockingPlan.StripCenter;
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
            SetRect(_noticePill.rectTransform, cx, cy, w, 32f);
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
            _noticePill.color = new Color(NoticeFill.r, NoticeFill.g, NoticeFill.b, a);
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

        // ------------------------------------------------------------------ la estación

        /// <summary>Dibuja la estación con los módulos acoplados: los anillos que se ven (los 5 más nuevos), sus casilleros (ocupados u apagados), la torre y el color lima de los anillos completos.</summary>
        private void RefreshStation()
        {
            if (_tower == null) return;
            var (first, count) = DockingPlan.VisibleRange(_stationShown);
            float top = DockingPlan.StationBase - 40f - (count - 1) * DockingPlan.StationStep, bottom = DockingPlan.StationBase + 20f, cx = DockingPlan.StationCx;
            SetRect(_tower.rectTransform, cx, (top + bottom) * 0.5f, 40f, bottom - top);
            SetRect(_towerInk.rectTransform, cx, (top + bottom) * 0.5f, 46f, bottom - top + 6f);
            SetRect(_towerShadow.rectTransform, cx, (top + bottom) * 0.5f + 4f, 46f, bottom - top + 6f);
            for (int vr = 0; vr < Rings; vr++)
            {
                bool shown = vr < count;
                int ring = first + vr;
                bool full = _stationShown >= (ring + 1) * SlotsPerRing;
                _ringInk[vr].gameObject.SetActive(shown);
                _ringColor[vr].gameObject.SetActive(shown);
                _ringColor[vr].color = full ? Lime : RingViolet;
                float ry = DockingPlan.RingY(vr);
                SetRect(_ringInk[vr].rectTransform, cx, ry, DockingSprites.RingSpriteW, DockingSprites.RingSpriteH);
                SetRect(_ringColor[vr].rectTransform, cx, ry, DockingSprites.RingSpriteW, DockingSprites.RingSpriteH);
                for (int k = 0; k < SlotsPerRing; k++)
                {
                    bool on = shown && ring * SlotsPerRing + k < _stationShown;
                    var slot = _slots[vr, k];
                    var dot = _slotDots[vr, k];
                    slot.gameObject.SetActive(shown);
                    dot.gameObject.SetActive(on);
                    if (!shown) continue;
                    var p = DockingPlan.SlotPoint(ry, k);
                    float sc = 0.82f + 0.18f * (p.depth + 1f) * 0.5f;
                    if (_baked) slot.sprite = on ? (full ? DockingSprites.SlotLime() : DockingSprites.SlotSky()) : DockingSprites.SlotEmpty();
                    SetRect(slot.rectTransform, p.x, p.y, DockingSprites.SlotSide * sc, DockingSprites.SlotSide * sc);
                    SetRect(dot.rectTransform, p.x, p.y, 6.4f * sc, 6.4f * sc);
                }
            }
        }

        /// <summary>El aro blanco que se abre en el casillero recién ocupado (450 ms; no con «quitar animaciones»).</summary>
        private void AnimateSlotFlash(float now)
        {
            float k = (now - _slotFlashAt) / 0.45f;
            bool on = Motion.Decorative && _slotFlashIndex >= 0 && k >= 0f && k < 1f;
            _slotFlash.gameObject.SetActive(on);
            if (!on) return;
            var (first, _) = DockingPlan.VisibleRange(_stationShown - 1);
            var (ring, slot) = DockingContract.SlotOf(_slotFlashIndex);
            var p = DockingPlan.SlotPoint(DockingPlan.RingY(ring - first), slot);
            float d = (10f + 14f * k) * 2f;
            SetRect(_slotFlash.rectTransform, p.x, p.y, d, d);
            _slotFlash.color = new Color(1f, 1f, 1f, 1f - k);
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
