using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using static NeuroVida.Games.Shared.UiKit;
using Motion = NeuroVida.Games.Shared.Motion; // UnityEngine.Motion también existe

namespace NeuroVida.Games.Parejas
{
    public sealed partial class ConstelacionGameController
    {
        // ------------------------------------------------------------------ armar y limpiar el cielo

        /// <summary>Muestra un cielo nuevo: una vista por luz (dormida, de su tamaño), sin líneas ni etiquetas.</summary>
        private void BindBoard(ConstelacionSky sky)
        {
            ResetLinks();
            _cardLayer.gameObject.SetActive(true);
            _rowLayer.gameObject.SetActive(true);
            _boardRoot.gameObject.SetActive(true);
            _fxLayer.gameObject.SetActive(true);
            _hintLights.Clear();
            _hintDueAt = -1f;
            _lastOpps = -1;
            _lastCardTitle = _lastCardSub = null;
            Layout();
            float skyH = sky.Placement.Height, r = sky.Placement.R, k = r / ConstelacionSprites.BakeR;
            var dormant = ConstelacionSprites.Dormant();
            for (int i = 0; i < LightPool; i++)
            {
                var v = _lv[i];
                v.Root.gameObject.SetActive(false);
                if (i >= sky.Lights.Length) continue;
                var l = sky.Lights[i];
                v.F = v.Vf = 0f;
                v.HintAt = v.PressAt = -10f;
                v.Up = false;
                v.Kind = l.Kind;
                v.Variant = l.Variant;
                v.RingKind = ConLine.None;
                v.Root.anchoredPosition = SkyLocal(l.Pos.X, l.Pos.Y, skyH);
                float body = ConstelacionSprites.DormantBox * k * Su;
                v.Body.rectTransform.sizeDelta = Vector2.one * body;
                v.Body.sprite = dormant;
                v.Obj.rectTransform.sizeDelta = Vector2.one * (ConstelacionSprites.ObjBox * (r * 1.32f / 40f) * Su);
                v.Obj.sprite = ConstelacionSprites.Object(l.Kind, l.Variant);
                v.Obj.gameObject.SetActive(false);
                v.Ring.rectTransform.sizeDelta = Vector2.one * (ConstelacionSprites.RingBox * k * Su);
                v.Ring.gameObject.SetActive(false);
                v.HintRing.rectTransform.sizeDelta = Vector2.one * (ConstelacionSprites.RingBox * k * Su * 1.04f);
                v.Hint.rectTransform.sizeDelta = Vector2.one * ((2f * r + 52f) * Su);
                v.Hint.color = new Color(GoldSoft.r, GoldSoft.g, GoldSoft.b, 0f);
                v.HintRing.color = new Color(GoldSoft.r, GoldSoft.g, GoldSoft.b, 0f);
            }
            LayoutDots();
        }

        /// <summary>Todo apagado: sin luces, líneas, etiquetas ni partículas (al empezar, al terminar y al abrir «Cómo se juega»).</summary>
        private void ResetBoardViews()
        {
            foreach (var v in _lv) if (v != null) v.Root.gameObject.SetActive(false);
            ResetLinks();
            foreach (var f in _floats) if (f != null) { f.At = -10f; f.Root.gameObject.SetActive(false); }
            foreach (var sp in _sparks) { sp.Alive = false; sp.Img.gameObject.SetActive(false); }
            foreach (var d in _dots) if (d != null) d.Root.gameObject.SetActive(false);
            _hintLights.Clear();
            _hintDueAt = -1f;
            _lastOpps = -1;
            _msgAt = -10f;
        }

        private void ResetLinks()
        {
            foreach (var l in _links)
            {
                l.Link = null;
                l.Settled = false;
                l.Glow.gameObject.SetActive(false);
                l.Border.gameObject.SetActive(false);
                l.Core.gameObject.SetActive(false);
                l.Spark.gameObject.SetActive(false);
            }
            foreach (var t in _thread) if (t != null) t.gameObject.SetActive(false);
            _linkGlowAt.Clear();
        }

        private void AddLinkView(ConLink link)
        {
            LinkView free = null;
            foreach (var l in _links) if (l.Link == null) { free = l; break; }
            if (free == null) free = _links[0];
            free.Link = link;
            free.Settled = false;
        }

        // ------------------------------------------------------------------ cada cuadro: las luces y las líneas

        private void AnimateBoard(float now, float dt)
        {
            if (_sky == null || !_boardRoot.gameObject.activeSelf) return;
            bool deco = Motion.Decorative;
            float nowMs = now * 1000f, skyH = _sky.Placement.Height;
            bool leaving = _phase == Phase.Leaving;
            for (int i = 0; i < _sky.Lights.Length; i++)
            {
                var l = _sky.Lights[i];
                var v = _lv[i];
                // aparece escalonada: crece con un pequeño rebote (con «quitar animaciones», ya está)
                float ap = deco ? Mathf.Clamp01((nowMs - l.AppearAtMs) / ConstelacionContract.AppearMs) : (nowMs >= l.AppearAtMs ? 1f : 0f);
                float sc = deco ? ConstelacionMotion.EaseBack(ap) : 1f;
                if (leaving && deco) sc *= 1f - ConstelacionMotion.EaseOut((now - _leaveAt) / ConstelacionMotion.LeaveSeconds);
                if (v.PressAt > 0f && now - v.PressAt < ConstelacionMotion.PressSeconds && deco) sc *= 1f - (1f - ConstelacionMotion.PressScale) * (1f - (now - v.PressAt) / ConstelacionMotion.PressSeconds);
                bool visible = ap > 0f && sc > 0.01f && _phase != Phase.Done;
                if (v.Root.gameObject.activeSelf != visible) v.Root.gameObject.SetActive(visible);
                if (!visible) continue;
                v.Root.localScale = Vector3.one * sc;
                // el volteo: abrir con un poco de rebote, cerrar más rápido y sin rebote
                float target = l.State == ConState.Down ? 0f : 1f;
                if (!deco) { v.F = target; v.Vf = 0f; }
                else ConstelacionMotion.Flip(ref v.F, ref v.Vf, target, dt);
                bool up = v.F > 0.5f;
                if (up != v.Up || v.Body.sprite == null)
                {
                    v.Up = up;
                    v.Body.sprite = up ? ConstelacionSprites.Open() : ConstelacionSprites.Dormant();
                }
                if (v.Obj.gameObject.activeSelf != up) v.Obj.gameObject.SetActive(up);
                float sx = ConstelacionMotion.FlipWidth(v.F), lift = ConstelacionMotion.FlipLift(v.F);
                v.Flip.localScale = new Vector3(sx * lift, lift, 1f);
                // el aro de la pareja hecha: dorado continuo (de memoria) o celeste punteado (a la primera vista)
                bool done = l.State == ConState.Done;
                if (v.Ring.gameObject.activeSelf != done) v.Ring.gameObject.SetActive(done);
                if (done && v.RingKind != l.Ring)
                {
                    v.RingKind = l.Ring;
                    v.Ring.sprite = l.Ring == ConLine.Memory ? ConstelacionSprites.RingMemory() : ConstelacionSprites.RingNew();
                }
                // el brillo de pista: aquí estaba la pareja que se te escapó (no se da vuelta); fijo con «quitar animaciones»
                float hk = (now - v.HintAt) / (ConstelacionContract.HintMs / 1000f);
                float ha = hk >= 0f && hk < 1f ? (deco ? Mathf.Sin(Mathf.PI * hk) : (hk < 0.8f ? 1f : 0f)) : 0f;
                v.Hint.color = new Color(GoldSoft.r, GoldSoft.g, GoldSoft.b, ha);
                v.HintRing.color = new Color(GoldSoft.r, GoldSoft.g, GoldSoft.b, ha);
            }
            for (int i = _sky.Lights.Length; i < LightPool; i++) if (_lv[i].Root.gameObject.activeSelf) _lv[i].Root.gameObject.SetActive(false);

            foreach (var lv in _links) if (lv.Link != null) UpdateLink(lv, nowMs, deco, skyH);
            UpdateThread(skyH);
        }

        private static List<Vector2> TracePoints(ConLinkGeo g, float t0, float t1, float skyH)
        {
            var pts = new List<Vector2>(29);
            for (int i = 0; i <= 28; i++)
            {
                var p = g.At(t0 + (t1 - t0) * i / 28f);
                pts.Add(SkyLocal(p.X, p.Y, skyH));
            }
            return pts;
        }

        /// <summary>Una línea: se traza en 380 ms desacelerando (con una chispa en la punta), con borde oscuro y sobre todas las luces; al terminar el cielo se ilumina una por una.</summary>
        private void UpdateLink(LinkView v, float nowMs, bool deco, float skyH)
        {
            var l = v.Link;
            float k = deco ? Mathf.Clamp01((nowMs - l.AtMs) / (ConstelacionMotion.TraceSeconds * 1000f)) : (nowMs >= l.AtMs ? 1f : 0f);
            if (k <= 0f)
            {
                v.Glow.gameObject.SetActive(false); v.Border.gameObject.SetActive(false); v.Core.gameObject.SetActive(false); v.Spark.gameObject.SetActive(false);
                return;
            }
            float lg = 0f;
            if (_linkGlowAt.TryGetValue(l, out var g) && nowMs > g) lg = Mathf.Max(0f, 1f - (nowMs - g) / (ConstelacionMotion.GlowSeconds * 1000f));
            if (k >= 1f && v.Settled && lg <= 0f && !v.WasGlowing) return;
            v.WasGlowing = lg > 0f;
            float e = ConstelacionMotion.EaseOut(k), t0 = l.Geo.T0, t1 = t0 + (l.Geo.T1 - t0) * e;
            var pts = TracePoints(l.Geo, t0, t1, skyH);
            v.Glow.gameObject.SetActive(true); v.Border.gameObject.SetActive(true); v.Core.gameObject.SetActive(true);
            if (l.Type == ConLine.Memory)
            {
                v.Glow.Set(pts, (12f + 8f * lg) * Su, new Color(1f, 201f / 255f, 74f / 255f, 0.22f + 0.35f * lg));
                v.Border.Set(pts, 7.5f * Su, new Color(11f / 255f, 10f / 255f, 38f / 255f, 0.75f));      // borde oscuro: se lee nítida sobre una luz dormida
                v.Core.Set(pts, 4f * Su, Gold);
            }
            else
            {
                v.Glow.Set(pts, (9f + 6f * lg) * Su, new Color(Cyan.r, Cyan.g, Cyan.b, 0.12f + 0.25f * lg));
                v.Border.Set(pts, 6.5f * Su, new Color(11f / 255f, 10f / 255f, 38f / 255f, 0.7f), 7f * Su, 7f * Su);
                v.Core.Set(pts, 3f * Su, new Color(CyanSoft.r, CyanSoft.g, CyanSoft.b, 0.9f), 7f * Su, 7f * Su);
            }
            // la chispa que viaja mientras se traza
            bool spark = k < 1f && deco;
            v.Spark.gameObject.SetActive(spark);
            if (spark)
            {
                var tip = pts[pts.Count - 1];
                v.Spark.rectTransform.anchoredPosition = tip;
                v.Spark.rectTransform.sizeDelta = Vector2.one * (28f * Su);
                v.Spark.color = l.Type == ConLine.Memory ? new Color(1f, 214f / 255f, 120f / 255f, 0.9f) : new Color(CyanSoft.r, CyanSoft.g, CyanSoft.b, 0.9f);
            }
            if (k >= 1f) v.Settled = true;
        }

        /// <summary>Un trío a medias: un hilo tenue punteado une las dos abiertas mientras falta la tercera.</summary>
        private void UpdateThread(float skyH)
        {
            int n = _phase == Phase.Play ? _sky.Face.Count : 0;
            for (int i = 0; i < _thread.Length; i++)
            {
                bool on = i + 1 < n;
                if (_thread[i].gameObject.activeSelf != on) _thread[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = _sky.Face[i].Pos;
                var b = _sky.Face[i + 1].Pos;
                float dx = b.X - a.X, dy = b.Y - a.Y, d = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy)), o = _sky.Placement.R + 6f;
                var p0 = SkyLocal(a.X + dx / d * o, a.Y + dy / d * o, skyH);
                var p1 = SkyLocal(b.X - dx / d * o, b.Y - dy / d * o, skyH);
                _thread[i].Set(new List<Vector2> { p0, p1 }, 2.5f * Su, new Color(GoldSoft.r, GoldSoft.g, GoldSoft.b, 0.75f), 3f * Su, 6f * Su);
            }
        }

        // ------------------------------------------------------------------ la tarjeta de arriba y la fila de abajo

        private string _lastCardTitle, _lastCardSub;
        private int _lastOpps = -1;

        private void AnimateCard(float now)
        {
            if (!_cardLayer.gameObject.activeSelf || _sky == null) return;
            string title, sub;
            Color tc, sc = Lavender, rim = new Color(142f / 255f, 131f / 255f, 216f / 255f, 0.35f);
            if (_phase == Phase.BoardEnd || _phase == Phase.Leaving)
            {
                title = ConstelacionContract.CardDoneTitle;
                tc = Mint;
                sub = ConstelacionContract.DoneLine(_sky.Hits, _sky.OpportunityCount, _recordNow && !_guided);
            }
            else if (now - _msgAt < 1.8f)
            {
                title = ConstelacionContract.MissTitle;
                tc = GoldSoft;
                sub = ConstelacionContract.MissSub;
                rim = new Color(Gold.r, Gold.g, Gold.b, 0.5f);
            }
            else
            {
                title = ConstelacionContract.CardTitle(_sky.Stage);
                tc = TextColor;
                sub = ConstelacionContract.CardSub(_sky.Stage, _sky.PairsLeft, _sky.TriosLeft);
            }
            _cardRim.color = rim;
            _cardT.color = tc;
            _cardS.color = sc;
            if (title != _lastCardTitle) { _lastCardTitle = title; _cardT.text = title; }
            if (sub != _lastCardSub) { _lastCardSub = sub; _cardS.text = sub; }
        }

        private void AnimateRow(float now)
        {
            if (!_rowLayer.gameObject.activeSelf || _sky == null) return;
            int n = _sky.Opps.Count;
            if (n != _lastOpps)
            {
                _lastOpps = n;
                LayoutDots();
                _rowCount.text = ConstelacionContract.MemoryCount(_sky.Hits, n);
                _rowCount.color = n > 0 ? TextColor : Muted;
            }
        }

        // ------------------------------------------------------------------ etiquetas que flotan y partículas

        private void AddFloat(Vector2 logical, string text, Color color)
        {
            FloatView f = null;
            float oldest = float.MaxValue;
            foreach (var x in _floats)
            {
                if (x.At < oldest) { oldest = x.At; f = x; }
            }
            if (f == null) return;
            f.At = GameClock.Time;
            f.Pos = logical;
            f.Color = color;
            f.Text.text = text;
            float w = f.Text.preferredWidth / Mathf.Max(0.01f, _s) + 20f;
            f.Bg.rectTransform.sizeDelta = new Vector2(w * _s, 28f * _s);
            SetRadius(f.Bg, 14f * _s);
            f.Text.rectTransform.sizeDelta = new Vector2(w * _s, 28f * _s);
            f.Root.gameObject.SetActive(true);
        }

        private void AnimateFloats(float now)
        {
            foreach (var f in _floats)
            {
                if (!f.Root.gameObject.activeSelf) continue;
                float k = (now - f.At) / ConstelacionMotion.FloatSeconds;
                if (k >= 1f || k < 0f) { f.Root.gameObject.SetActive(false); continue; }
                float a = Mathf.Min(1f, Mathf.Min(k * 6f, (1f - k) * 3f));
                float y = f.Pos.y - (Motion.Decorative ? ConstelacionMotion.FloatRise * ConstelacionMotion.EaseOut(k) : 0f);
                f.Root.anchoredPosition = P(f.Pos.x, y);
                var bg = f.Bg.color; bg.a = 0.88f * a; f.Bg.color = bg;
                f.Text.color = new Color(f.Color.r, f.Color.g, f.Color.b, a);
            }
        }

        private void Emit(Vector2 logicalAt, int n, float spread, float life, Color color, float up)
        {
            if (!Motion.Decorative) return;
            for (int i = 0; i < n; i++)
            {
                var p = _sparks.Find(x => !x.Alive);
                if (p == null) return;
                p.Alive = true;
                p.Pos = logicalAt;
                p.Vel = new Vector2(((float)_rng.NextDouble() - 0.5f) * spread, ((float)_rng.NextDouble() - 0.5f) * spread - up);
                p.Age = 0f;
                p.Life = life * (0.6f + (float)_rng.NextDouble() * 0.7f);
                p.Size = 0.8f + (float)_rng.NextDouble() * 1.8f;
                p.Color = color;
                p.Img.gameObject.SetActive(true);
            }
        }

        private void AnimateSparks(float dt)
        {
            foreach (var p in _sparks)
            {
                if (!p.Alive) continue;
                p.Age += dt;
                if (p.Age >= p.Life) { p.Alive = false; p.Img.gameObject.SetActive(false); continue; }
                p.Vel.y += 60f * dt;
                p.Pos += p.Vel * dt;
                float k = p.Age / p.Life;
                p.Img.rectTransform.anchoredPosition = P(p.Pos);
                p.Img.rectTransform.sizeDelta = Vector2.one * (p.Size * 2f * (1f - k * 0.5f) * _s);
                p.Img.color = NeuroStyle.WithAlpha(p.Color, 0.9f * (1f - k));
            }
        }

        // ------------------------------------------------------------------ «NUEVO»: la ilustración de la regla

        private int _artCursor;

        private Image ArtImage(Sprite sprite, float cx, float cy, float dp, Color? tint = null)
        {
            var img = _introImgs[_artCursor++];
            img.sprite = sprite;
            img.color = tint ?? Color.white;
            SetRect(img.rectTransform, cx, cy, dp, dp);
            img.gameObject.SetActive(true);
            return img;
        }

        /// <summary>Una luz de muestra: el aro (si lo tiene), el disco claro y el objeto.</summary>
        private void ArtLight(float cx, float cy, float r, int kind, int variant, ConLine ring)
        {
            float k = r / ConstelacionSprites.BakeR;
            if (ring != ConLine.None) ArtImage(ring == ConLine.Memory ? ConstelacionSprites.RingMemory() : ConstelacionSprites.RingNew(), cx, cy, ConstelacionSprites.RingBox * k);
            ArtImage(ConstelacionSprites.Open(), cx, cy, ConstelacionSprites.DormantBox * k);
            ArtImage(ConstelacionSprites.Object(kind, variant), cx, cy, ConstelacionSprites.ObjBox * (r * 1.32f / 40f));
        }

        private void ArtLabel(int i, float cx, float cy, string text, float dp = 14f)
        {
            var t = _introLabels[i];
            t.text = text;
            t.fontSize = Mathf.RoundToInt(dp * _s);
            SetRect(t.rectTransform, cx, cy, 160f, 24f);
            t.gameObject.SetActive(true);
        }

        private void ArtLine(LineGraphic line, float width, Color color, params Vector2[] logicalPts)
        {
            var pts = new List<Vector2>();
            foreach (var p in logicalPts) pts.Add(P(p));
            line.Set(pts, width * _s, color);
            line.gameObject.SetActive(true);
        }

        private void LayoutIntroArt(float cx, float cy)
        {
            _artCursor = 0;
            foreach (var i in _introImgs) i.gameObject.SetActive(false);
            foreach (var t in _introLabels) t.gameObject.SetActive(false);
            _introLine.gameObject.SetActive(false);
            _introLine2.gameObject.SetActive(false);
            var ink = new Color(11f / 255f, 10f / 255f, 38f / 255f, 0.8f);
            float cw = ConstelacionLayout.W;
            switch (_introKind)
            {
                case ConIntro.Cielo:
                    ArtLine(_introLine, 7.5f, ink, new Vector2(cx - 60f, cy), new Vector2(cx + 60f, cy));
                    ArtLine(_introLine2, 4f, Gold, new Vector2(cx - 60f, cy), new Vector2(cx + 60f, cy));
                    ArtLight(cx - 60f, cy, 30f, 1, 0, ConLine.Memory);
                    ArtLight(cx + 60f, cy, 30f, 1, 0, ConLine.Memory);
                    break;
                case ConIntro.Gemelos:
                    ArtLight(cx - 60f, cy, 30f, 0, 0, ConLine.None);
                    ArtLight(cx + 60f, cy, 30f, 0, 1, ConLine.None);
                    ArtLabel(0, cx, cy, "≠", 26f);
                    _introLabels[0].color = GoldSoft;
                    ArtLabel(1, cx - 60f, cy + 46f, "con anillo");
                    ArtLabel(2, cx + 60f, cy + 46f, "sin anillo");
                    break;
                case ConIntro.Trios:
                    ArtLine(_introLine, 7.5f, ink, new Vector2(cx - 80f, cy + 6f), new Vector2(cx, cy - 18f), new Vector2(cx + 80f, cy + 6f));
                    ArtLine(_introLine2, 4f, Gold, new Vector2(cx - 80f, cy + 6f), new Vector2(cx, cy - 18f), new Vector2(cx + 80f, cy + 6f));
                    ArtLight(cx - 80f, cy + 6f, 26f, 6, 0, ConLine.Memory);
                    ArtLight(cx, cy - 18f, 26f, 6, 0, ConLine.Memory);
                    ArtLight(cx + 80f, cy + 6f, 26f, 6, 0, ConLine.Memory);
                    break;
                case ConIntro.Mezcla:
                    {
                        float x0 = cx - 130f;
                        ArtLine(_introLine, 3.5f, Gold, new Vector2(x0, cy), new Vector2(x0 + 58f, cy));
                        ArtLine(_introLine2, 3.5f, Gold, new Vector2(cx + 4f, cy), new Vector2(cx + 62f, cy));
                        ArtLight(x0, cy, 20f, 5, 0, ConLine.Memory);
                        ArtLight(x0 + 58f, cy, 20f, 5, 0, ConLine.Memory);
                        ArtLight(cx + 4f, cy, 20f, 7, 0, ConLine.Memory);
                        ArtLight(cx + 62f, cy, 20f, 7, 0, ConLine.Memory);
                        ArtLight(cx + 120f, cy, 20f, 7, 0, ConLine.New);
                        ArtLabel(0, x0 + 29f, cy + 38f, "pareja");
                        ArtLabel(1, cx + 62f, cy + 38f, "trío: falta una");
                        break;
                    }
                default:
                    for (int i = 0; i < 18; i++)
                    {
                        float x = cx - 110f + (i % 6) * 44f + ((i / 6) % 2) * 14f, y = cy - 34f + (i / 6) * 34f;
                        ArtImage(ConstelacionSprites.Dormant(), x, y, 28f);
                    }
                    break;
            }
        }

        private void AnimateIntro(float now)
        {
            if (!_introLayer.gameObject.activeSelf) return;
            bool deco = Motion.Decorative;
            float k = deco ? 1f - Mathf.Pow(1f - Mathf.Clamp01((now - _introAt) / 0.35f), 3f) : 1f;
            _dim.color = new Color(2f / 255f, 3f / 255f, 15f / 255f, 0.8f * k);
            _introGroup.alpha = k;
            _introTap.color = deco ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f + 0.3f * Mathf.Sin(now * 4f)) : Cyan;
        }

        // ------------------------------------------------------------------ verificación de que lo dibujado se ve

        /// <summary>Para las pruebas: arma un cielo de cada etapa y devuelve lo que NO se vería (pieza apagada, sin imagen o sin opacidad): la tarjeta, la fila, cada luz con su cuerpo, su objeto y su aro, las líneas (tres mallas
        /// cada una) y los 12 objetos con sus 4 gemelos. Una pieza horneada sin sprite se dibuja como un cuadrado blanco (lo que pasó con las fichas de Punta el 3-oct).</summary>
        public List<string> AuditVisibility()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _lay = ConstelacionLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            var bake = ConstelacionSprites.Prewarm();
            while (bake.MoveNext()) { }
            AssignSprites();
            void Check(string what, Graphic g, bool needsSprite, bool needsAlpha = true)
            {
                if (g == null) { problems.Add(what + ": no existe"); return; }
                if (!g.gameObject.activeInHierarchy) problems.Add(what + ": apagada");
                else if (needsAlpha && g.color.a <= 0.01f) problems.Add(what + ": transparente");
                if (needsSprite && g is Image im && im.sprite == null) problems.Add(what + ": sin imagen");
            }
            _cardLayer.gameObject.SetActive(true);
            _rowLayer.gameObject.SetActive(true);
            Check("tarjeta.fondo", _cardBg, true);
            Check("tarjeta.borde", _cardRim, true, false);
            if (_links.Count < LinkPool) problems.Add("faltan vistas de líneas");
            for (int level = 1; level <= ConstelacionContract.MaxLevel; level++)
            {
                var st = ConstelacionContract.Stage(level);
                string tag = "etapa " + level + ".";
                if (st.Lights > LightPool) problems.Add(tag + st.Lights + " luces y solo hay " + LightPool + " vistas");
                if (st.Lights - st.Groups > LinkPool) problems.Add(tag + (st.Lights - st.Groups) + " líneas y solo hay " + LinkPool + " vistas");
                _sky = ConstelacionSky.Create(st, _rng, ConstelacionLayout.SkyW, _lay.SkyH);
                BindBoard(_sky);
                for (int i = 0; i < _sky.Lights.Length; i++)
                {
                    var v = _lv[i];
                    v.Root.gameObject.SetActive(true);
                    Check(tag + "luz " + i + ".cuerpo", v.Body, true);
                    v.Obj.gameObject.SetActive(true);
                    Check(tag + "luz " + i + ".objeto", v.Obj, true);
                    v.Ring.gameObject.SetActive(true);
                    v.Ring.sprite = ConstelacionSprites.RingMemory();
                    Check(tag + "luz " + i + ".aro", v.Ring, true);
                    if (v.Body.rectTransform.sizeDelta.x < 1f) problems.Add(tag + "luz " + i + ": sin tamaño");
                }
            }
            for (int kind = 0; kind < ConstelacionContract.ObjectKinds; kind++)
            {
                if (ConstelacionSprites.Object(kind, 0) == null) problems.Add("objeto " + kind + ": sin imagen");
                if (ConstelacionContract.HasTwin(kind) && ConstelacionSprites.Object(kind, 1) == null) problems.Add("gemelo " + kind + ": sin imagen");
            }
            foreach (var lv in _links)
            {
                lv.Glow.gameObject.SetActive(true); lv.Border.gameObject.SetActive(true); lv.Core.gameObject.SetActive(true);
                if (lv.Glow == null || lv.Border == null || lv.Core == null) problems.Add("línea sin malla");
            }
            ResetBoardViews();
            _sky = null;
            return problems;   // nada se destruye aquí (en modo edición no se puede): quien llama destruye el objeto entero
        }
    }
}
