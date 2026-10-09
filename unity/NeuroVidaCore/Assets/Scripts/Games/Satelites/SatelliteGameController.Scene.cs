using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Satelites
{
    public sealed partial class SatelliteGameController
    {
        // ------------------------------------------------------------------ armar y limpiar

        /// <summary>Arma el enjambre de la ronda y deja a los satélites en sus lugares (sin señal, sin marcas).</summary>
        private void BeginRoundObjects(LevelSpec spec, Surprise surprise)
        {
            _swarm = new OrbitSwarm(spec, surprise, _plan.Orbits, new System.Random(_rng.Next()));
            _orbitLayer.gameObject.SetActive(true);
            _cloud.gameObject.SetActive(false);
            _crossMarker.gameObject.SetActive(false);
            for (int i = 0; i < _sats.Length; i++)
            {
                var v = _sats[i];
                _marked[i] = false;
                v.Root.gameObject.SetActive(i < _swarm.Count);
                v.Root.localScale = Vector3.one;
                v.Body.color = Color.white;
                v.Glow.gameObject.SetActive(false);
                v.CueRing.gameObject.SetActive(false);
                v.Envelope.gameObject.SetActive(false);
                v.MarkRing.gameObject.SetActive(false);
                v.Dashed.gameObject.SetActive(false);
                v.Badge.gameObject.SetActive(false);
            }
            PlaceSwarm();
            UpdateFooter();
        }

        /// <summary>Todo lo de una ronda se esconde (el planeta y sus luces se quedan).</summary>
        private void ClearRoundViews()
        {
            _swarm = null;
            foreach (var v in _sats) v.Root.gameObject.SetActive(false);
            _cloud.gameObject.SetActive(false);
            _crossMarker.gameObject.SetActive(false);
        }

        /// <summary>El estado de reposo: nada de ronda, sin aviso, sin pantalla final, sin vuelos.</summary>
        private void ResetRoundViews()
        {
            _swarm = null;
            foreach (var v in _sats) v.Root.gameObject.SetActive(false);
            foreach (var f in _flights) { f.Active = false; f.Root.gameObject.SetActive(false); }
            _cloud.gameObject.SetActive(false);
            _crossMarker.gameObject.SetActive(false);
            _surpriseLayer.gameObject.SetActive(false);
            _endLayer.gameObject.SetActive(false);
            _orbitLayer.gameObject.SetActive(false);
            _promptLayer.gameObject.SetActive(true);
            _footerLayer.gameObject.SetActive(true);
            _timerBg.gameObject.SetActive(false);
            _promptA.text = _promptB.text = "";
            for (int i = 0; i < RoundSlots; i++) { _discRing[i].gameObject.SetActive(false); _discFill[i].gameObject.SetActive(false); }
            _lightsText.text = "";
            StopHum();
        }

        /// <summary>El planeta a oscuras otra vez (al empezar una partida y al volver del tutorial).</summary>
        private void ResetPlanet()
        {
            _lightsShown = 0;
            _lightSpots.Clear();
            _planetBig = false;
            _planetRoot.localScale = Vector3.one;
            _planetRoot.anchoredPosition = P(_plan.Cx, _plan.Cy);
            foreach (var d in _lightDots) { d.On = false; d.Root.gameObject.SetActive(false); }
            _halo.color = new Color(1f, 214f / 255f, 120f / 255f, 0f);
        }

        // ------------------------------------------------------------------ textos

        private void SetPrompt(string a, string b, Color colorA)
        {
            _promptA.text = a;
            _promptA.color = colorA;
            _promptB.text = b;
        }

        private void UpdateFooter()
        {
            bool discs = !Endless && !_guided;
            int played = _run != null ? _run.RoundsPlayed : 0;
            for (int i = 0; i < RoundSlots; i++)
            {
                _discRing[i].gameObject.SetActive(discs);
                _discRing[i].color = i < played ? new Color(Lavender.r, Lavender.g, Lavender.b, 0.7f) : i == played ? Cyan : new Color(Lavender.r, Lavender.g, Lavender.b, 0.45f);
                bool done = discs && i < played;
                _discFill[i].gameObject.SetActive(done);
                if (!done) continue;
                bool perfect = SatelliteContract.IsPerfect(_run.Rounds[i].hits, _run.Rounds[i].targets);
                _discFill[i].sprite = perfect ? DiscSprite.Get() : SatelliteSprites.HalfDisc();
                _discFill[i].color = perfect ? Good : Gold;
            }
            _lightsText.gameObject.SetActive(!_guided);
            _lightsText.text = SatelliteContract.LightsFooter(_lightsShown);
        }

        // ------------------------------------------------------------------ los satélites

        private void PlaceSwarm()
        {
            for (int i = 0; i < _swarm.Count; i++) _sats[i].Root.anchoredPosition = P(_swarm.X[i], _swarm.Y[i]);
            if (_swarm.Cloud != null) _cloud.rectTransform.anchoredPosition = P(_swarm.Cloud.X, _swarm.Cloud.Y);
        }

        /// <summary>La señal de un satélite con mensaje: aro dorado (y, en la presentación, resplandor y sobre); <paramref name="fade"/> la apaga de a poco.</summary>
        private void SetCue(int i, float fade, float pulse, bool full = true)
        {
            var v = _sats[i];
            v.CueRing.gameObject.SetActive(true);
            v.CueRing.color = new Color(Gold.r, Gold.g, Gold.b, fade);
            v.Glow.gameObject.SetActive(full);
            v.Glow.color = new Color(Gold.r, Gold.g, Gold.b, 0.6f * pulse * fade);
            v.Envelope.gameObject.SetActive(full);
            v.Envelope.color = new Color(1f, 1f, 1f, fade);
        }

        private void HideCue(int i)
        {
            var v = _sats[i];
            v.CueRing.gameObject.SetActive(false);
            v.Glow.gameObject.SetActive(false);
            v.Envelope.gameObject.SetActive(false);
        }

        private static void ShowBadge(SatView v, bool ok)
        {
            v.Badge.sprite = ok ? AnswerMarkSprite.Check() : AnswerMarkSprite.Cross();
            v.Badge.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ «¿Aquí se cruzaron?»

        private void ShowCross(float x, float y)
        {
            const float w = 172f, h = 28f;
            _crossMarker.gameObject.SetActive(true);
            _crossMarker.anchoredPosition = P(x, y);
            _crossMarker.sizeDelta = Vector2.zero;
            float above = y - 25f - h * 0.5f - 6f;
            float dy = above < _plan.BandTop - 10f ? 25f + h * 0.5f + 6f : -(25f + h * 0.5f + 6f);
            float cx = Mathf.Clamp(x, w * 0.5f + 8f, ScreenPlan.Width - w * 0.5f - 8f);
            float dx = cx - x;
            SetChild(_crossPillBg.rectTransform, dx, dy, w, h);
            SetChild(_crossLabel.rectTransform, dx, dy, w, h);
            StartCoroutine(PopIn(_crossMarker, 0.3f));
        }

        // ------------------------------------------------------------------ las luces del planeta

        private void StartFlight(int sat, float at)
        {
            Flight f = null;
            foreach (var c in _flights) if (!c.Active) { f = c; break; }
            if (f == null) return;
            var spot = SatelliteContract.NextLightSpot(_lightSpots, _rng);
            f.Active = true;
            f.Landed = false;
            f.At = at;
            f.From = new Vector2(_swarm.X[sat], _swarm.Y[sat] - SatelliteContract.SatRadius - 14f);
            f.To = new Vector2(_plan.Cx + spot.x, _plan.Cy + spot.y);
            f.Light = -1;
            if (_lightSpots.Count < _lightDots.Length)
            {
                f.Light = _lightSpots.Count;
                _lightDots[f.Light].Spot = new Vector2(spot.x, spot.y);
                SetChild(_lightDots[f.Light].Root, spot.x, spot.y, 18f, 18f);
            }
            _lightSpots.Add(spot);
            f.Root.gameObject.SetActive(false);
        }

        private void AnimateFlights(float now)
        {
            foreach (var f in _flights)
            {
                if (!f.Active) continue;
                float k = (now - f.At) / f.Duration;
                if (k < 0f) continue;
                if (k >= 1f)
                {
                    f.Active = false;
                    f.Root.gameObject.SetActive(false);
                    LightUp(f);
                    continue;
                }
                if (!f.Root.gameObject.activeSelf) f.Root.gameObject.SetActive(true);
                if (Motion.Decorative)
                {
                    float e = k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) / 2f;
                    float x = Mathf.Lerp(f.From.x, f.To.x, e), y = Mathf.Lerp(f.From.y, f.To.y, e) - Mathf.Sin(Mathf.PI * e) * 40f;
                    f.Root.anchoredPosition = P(x, y);
                    f.Root.localScale = Vector3.one * (1f - 0.5f * e);
                    f.Glow.gameObject.SetActive(true);
                    f.Envelope.color = Color.white;
                }
                else
                {
                    // «quitar animaciones»: el sobre no vuela; se queda donde estaba y se apaga, y la luz aparece al terminar
                    f.Root.anchoredPosition = P(f.From.x, f.From.y);
                    f.Root.localScale = Vector3.one;
                    f.Glow.gameObject.SetActive(false);
                    f.Envelope.color = new Color(1f, 1f, 1f, 1f - k);
                }
            }
        }

        private void LightUp(Flight f)
        {
            _lightsShown++;
            if (f.Light >= 0)
            {
                var d = _lightDots[f.Light];
                d.On = true;
                d.At = Now;
                d.Root.gameObject.SetActive(true);
                if (Motion.Decorative) StartCoroutine(UiFx.SparkBurst(_fxLayer, P(_plan.Cx + d.Spot.x, _plan.Cy + d.Spot.y), Gold, 5, 40f * _s, 6f * _s, 0.4f));
            }
            PlayClip(SatelliteSounds.LightOn(_lightsShown), 0.7f);
            UpdateFooter();
        }

        private void AnimateLights(float now)
        {
            float lit = Mathf.Min(1f, _lightsShown / 30f);
            _halo.color = new Color(1f, 214f / 255f, 120f / 255f, lit > 0f ? 0.25f + 0.45f * lit : 0f);
            foreach (var d in _lightDots)
            {
                if (!d.On) continue;
                float k = Motion.Decorative ? Mathf.Min(1f, (now - d.At) / 0.5f) : 1f;
                float pulse = Motion.Decorative ? 1f + 0.25f * Mathf.Sin(now * 2.5f + d.Spot.x) : 1f;       // con «quitar animaciones» las luces quedan fijas
                d.Glow.color = new Color(1f, 220f / 255f, 140f / 255f, k);
                d.Glow.rectTransform.localScale = Vector3.one * pulse;
                d.Core.color = new Color(1f, 231f / 255f, 168f / 255f, k);
            }
        }

        // ------------------------------------------------------------------ la pantalla final

        private void ShowEnd(int record, bool broke)
        {
            _orbitLayer.gameObject.SetActive(false);
            _promptLayer.gameObject.SetActive(false);
            _footerLayer.gameObject.SetActive(false);
            _timerBg.gameObject.SetActive(false);
            foreach (var f in _flights) { f.Active = false; f.Root.gameObject.SetActive(false); }
            _planetBig = true;
            _planetRoot.localScale = Vector3.one * 1.75f;
            _planetRoot.anchoredPosition = P(ScreenPlan.Width * 0.5f, EndPlanetY);
            _endTitle.text = SatelliteContract.LightsTitle(_run.Lights);
            float cap = _run.Capacity, mean = _run.MeanTargets;
            _endLabel.gameObject.SetActive(cap >= 0f);
            _endValue.text = cap >= 0f ? SatelliteContract.TrackedLine(cap, mean) : "";
            _endDetail.text = SatelliteContract.PerfectLine(_run.Perfect, _run.RoundsPlayed, _run.BestStreak);
            _endRecord.text = SatelliteContract.RecordLine(record, broke);
            _endLayer.gameObject.SetActive(true);
        }

        /// <summary>El centro del planeta grande de la pantalla final (dp desde arriba).</summary>
        private static float EndPlanetY => ScreenPlan.HudBottom + 78f + 80f;
    }
}
