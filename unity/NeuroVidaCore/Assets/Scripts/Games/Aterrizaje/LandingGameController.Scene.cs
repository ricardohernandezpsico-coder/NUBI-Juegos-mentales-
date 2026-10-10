using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Aterrizaje
{
    public sealed partial class LandingGameController
    {
        private float _timerFraction = 1f, _shootAt = -100f, _shootX, _shootY, _domePopAt = -10f, _endAt = -10f, _confettiAt;
        private bool _confettiOn;
        private readonly Vector2[] _confettiPos = new Vector2[ConfettiPool], _confettiVel = new Vector2[ConfettiPool];
        private readonly float[] _confettiLife = new float[ConfettiPool];
        private System.Random _worldRng = new System.Random(5);

        /// <summary>Un hijo posicionado por su DESPLAZAMIENTO (dp) desde el centro del padre (x a la derecha, y hacia abajo).</summary>
        private void SetChild(RectTransform r, float dx, float dy, float w, float h)
        {
            r.sizeDelta = new Vector2(w * _s, h * _s);
            r.anchoredPosition = new Vector2(dx * _s, -dy * _s);
        }

        private static float EaseOutCubic(float k) => 1f - Mathf.Pow(1f - Mathf.Clamp01(k), 3f);

        // ------------------------------------------------------------------ reposo

        /// <summary>El estado de reposo: sin nave, sin guía, sin resultado, sin avisos, sin pantalla final y el marcador en cero.</summary>
        private void ResetViews()
        {
            _trial = null;
            _land = null;
            _landerRoot.gameObject.SetActive(false);
            _guideArrow.gameObject.SetActive(false);
            foreach (var d in _guideDot) d.gameObject.SetActive(false);
            ClearResultViews();
            _hint.gameObject.SetActive(false);
            _timerTrack.gameObject.SetActive(false);                                  // la barra del Reto solo se ve en la partida (GameLoop la prende después del tutorial y la cuenta regresiva)
            _timerFill.gameObject.SetActive(false);
            _endLayer.gameObject.SetActive(false);
            foreach (var c in _confetti) c.gameObject.SetActive(false);
            _confettiOn = false;
            _hud.Rect.gameObject.SetActive(true);
            _hudLayer.gameObject.SetActive(true);
            _rulerLayer.gameObject.SetActive(true);
            _guideLayer.gameObject.SetActive(true);
            _landerLayer.gameObject.SetActive(true);
            _noticeLayer.gameObject.SetActive(true);
            _shownHits = _shownDomes = 0;
            _missionSmall.gameObject.SetActive(true);
            _missionNumber.gameObject.SetActive(true);
            StopHover();
            RefreshHudExtras();
        }

        private void SetResultVisible(bool on)
        {
            _gapLine.gameObject.SetActive(on);
            _badge.gameObject.SetActive(on);
            _pole.gameObject.SetActive(on);
            _pennant.gameObject.SetActive(on);
            _flagShadow.gameObject.SetActive(on);
            _flagInk.gameObject.SetActive(on);
            _flagFill.gameObject.SetActive(on);
            _flagText.gameObject.SetActive(on);
        }

        /// <summary>Quita el tramo, la insignia, la bandera, el polvo y los avisos de un aterrizaje.</summary>
        private void ClearResultViews()
        {
            SetResultVisible(false);
            foreach (var d in _dust) d.gameObject.SetActive(false);
            foreach (var n in _notices)
            {
                n.Live = false;
                SetNoticeVisible(n, false);
            }
        }

        // ------------------------------------------------------------------ un cuadro

        private void Animate(float now, float dt)
        {
            AnimateWorld(now);
            AnimateLander(now);
            AnimateGuide();
            AnimateResult();
            AnimateNotices();
            AnimateHud(now);
            AnimateHover(Mathf.Max(GameClock.RealDeltaTime, 0f));
            if (_phase == Phase.Done) AnimateEnd(now, dt);
        }

        /// <summary>El cielo con vida: las estrellas titilan, de vez en cuando pasa una fugaz y un satélite cruza despacio; las dos cordilleras se desplazan (3 y 7 dp/s). Con «quitar animaciones» las estrellas quedan fijas y no hay fugaz, ni satélite, NI CORDILLERAS (quietas junto a la regla serían marcas).</summary>
        private void AnimateWorld(float now)
        {
            bool dec = Motion.Decorative;
            for (int i = 0; i < StarCount; i++)
            {
                float a = dec ? 0.35f + 0.5f * (0.5f + 0.5f * Mathf.Sin(now * 1000f / 700f + _starPhase[i])) : 0.6f;
                _stars[i].color = new Color(1f, 1f, 1f, a);
            }
            // la estrella fugaz (cada 9 s o más, 700 ms de recorrido)
            bool shooting = false;
            if (dec)
            {
                if (_shootAt < -50f || now - _shootAt > 9f)
                {
                    _shootAt = now + 2.5f + (float)_worldRng.NextDouble() * 5f;
                    _shootX = 40f + (float)_worldRng.NextDouble() * 200f;
                    _shootY = 170f + (float)_worldRng.NextDouble() * 120f;
                }
                float k = (now - _shootAt) / 0.7f;
                if (k > 0f && k < 1f)
                {
                    shooting = true;
                    float x = _shootX + 160f * k, y = _shootY + 60f * k;
                    SetRect(_shoot.rectTransform, x - 25f, y - 9.5f, 53.5f, 2.4f);
                    _shoot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -20.8f);
                    _shoot.color = new Color(1f, 1f, 1f, 0.9f * (1f - k));
                }
            }
            _shoot.gameObject.SetActive(shooting);
            // el satélite: cruza a 9 dp/s
            _satellite.gameObject.SetActive(dec);
            if (dec)
            {
                float sx = (9f * now) % (LandingPlan.Width + 60f) - 30f;
                SetRect(_satellite.rectTransform, sx, 236f + 12f * Mathf.Sin(sx / 90f), LandingSprites.SatelliteW, LandingSprites.SatelliteH);
            }
            // las cordilleras: detrás del borde de la luna; solo con animaciones
            _rangeFar.gameObject.SetActive(dec);
            _rangeNear.gameObject.SetActive(dec);
            if (dec)
            {
                SetRect(_rangeFar.rectTransform, LandingPlan.Width - (3f * now) % LandingPlan.Width, _plan.RulerY - 4f - LandingSprites.RangeH * 0.5f, LandingSprites.RangeW, LandingSprites.RangeH);
                SetRect(_rangeNear.rectTransform, LandingPlan.Width - (7f * now) % LandingPlan.Width, _plan.RulerY - LandingSprites.RangeH * 0.5f, LandingSprites.RangeW, LandingSprites.RangeH);
            }
        }

        /// <summary>La nave: su lugar, la inclinación leve hacia donde la llevan, la llama (solo con animaciones), las dos luces que parpadean y su aparición (un pequeño salto) arriba.</summary>
        private void AnimateLander(float now)
        {
            bool vis = _phase == Phase.Fall || _phase == Phase.Drop || _phase == Phase.Land;
            if (_landerRoot.gameObject.activeSelf != vis) _landerRoot.gameObject.SetActive(vis);
            if (!vis) return;
            bool dec = Motion.Decorative;
            bool flying = _phase == Phase.Fall || _phase == Phase.Drop;
            _landerRoot.sizeDelta = new Vector2(LandingPlan.LanderSide * _s, LandingPlan.LanderSide * _s);
            _landerRoot.anchoredPosition = P(_x, _y);
            float tilt = dec && flying ? Mathf.Clamp((_tx - _x) * -0.14f, -10f, 10f) : 0f;      // inclinación leve según hacia dónde se mueve
            float pop = dec ? 0.6f + 0.4f * Mathf.Clamp01(EaseOutBackLocal((now - _popAt) / 0.25f)) : 1f;
            _landerRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
            _landerRoot.localScale = new Vector3(pop, pop, 1f);
            SetChild(_landerBody.rectTransform, 0f, 0f, LandingPlan.LanderSide, LandingPlan.LanderSide);
            // la llama: bajo la base, solo mientras vuela y con animaciones
            bool flame = flying && dec;
            _flame.gameObject.SetActive(flame);
            if (flame)
            {
                float h = (30f + 6f * Mathf.Sin(now * 1000f / 50f)) * LandingPlan.LanderScale;
                SetChild(_flame.rectTransform, 0f, LandingPlan.LanderScale * 14f + h * 0.5f, 22f * LandingPlan.LanderScale, h);
            }
            // las luces: coral a la izquierda y lima a la derecha; parpadean (fijas con «quitar animaciones»)
            float blink = dec ? (Mathf.FloorToInt(now * 1000f / 400f) % 2 == 1 ? 1f : 0.3f) : 1f;
            SetChild(_lightL.rectTransform, -15f * LandingPlan.LanderScale, 4f * LandingPlan.LanderScale, 12f * LandingPlan.LanderScale, 12f * LandingPlan.LanderScale);
            SetChild(_lightR.rectTransform, 15f * LandingPlan.LanderScale, 4f * LandingPlan.LanderScale, 12f * LandingPlan.LanderScale, 12f * LandingPlan.LanderScale);
            _lightL.color = new Color(Coral.r, Coral.g, Coral.b, blink);
            _lightR.color = new Color(Lime.r, Lime.g, Lime.b, blink);
            // al terminar el resultado se desvanece (solo con animaciones)
            float fade = 1f;
            if (_phase == Phase.Land && dec) fade = 1f - Mathf.Clamp01((ResultT() - (_holdT - 0.3f)) / 0.3f);
            _landerGroup.alpha = fade;
        }

        private static float EaseOutBackLocal(float k)
        {
            k = Mathf.Clamp01(k);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
        }

        /// <summary>La guía de aterrizaje: puntos lima desde la nave hasta la regla y una flecha sobre la regla (solo mientras la nave baja).</summary>
        private void AnimateGuide()
        {
            bool flying = _phase == Phase.Fall || _phase == Phase.Drop;
            _guideArrow.gameObject.SetActive(flying);
            int n = 0;
            if (flying)
            {
                for (float yy = _y + 42f; yy < _plan.RulerY - 4f && n < GuideDots; yy += 12f)
                {
                    var d = _guideDot[n++];
                    d.gameObject.SetActive(true);
                    SetRect(d.rectTransform, _x, yy, 4.4f, 4.4f);
                }
                SetRect(_guideArrow.rectTransform, _x, _plan.RulerY - 6f, LandingSprites.ArrowW, LandingSprites.ArrowH);
            }
            for (int i = n; i < GuideDots; i++) _guideDot[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// El resultado, según el tiempo desde que la nave tocó la regla: el tramo entre ella y el lugar justo se dibuja entre los 300 y los 600 ms (lima con ✓ si fue justo, coral con aspa si no: NUNCA solo color) y la bandera sube 80 dp entre los 380 y los 800 ms con su número
        /// (en las sumas, el RESULTADO). Con «quitar animaciones» aparecen de una vez. Mostrar la verdad: la bandera siempre está en el lugar justo.
        /// </summary>
        private void AnimateResult()
        {
            if (_phase != Phase.Land || _land == null)
            {
                if (_gapLine.gameObject.activeSelf) SetResultVisible(false);
                for (int i = 0; i < DustCount; i++) if (_dust[i].gameObject.activeSelf) _dust[i].gameObject.SetActive(false);
                return;
            }
            bool dec = Motion.Decorative;
            float t = ResultT();
            float tx = LandingPlan.FracX(_land.TargetFrac), lx = _x, yy = _plan.RulerY + LandingPlan.RulerH * 0.5f;
            // el tramo y su insignia
            float k = dec ? Mathf.Clamp01((t - LandingContract.LineDelayMs / 1000f) / (LandingContract.LineMs / 1000f)) : 1f;
            _gapLine.gameObject.SetActive(k > 0f);
            if (k > 0f)
            {
                float reach = (tx - lx) * k;
                SetRect(_gapLine.rectTransform, lx + reach * 0.5f, yy, Mathf.Abs(reach) + 6f, 6f);
                _gapLine.color = new Color(_land.Hit ? Lime.r : Coral.r, _land.Hit ? Lime.g : Coral.g, _land.Hit ? Lime.b : Coral.b, k);
            }
            bool badge = k >= 1f;
            _badge.gameObject.SetActive(badge);
            if (badge)
            {
                _badge.sprite = LandingSprites.Badge(_land.Hit);
                SetRect(_badge.rectTransform, (lx + tx) * 0.5f, yy + 26f, LandingSprites.BadgeSide, LandingSprites.BadgeSide);
            }
            // la bandera: sube en el lugar justo
            float kf = dec ? Mathf.Clamp01((t - LandingContract.FlagDelayMs / 1000f) / (LandingContract.FlagMs / 1000f)) : 1f;
            float fh = LandingContract.FlagHeight * EaseOutCubic(kf);
            bool flag = kf > 0f;
            _pole.gameObject.SetActive(flag);
            bool top = flag && fh > 30f;
            _pennant.gameObject.SetActive(top);
            _flagShadow.gameObject.SetActive(top);
            _flagInk.gameObject.SetActive(top);
            _flagFill.gameObject.SetActive(top);
            _flagText.gameObject.SetActive(top);
            if (flag) SetRect(_pole.rectTransform, tx, _plan.RulerY - fh * 0.5f, 3f, fh);
            if (top)
            {
                SetRect(_pennant.rectTransform, tx + 15f, _plan.RulerY - fh + 9f, LandingSprites.PennantW, LandingSprites.PennantH);
                _flagText.text = _trial != null ? _trial.FlagText : "";
                _flagText.fontSize = Mathf.RoundToInt(18f * _s);
                float w = _flagText.preferredWidth / _s + 16f;
                float bx = Mathf.Max(10f + w * 0.5f, Mathf.Min(LandingPlan.Width - 10f - w * 0.5f, tx)), by = _plan.RulerY - fh - 22f;
                SetRect(_flagText.rectTransform, bx, by + 1f, w, 28f);
                SetRect(_flagFill.rectTransform, bx, by, w, 28f);
                SetRect(_flagInk.rectTransform, bx, by, w + 5.2f, 33.2f);
                SetRect(_flagShadow.rectTransform, bx, by + 3f, w + 5.2f, 33.2f);
            }
            AnimateDust(t);
        }

        // ------------------------------------------------------------------ polvo

        /// <summary>El polvo que levanta la nave al posarse: 14 motas (sin «quitar animaciones»).</summary>
        private void SpawnDust()
        {
            var rng = _worldRng;
            for (int i = 0; i < DustCount; i++)
            {
                _dustDx[i] = ((float)rng.NextDouble() - 0.5f) * 18f;
                _dustVel[i] = new Vector2(((float)rng.NextDouble() - 0.5f) * 90f, -20f - (float)rng.NextDouble() * 40f);
                _dustLife[i] = 0.7f + (float)rng.NextDouble() * 0.4f;
            }
        }

        private void AnimateDust(float t)
        {
            bool dec = Motion.Decorative;
            for (int i = 0; i < DustCount; i++)
            {
                float life = _dustLife[i];
                bool on = dec && life > 0f && t >= 0f && t < life;
                _dust[i].gameObject.SetActive(on);
                if (!on) continue;
                float k = t / life;
                float x = _x + _dustDx[i] + _dustVel[i].x * t, y = _plan.RulerY - 2f + _dustVel[i].y * t + 30f * t * t;
                float d = 2f * (3f + 5f * k);
                SetRect(_dust[i].rectTransform, x, y, d, d);
                _dust[i].color = new Color(214f / 255f, 209f / 255f, 242f / 255f, 0.6f * (1f - k));
            }
        }

        // ------------------------------------------------------------------ los avisos (en el cielo vacío, después de posarse)

        /// <summary>Un aviso (en la ranura 0: lo que salió; en la 1: la cúpula nueva), que nace en el instante <paramref name="at"/> del resultado (el de la tabla del diseño, no el cuadro en que se detectó: así las capturas que detienen el tiempo lo ven).</summary>
        private void SetNotice(int slot, string text, Color color, bool big, float life, float at)
        {
            var n = _notices[slot];
            n.Message = text ?? "";
            n.Color = color;
            n.Big = big;
            n.Life = life;
            n.At = at;
            n.Live = !string.IsNullOrEmpty(text);
            SetNoticeVisible(n, n.Live);
            if (n.Live) LayoutNotice(n);
        }

        private void SetNoticeVisible(NoticeView n, bool on)
        {
            n.Text.gameObject.SetActive(on);
            n.Shadow.gameObject.SetActive(on);
            n.Border.gameObject.SetActive(on);
            n.Pill.gameObject.SetActive(on);
        }

        /// <summary>Acomoda la píldora al texto (17 dp; 19 en las grandes; si no cabe en el ancho, achica hasta 14 dp) en su fila: la grande o la chica.</summary>
        private void LayoutNotice(NoticeView n)
        {
            n.Text.text = n.Message;
            n.Text.horizontalOverflow = HorizontalWrapMode.Overflow;
            float fs = n.Big ? 19f : 17f;
            do
            {
                n.Text.fontSize = Mathf.RoundToInt(fs * _s);
                if (n.Text.preferredWidth / _s <= LandingPlan.NoticeMaxWidth - 30f) break;
                fs -= 1f;
            } while (fs > 14f);
            n.Text.fontSize = Mathf.RoundToInt(Mathf.Max(14f, fs) * _s);
            float w = Mathf.Min(LandingPlan.NoticeMaxWidth, n.Text.preferredWidth / _s + 30f);
            n.Width = w;
            float cx = LandingPlan.Width * 0.5f, cy = n.Big ? _plan.NoticeBigY : _plan.NoticeSmallY;
            SetRect(n.Text.rectTransform, cx, cy + 1f, w, 30f);
            SetRect(n.Pill.rectTransform, cx, cy, w, LandingPlan.NoticeH);
            SetRect(n.Border.rectTransform, cx, cy, w + 6f, LandingPlan.NoticeH + 6f);
            SetRect(n.Shadow.rectTransform, cx, cy + 4f, w + 6f, LandingPlan.NoticeH + 6f);
        }

        private void AnimateNotices()
        {
            foreach (var n in _notices)
            {
                if (!n.Live) continue;
                if (_phase != Phase.Land) { n.Live = false; SetNoticeVisible(n, false); continue; }
                float k = (ResultT() - n.At) / n.Life;
                if (k >= 1f) { n.Live = false; SetNoticeVisible(n, false); continue; }
                float a = Mathf.Clamp01(Mathf.Min(1f, k * 7f, (1f - k) * 3f));
                n.Text.color = new Color(n.Color.r, n.Color.g, n.Color.b, a);
                n.Pill.color = new Color(NoticeFill.r, NoticeFill.g, NoticeFill.b, a);
                n.Border.color = new Color(Ink.r, Ink.g, Ink.b, a);
                n.Shadow.color = new Color(Shadow.r, Shadow.g, Shadow.b, Shadow.a * a);
            }
        }

        // ------------------------------------------------------------------ el marcador

        private void UpdateHud()
        {
            if (_guided) return;
            _hud.SetLevel(_dda.PresentedLevel);
            _hud.SetInfo(LandingContract.RoundChip(_trials + 1, Endless ? 0 : _trialsTotal));
            RefreshHudExtras();
        }

        /// <summary>La cúpula de tu base (celeste desde la primera) con su número y los 5 puntos hacia la próxima, y «Racha ×N» desde la racha 2. Todo arriba a la derecha: nada de esto queda cerca de la regla.</summary>
        private void RefreshHudExtras()
        {
            if (_hudCount == null) return;
            _hudCount.text = _shownDomes.ToString();
            if (_baked) _hudDome.sprite = LandingSprites.Dome(_shownDomes > 0);
            int toNext = LandingContract.ToNextDome(_shownHits);
            for (int i = 0; i < ProgressDots; i++) _hudDot[i].color = i < toNext ? Lime : new Color(184f / 255f, 176f / 255f, 1f, 0.35f);
            bool hot = !_guided && _streak >= 2 && _phase != Phase.Done;
            _streakBg.gameObject.SetActive(hot);
            _streakText.gameObject.SetActive(hot);
            if (hot) _streakText.text = LandingContract.StreakChip(_streak);
        }

        /// <summary>El salto de la cúpula del marcador cuando se arma una nueva (solo con animaciones).</summary>
        private void AnimateHud(float now)
        {
            float k = (now - _domePopAt) / 0.45f;
            float s = Motion.Decorative && k >= 0f && k < 1f ? 1.1f + 0.3f * Mathf.Sin(k * Mathf.PI) : 1.1f;
            SetRect(_hudDome.rectTransform, LandingPlan.Width - 30f, 20f, LandingSprites.DomeW * s, LandingSprites.DomeH * s);
        }

        /// <summary>La barra del Reto (llena = todo el tiempo; baja de sol a lima... a coral): fina, bajo el marcador.</summary>
        private void SetTimer(float fraction)
        {
            _timerFraction = Mathf.Clamp01(fraction);
            var r = _timerFill.rectTransform;
            r.sizeDelta = new Vector2(Mathf.Max(0.01f, (LandingPlan.Width - 24f) * _timerFraction) * _s, 5f * _s);
            r.anchoredPosition = P(12f, 71f);
            _timerFill.color = _timerFraction > 0.5f ? Color.Lerp(Gold, Lime, (_timerFraction - 0.5f) * 2f) : Color.Lerp(Coral, Gold, _timerFraction * 2f);
        }

        // ------------------------------------------------------------------ el propulsor (mientras baja)

        /// <summary>El volumen del propulsor: sube con una constante de 0,15 s mientras la nave baja (vuelo sin soltar) y baja a 0 con 0,08 s al soltar; con la congelación de Nubi queda al 30 % y sin sonido no suena.</summary>
        private void AnimateHover(float realDt)
        {
            if (_hoverSource == null || _hoverSource.clip == null) return;
            bool want = _phase == Phase.Fall && SoundWanted && !GameClock.AudioSilenced;
            float target = want ? LandingSounds.HoverVolume * GameClock.LoopVolume : 0f;
            float tau = target > _hoverSource.volume ? 0.15f : 0.08f;
            _hoverSource.volume += (target - _hoverSource.volume) * (1f - Mathf.Exp(-realDt / tau));
            if (want && !_hoverSource.isPlaying) _hoverSource.Play();
            else if (!want && _hoverSource.isPlaying && _hoverSource.volume < 0.002f) _hoverSource.Stop();
        }

        private void StopHover()
        {
            if (_hoverSource == null) return;
            _hoverSource.volume = 0f;
            if (_hoverSource.isPlaying) _hoverSource.Stop();
        }

        // ------------------------------------------------------------------ la pantalla final

        private void ShowEnd(float meanErr)
        {
            _hud.Rect.gameObject.SetActive(false);
            _hudLayer.gameObject.SetActive(false);
            _noticeLayer.gameObject.SetActive(false);
            _timerTrack.gameObject.SetActive(false);
            _timerFill.gameObject.SetActive(false);
            _endTitle.text = LandingContract.EndTitle(_hits, _trials);
            _endEstimate.text = LandingContract.EndEstimate(meanErr);
            _endSummary.text = LandingContract.EndSummary(_bulls, _bestStreak, _domes);
            _endAt = Now;
            _endLayer.gameObject.SetActive(true);
            Layout();
            if (Motion.Decorative)
            {
                _confettiOn = true;
                _confettiAt = Now;
                var rng = new System.Random();
                var cols = new[] { Lime, Cyan, Gold, new Color(183f / 255f, 155f / 255f, 1f), Coral };
                for (int i = 0; i < ConfettiPool; i++)
                {
                    _confettiPos[i] = new Vector2(LandingPlan.Width * 0.5f + ((float)rng.NextDouble() - 0.5f) * 120f, 170f);
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
