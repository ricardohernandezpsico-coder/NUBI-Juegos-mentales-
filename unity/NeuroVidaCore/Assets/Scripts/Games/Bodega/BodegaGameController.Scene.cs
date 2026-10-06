using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeuroVida.Games.Shared;
using Motion = NeuroVida.Games.Shared.Motion;

namespace NeuroVida.Games.Bodega
{
    public sealed partial class BodegaGameController
    {
        private sealed class RobotState
        {
            // posición, ángulo hacia donde mira y sus objetivos (todo sigue al objetivo con un resorte); antena y caja colgando con su propio resorte; aplastado al lanzar el haz
            public float X = BodegaLayout.CenterX, Y = BodegaLayout.CenterY, Vx, Vy, Tx = BodegaLayout.CenterX, Ty = BodegaLayout.CenterY;
            public float Ang = Mathf.PI / 2f, Va, Ta = Mathf.PI / 2f;
            public float Ant, Vant, Sw, Vsw, Sq;
        }

        private struct Pt { public float x, y, a; }

        private sealed class Found { public int Obj; public bool First; }

        private readonly RobotState _robot = new RobotState();
        private readonly List<Found> _found = new List<Found>();
        private readonly float[] _landAt = new float[TrayPool];

        // la bodega
        private float _rot;
        private int _hint = -1;
        // la carga que viaja de la esclusa a su escotilla
        private bool _flyActive;
        private int _flyObjId, _flyTo;
        private float _flyStartsAt, _flyDur, _lockInAt = -10f, _lockOutAt = -10f;
        private Vector2 _lastFlyPt;
        // el haz
        private bool _beamOn;
        private int _beamHatch = -1;                       // -1 = sigue a la carga que viaja
        private float _beamAt, _beamOffAt = -10f;
        private Vector2 _beamLastEnd;
        // la caja
        private CrateMode _crateMode;
        private int _crateHatch;
        private float _crateAt, _crateDur;
        // la tarjeta de arriba
        private CardKind _card;
        private int _cardObj_ = -1, _cardFirst;
        private float _cardAt;
        private bool _cardPerfect;
        // el aviso de abajo
        private string _toastTitleFull = "", _toastSubFull = "";
        private bool _toastGood;
        private float _toastAt = -10f, _toastSeconds = 3f;

        // ------------------------------------------------------------------ el pedido en pantalla

        private void SetUpOrder(Order o)
        {
            ResetBoardState();
            _order = o;
            for (int i = 0; i < HatchPool; i++)
            {
                var v = _hv[i];
                v.Obj_ = i < o.Hatches ? o.Loaded[i] : -1;
                v.Root.gameObject.SetActive(i < o.Hatches);
            }
            _card = CardKind.Watch;
            _recordText.text = _bestRecord > 0 ? BodegaContract.RecordLine(_bestRecord) : "";
            _trayLabel.gameObject.SetActive(!_guided);               // en el tutorial no hay textos del carro: Nubi y su globo usan ese lugar
            _recordText.gameObject.SetActive(!_guided);
            _trayLayer.gameObject.SetActive(true);
            _cardLayer.gameObject.SetActive(true);
            _toastLayer.gameObject.SetActive(true);
            LayoutTray();
        }

        /// <summary>Deja la bodega como al empezar (sin pedido): puertas cerradas, el robot en el centro, nada volando.</summary>
        private void ResetBoard()
        {
            ResetBoardState();
            _order = null;
            _card = CardKind.None;
            _toastAt = -10f;
            if (_cardLayer != null) _cardLayer.gameObject.SetActive(false);
            if (_trayLayer != null) _trayLayer.gameObject.SetActive(false);
            if (_toastRoot != null) _toastRoot.gameObject.SetActive(false);
        }

        private void ResetBoardState()
        {
            _rot = 0f;
            _hint = -1;
            _flyActive = false;
            _beamOn = false;
            _beamOffAt = -10f;
            _crateMode = CrateMode.None;
            _lockInAt = _lockOutAt = -10f;
            _found.Clear();
            for (int i = 0; i < _landAt.Length; i++) _landAt[i] = 0f;
            var r = _robot;
            r.X = r.Tx = BodegaLayout.CenterX;
            r.Y = r.Ty = BodegaLayout.CenterY;
            r.Vx = r.Vy = r.Va = r.Vant = r.Vsw = 0f;
            r.Ang = r.Ta = Mathf.PI / 2f;
            r.Ant = r.Sw = r.Sq = 0f;
            foreach (var v in _hv)
            {
                if (v == null) continue;
                v.Open = v.Vo = v.Target = 0f;
                v.ShowAt = -1f;
                v.GlowAt = v.ShakeAt = v.PressAt = v.RevealAt = -10f;
                v.Obj_ = -1;
            }
            foreach (var f in _fly) if (f != null) { f.On = false; f.Glow.gameObject.SetActive(false); f.Obj.gameObject.SetActive(false); }
            foreach (var sp in _sparks) { sp.Alive = false; sp.Img.gameObject.SetActive(false); }
        }

        // ------------------------------------------------------------------ geometría de la bodega (coordenadas del boceto, y hacia abajo)

        private int Places => (_order != null ? _order.Hatches : 8) + 1;

        /// <summary>El centro de la escotilla <paramref name="i"/> (0..N-1) en el anillo, con el giro de ahora.</summary>
        private Vector2 HatchPos(int i)
        {
            BodegaLayout.RingPoint(i + 1, Places, _rot, BodegaLayout.Ring, out float x, out float y, out float a);
            return new Vector2(x, y);
        }

        private Pt HatchPt(int i)
        {
            BodegaLayout.RingPoint(i + 1, Places, _rot, BodegaLayout.Ring, out float x, out float y, out float a);
            return new Pt { x = x, y = y, a = a };
        }

        /// <summary>La esclusa de carga: SIEMPRE la posición 0 del anillo (gira con la bodega).</summary>
        private Pt LockPt()
        {
            BodegaLayout.RingPoint(0, Places, _rot, BodegaLayout.Ring, out float x, out float y, out float a);
            return new Pt { x = x, y = y, a = a };
        }

        /// <summary>Donde se para el robot para trabajar en una escotilla: sobre su dirección, a <paramref name="inset"/> del borde del anillo, mirando hacia ella.</summary>
        private Pt NearHatch(int i, float inset = 52f)
        {
            var p = HatchPos(i);
            float a = Mathf.Atan2(p.y - BodegaLayout.CenterY, p.x - BodegaLayout.CenterX);
            return new Pt { x = BodegaLayout.CenterX + Mathf.Cos(a) * (BodegaLayout.Ring - inset), y = BodegaLayout.CenterY + Mathf.Sin(a) * (BodegaLayout.Ring - inset), a = a };
        }

        private void RobotHome()
        {
            _robot.Tx = BodegaLayout.CenterX;
            _robot.Ty = BodegaLayout.CenterY;
            _robot.Ta = Mathf.PI / 2f;
        }

        private void LookAtLock()
        {
            var l = LockPt();
            _robot.Ta = Mathf.Atan2(l.y - BodegaLayout.CenterY, l.x - BodegaLayout.CenterX);
        }

        /// <summary>Dónde va la carga que viaja de la esclusa a su escotilla: por DENTRO del anillo, en arco y por el camino más corto (el radio baja hasta Ring−34 a mitad del camino). Null si no viaja.</summary>
        private bool FlyPoint(float now, out Vector2 pt, out float k)
        {
            pt = _lastFlyPt;
            k = 0f;
            if (!_flyActive) return false;
            k = Motion.Decorative ? Mathf.Clamp01((now - _flyStartsAt) / Mathf.Max(0.01f, _flyDur)) : 1f;
            float e = BodegaMotion.EaseInOut(k);
            float a0 = LockPt().a, a1 = HatchPt(_flyTo).a;
            float d = BodegaMotion.AngleDelta(a0, a1);
            float ang = a0 + d * e, r = BodegaLayout.Ring - 34f * Mathf.Sin(Mathf.PI * e);
            pt = new Vector2(BodegaLayout.CenterX + Mathf.Cos(ang) * r, BodegaLayout.CenterY + Mathf.Sin(ang) * r);
            _lastFlyPt = pt;
            return true;
        }

        private void BeamStart(int hatch)
        {
            _beamOn = true;
            _beamHatch = hatch;
            _beamAt = GameClock.Time;
        }

        private void BeamStop()
        {
            if (!_beamOn) return;
            _beamOn = false;
            _beamOffAt = GameClock.Time;
        }

        private void CrateStart(CrateMode mode, int hatch, float seconds)
        {
            _crateMode = mode;
            _crateHatch = hatch;
            _crateAt = GameClock.Time;
            _crateDur = seconds;
        }

        // ------------------------------------------------------------------ movimiento con flow: todo sigue a su objetivo con un resorte

        private void StepMotion(float dt)
        {
            var r = _robot;
            if (!Motion.Decorative)
            {
                r.X = r.Tx; r.Y = r.Ty; r.Ang = r.Ta;
                r.Vx = r.Vy = r.Va = 0f;
                r.Ant = r.Sw = r.Vant = r.Vsw = 0f;
                r.Sq = 0f;
            }
            else
            {
                BodegaMotion.Spring(ref r.X, ref r.Vx, r.Tx, dt, BodegaMotion.RobotFreq, BodegaMotion.RobotZeta);
                BodegaMotion.Spring(ref r.Y, ref r.Vy, r.Ty, dt, BodegaMotion.RobotFreq, BodegaMotion.RobotZeta);
                float d = BodegaMotion.AngleDelta(r.Ang, r.Ta);
                BodegaMotion.Spring(ref r.Ang, ref r.Va, r.Ang + d, dt, BodegaMotion.TurnFreq, BodegaMotion.TurnZeta);
                BodegaMotion.Spring(ref r.Ant, ref r.Vant, -r.Vx * 0.006f - r.Va * 0.06f, dt, BodegaMotion.AntennaFreq, BodegaMotion.AntennaZeta);       // la antena se queda atrás y rebota
                BodegaMotion.Spring(ref r.Sw, ref r.Vsw, -r.Vx * 0.008f, dt, BodegaMotion.SwayFreq, BodegaMotion.SwayZeta);                               // la caja colgando se mece
                r.Sq *= Mathf.Pow(0.02f, BodegaMotion.ClampDt(dt));
            }
            foreach (var v in _hv)
            {
                if (!v.Root.gameObject.activeSelf) continue;
                if (!Motion.Decorative) { v.Open = v.Target; v.Vo = 0f; }
                else BodegaMotion.Spring(ref v.Open, ref v.Vo, v.Target, dt, BodegaMotion.DoorOpenFreq, v.Target > v.Open ? BodegaMotion.DoorOpenZeta : BodegaMotion.DoorCloseZeta);
                if (v.Target <= 0f && v.Open < 0.01f)
                {
                    v.Open = 0f;
                    v.Vo = 0f;
                    v.ShowAt = -1f;
                }
            }
        }

        // ------------------------------------------------------------------ dibujo por cuadro: la bodega

        private static Vector2 Rot2(float ang) => new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

        /// <summary>Una línea entre dos puntos del boceto (cápsula con la imagen redondeada), ancho en dp.</summary>
        private static void SetLine(Image img, Vector2 a, Vector2 b, float widthDp, Color color)
        {
            var ua = B(a.x, a.y);
            var ub = B(b.x, b.y);
            var d = ub - ua;
            float len = d.magnitude;
            var rt = img.rectTransform;
            rt.anchoredPosition = (ua + ub) * 0.5f;
            rt.sizeDelta = new Vector2(len + widthDp * Su, widthDp * Su);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            SetRadius(img, widthDp * Su / 2f);
            img.color = color;
        }

        private void AnimateBoard(float now)
        {
            if (!_boardRoot.gameObject.activeSelf) return;
            bool deco = Motion.Decorative;
            int places = Places;

            // los remaches giran con la bodega
            for (int k = 0; k < _rivets.Length; k++)
            {
                bool on = k < places;
                _rivets[k].gameObject.SetActive(on);
                if (!on) continue;
                float a = -Mathf.PI / 2f + (k + 0.5f) * 2f * Mathf.PI / places + _rot;
                _rivets[k].rectTransform.anchoredPosition = B(BodegaLayout.CenterX + Mathf.Cos(a) * (BodegaLayout.Ring + 37f), BodegaLayout.CenterY + Mathf.Sin(a) * (BodegaLayout.Ring + 37f));
            }

            // la esclusa: gira con la bodega, destella cuando entra o sale carga, y deja asomar la carga que llega
            var lk = LockPt();
            _lock.Root.anchoredPosition = B(lk.x, lk.y);
            _lock.Seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -_rot * Mathf.Rad2Deg);
            float flash = Mathf.Max(1f - (now - _lockInAt) / 0.6f, 1f - (now - _lockOutAt) / 0.6f);
            _lock.Glow.color = new Color(Gold.r, Gold.g, Gold.b, deco && flash > 0f ? Mathf.Clamp01(flash) : 0f);
            bool peek = _flyActive && now < _flyStartsAt;
            _lock.Peek.gameObject.SetActive(peek);
            if (peek)
            {
                _lock.Peek.sprite = BodegaSprites.Object(_flyObjId);
                float k = deco ? Mathf.Min(1f, BodegaMotion.EaseBack((now - _lockInAt) / 0.33f)) : 1f;
                _lock.Peek.rectTransform.sizeDelta = Vector2.one * (34f * Su * Mathf.Max(0.01f, k));
            }

            for (int i = 0; i < _hv.Length; i++) AnimateHatch(_hv[i], i, now, deco);
            AnimateCrate(now, deco);
            AnimateBeam(now, deco);
            AnimateFly(now, deco);
            AnimateRobot(now, deco);
        }

        private void AnimateHatch(HatchView v, int i, float now, bool deco)
        {
            bool active = _order != null && i < _order.Hatches;
            if (v.Root.gameObject.activeSelf != active) v.Root.gameObject.SetActive(active);
            if (!active) return;
            var pt = HatchPt(i);
            float sx = 0f;
            if (deco && now - v.ShakeAt < 0.42f) sx = Mathf.Sin((now - v.ShakeAt) * 1000f / 28f) * 4f * (1f - (now - v.ShakeAt) / 0.42f);
            float pr = now - v.PressAt < 0.18f ? 1f - 0.06f * (1f - (now - v.PressAt) / 0.18f) : 1f;
            v.Root.anchoredPosition = B(pt.x + sx, pt.y);
            v.Root.localScale = Vector3.one * pr;

            // brillo (pista o caja recién movida)
            float g = now - v.GlowAt < 0.9f ? 1f - (now - v.GlowAt) / 0.9f : 0f;
            if (_hint == i) g = Mathf.Max(g, deco ? 0.6f + 0.4f * Mathf.Sin(now * 1000f / 120f) : 0.85f);
            v.Glow.color = new Color(Gold.r, Gold.g, Gold.b, Mathf.Clamp01(g));

            float k = v.Open;
            SetKind(v, k <= 0.001f ? 2 : (v.Obj_ >= 0 || v.ShowAt >= 0f ? 1 : 0));
            bool showObj = k > 0f && v.Obj_ >= 0;
            v.Obj.gameObject.SetActive(showObj);
            if (showObj)
            {
                v.Obj.sprite = BodegaSprites.Object(v.Obj_);
                float s = v.ShowAt >= 0f ? Mathf.Min(1.08f, BodegaMotion.EaseBack((now - v.ShowAt) / BodegaMotion.ObjectPopSeconds)) : 1f;
                float fl = v.ShowAt >= 0f && deco ? Mathf.Sin((now - v.ShowAt) * 1000f / 260f) * 1.5f : 0f;
                v.Obj.rectTransform.sizeDelta = Vector2.one * (40f * Su * s * Mathf.Clamp(k, 0.01f, 1f));
                v.Obj.rectTransform.anchoredPosition = new Vector2(0f, -fl * Su);
            }
            v.Empty.gameObject.SetActive(k > 0.6f && v.Obj_ < 0 && now - v.RevealAt < 1.5f);

            // las dos hojas de la puerta se abren hacia los lados (la junta va hacia el centro de la bodega)
            bool leaves = k < 1.12f;
            v.Leaves.gameObject.SetActive(leaves);
            if (leaves)
            {
                float ang = Mathf.Atan2(pt.y - BodegaLayout.CenterY, pt.x - BodegaLayout.CenterX);
                v.Leaves.localRotation = Quaternion.Euler(0f, 0f, -ang * Mathf.Rad2Deg);
                float off = Mathf.Max(0f, k) * BodegaLayout.HatchR * 1.04f * Su;
                v.LeafA.rectTransform.anchoredPosition = new Vector2(0f, off);
                v.LeafB.rectTransform.anchoredPosition = new Vector2(0f, -off);
                float seam = k > 0.02f && k < 0.35f ? 1f - k / 0.35f : 0f;          // la junta brilla un instante al empezar a abrirse
                v.Seam.color = new Color(1f, 231f / 255f, 168f / 255f, seam);
            }
        }

        private void AnimateCrate(float now, bool deco)
        {
            bool flight = _crateMode == CrateMode.Pull || _crateMode == CrateMode.Push;
            bool carry = _crateMode == CrateMode.Carry;
            _crateFlight.gameObject.SetActive(flight);
            _crateFlightGlow.gameObject.SetActive(flight && deco);
            _crateCarried.gameObject.SetActive(carry);
            _crateCarriedGlow.gameObject.SetActive(carry && deco);
            _rope.gameObject.SetActive(carry);
            var r = _robot;
            if (flight)
            {
                float k = deco ? Mathf.Clamp01((now - _crateAt) / Mathf.Max(0.01f, _crateDur)) : 1f;
                float e = BodegaMotion.EaseInOut(k);
                var p = HatchPos(_crateHatch);
                var c = new Vector2(r.X, r.Y + 38f);
                Vector2 at;
                float sc;
                if (_crateMode == CrateMode.Pull) { at = p + (c - p) * e; sc = 0.5f + 0.5f * BodegaMotion.EaseBack(k); }
                else { at = c + (p - c) * e; sc = 1f - 0.6f * e; }
                _crateFlight.rectTransform.anchoredPosition = _crateFlightGlow.rectTransform.anchoredPosition = B(at.x, at.y);
                _crateFlight.rectTransform.localScale = Vector3.one * sc;
                _crateFlightGlow.rectTransform.localScale = Vector3.one * sc;
            }
            if (carry)
            {
                float cx = r.X + Mathf.Sin(r.Sw) * 34f, cy = r.Y + Mathf.Cos(r.Sw) * 34f + 4f;
                SetLine(_rope, new Vector2(r.X, r.Y + 18f), new Vector2(cx, cy - 12f), 2f, new Color(191f / 255f, 233f / 255f, 255f / 255f, 0.6f));
                _crateCarried.rectTransform.anchoredPosition = _crateCarriedGlow.rectTransform.anchoredPosition = B(cx, cy);
                _crateCarried.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -r.Sw * 0.8f * Mathf.Rad2Deg);
            }
        }

        /// <summary>El haz del robot: crece con suavidad (0,26 s), late, lleva 3 chispas y se apaga en 0,22 s sin cortarse. Sigue a la escotilla o a la carga que viaja.</summary>
        private void AnimateBeam(float now, bool deco)
        {
            bool fading = !_beamOn && deco && now - _beamOffAt < BodegaMotion.BeamFade;
            bool on = _beamOn || fading;
            _beamSoft.gameObject.SetActive(on);
            _beamCore.gameObject.SetActive(on);
            foreach (var s in _beamSparks) s.gameObject.SetActive(on && deco);
            if (!on) return;
            if (_beamOn)
            {
                if (_beamHatch >= 0) _beamLastEnd = HatchPos(_beamHatch);
                else if (FlyPoint(now, out var pt, out _)) _beamLastEnd = pt;
            }
            var from = new Vector2(_robot.X, _robot.Y);
            float k = _beamOn ? (deco ? BodegaMotion.EaseOut((now - _beamAt) / BodegaMotion.BeamGrow) : 1f) : 1f;
            float a = _beamOn ? 1f : 1f - (now - _beamOffAt) / BodegaMotion.BeamFade;
            var to = from + (_beamLastEnd - from) * k;
            float w = 1f + 0.15f * Mathf.Sin(now * 1000f / 60f);
            SetLine(_beamSoft, from, to, 16f * w, new Color(BeamSoft.r, BeamSoft.g, BeamSoft.b, BeamSoft.a * a));
            SetLine(_beamCore, from, to, 3f, new Color(BeamCore.r, BeamCore.g, BeamCore.b, BeamCore.a * a));
            if (deco)
                for (int j = 0; j < _beamSparks.Length; j++)
                {
                    float f = (((now * 1000f / 420f) + j / 3f) % 1f) * k;
                    var p = from + (_beamLastEnd - from) * f;
                    _beamSparks[j].rectTransform.anchoredPosition = B(p.x, p.y);
                    _beamSparks[j].color = new Color(1f, 1f, 1f, a);
                }
        }

        /// <summary>La carga que viaja de la esclusa a su escotilla; el robot la sigue con la mirada y se acerca un poco (20 % del camino).</summary>
        private void AnimateFly(float now, bool deco)
        {
            bool on = _flyActive && now >= _flyStartsAt && FlyPoint(now, out _, out _);
            _flyObj.gameObject.SetActive(on);
            _flyGlow.gameObject.SetActive(on && deco);
            if (!on) return;
            FlyPoint(now, out var p, out float k);
            _robot.Ta = Mathf.Atan2(p.y - BodegaLayout.CenterY, p.x - BodegaLayout.CenterX);
            _robot.Tx = BodegaLayout.CenterX + (p.x - BodegaLayout.CenterX) * 0.2f;
            _robot.Ty = BodegaLayout.CenterY + (p.y - BodegaLayout.CenterY) * 0.2f;
            _flyObj.sprite = BodegaSprites.Object(_flyObjId);
            _flyObj.rectTransform.anchoredPosition = _flyGlow.rectTransform.anchoredPosition = B(p.x, p.y);
            _flyObj.rectTransform.localRotation = Quaternion.Euler(0f, 0f, deco ? -Mathf.Sin(Mathf.PI * k) * 0.35f * Mathf.Rad2Deg : 0f);
        }

        private void AnimateRobot(float now, bool deco)
        {
            var r = _robot;
            float bob = BodegaMotion.Bob(now, deco);
            float lean = Mathf.Clamp(r.Vx * 0.0028f, -BodegaMotion.MaxLean, BodegaMotion.MaxLean);
            _robotShadow.rectTransform.anchoredPosition = B(r.X, r.Y + 32f);
            _robotShadow.rectTransform.sizeDelta = new Vector2(2f * (22f - bob) * Su, 10f * Su);          // la sombra queda siempre horizontal
            _robotRoot.anchoredPosition = B(r.X, r.Y + bob);
            _robotRoot.localRotation = Quaternion.Euler(0f, 0f, -lean * Mathf.Rad2Deg);
            _robotRoot.localScale = new Vector3(1f + r.Sq, 1f - r.Sq, 1f);

            // el visor mira hacia donde trabaja (se corre hacia ese lado: parece girar en 3D); parpadea cada ~3,6 s
            float look = r.Ang - lean, lx = Mathf.Cos(look) * 9f, ly = Mathf.Sin(look) * 7f;
            var vc = B(BodegaLayout.CenterX + lx * 0.6f, BodegaLayout.CenterY + ly * 0.6f - 2f);
            _visor.rectTransform.anchoredPosition = vc;
            _visor.rectTransform.sizeDelta = new Vector2(2f * (15f - Mathf.Abs(lx) * 0.25f) * Su, 18f * Su);
            float blink = deco && (now * 1000f) % 3600f < 110f ? 0.15f : 1f;
            _eyeL.rectTransform.anchoredPosition = vc + new Vector2(-5f * Su, 0f);
            _eyeR.rectTransform.anchoredPosition = vc + new Vector2(5f * Su, 0f);
            _eyeL.rectTransform.sizeDelta = _eyeR.rectTransform.sizeDelta = new Vector2(6f * Su, 6.4f * Su * blink);
            // la antena con retraso: se queda atrás al moverse y rebota
            _antRoot.localRotation = Quaternion.Euler(0f, 0f, -r.Ant * Mathf.Rad2Deg);
            _antGlow.color = new Color(Gold.r, Gold.g, Gold.b, deco ? 0.5f + 0.5f * Mathf.Sin(now * 1000f / 300f) : 0.5f);
        }

        // ------------------------------------------------------------------ la tarjeta de arriba, el carro, el aviso, la luz que vuela, las chispas, «NUEVO»

        private void AnimateCard(float now)
        {
            if (!_cardLayer.gameObject.activeSelf) return;
            bool wide = _card == CardKind.Ask || _card == CardKind.Store;
            bool deco = Motion.Decorative;
            if (_cardWide != wide) { _cardWide = wide; LayoutCardTexts(); }
            _cardDisc.gameObject.SetActive(wide);
            _cardObj.gameObject.SetActive(wide);
            _cardRim.color = _card == CardKind.Ask ? new Color(Gold.r, Gold.g, Gold.b, 0.7f) : new Color(142f / 255f, 131f / 255f, 216f / 255f, 0.35f);
            if (wide)
            {
                float k = _card == CardKind.Ask && deco ? Mathf.Min(1f, BodegaMotion.EaseBack((now - _cardAt) / 0.32f)) : 1f;
                _cardObj.sprite = _cardObj_ >= 0 ? BodegaSprites.Object(_cardObj_) : null;
                _cardObj.enabled = _cardObj_ >= 0;
                _cardObj.rectTransform.localScale = Vector3.one * Mathf.Max(0.01f, k);
                _cardA.text = _card == CardKind.Ask ? BodegaContract.AskLabel : BodegaContract.StoreLabel;
                _cardB.text = _cardObj_ < 0 ? "" : _card == CardKind.Ask ? BodegaContract.AskText(_cardObj_) : BodegaContract.Capitalized(_cardObj_);
                _cardT.text = _cardS.text = "";
                return;
            }
            _cardA.text = _cardB.text = "";
            switch (_card)
            {
                case CardKind.Watch:
                    _cardT.text = BodegaContract.WatchTitle; _cardT.color = ButtonText;
                    _cardS.text = _order != null ? BodegaContract.ObjectsLine(_order.Objects) : "";
                    break;
                case CardKind.Move:
                    _cardT.text = BodegaContract.MoveTitle; _cardT.color = ButtonText; _cardS.text = BodegaContract.MoveSub;
                    break;
                case CardKind.Spin:
                    _cardT.text = BodegaContract.SpinTitle; _cardT.color = ButtonText; _cardS.text = BodegaContract.SpinSub;
                    break;
                case CardKind.Done:
                    _cardT.text = _cardPerfect ? BodegaContract.PerfectTitle : BodegaContract.DoneTitle;
                    _cardT.color = _cardPerfect ? Mint : WarnText;
                    _cardS.text = BodegaContract.DoneLine(_cardFirst, _order != null ? _order.Objects : 0, _cardPerfect && _recordNow);
                    break;
                default:
                    _cardT.text = _cardS.text = "";
                    break;
            }
        }

        private void AnimateTray(float now)
        {
            if (!_trayLayer.gameObject.activeSelf || _order == null) return;
            bool deco = Motion.Decorative;
            int n = _order.Objects;
            for (int i = 0; i < _tray.Length; i++)
            {
                var v = _tray[i];
                bool on = i < n;
                if (v.Root.gameObject.activeSelf != on) v.Root.gameObject.SetActive(on);
                if (!on) continue;
                bool flying = false;
                foreach (var f in _fly) if (f.On && f.Slot == i) flying = true;
                var fd = i < _found.Count && !flying ? _found[i] : null;
                bool landed = fd != null;
                Color border = landed ? (fd.First ? Mint : new Color(Lavender.r, Lavender.g, Lavender.b, 0.6f)) : new Color(1f, 1f, 1f, 0.12f);
                float w = landed && fd.First ? 3f : 2f;
                v.Border.color = border;
                SetChild(v.Inner.rectTransform, 0f, 0f, BodegaLayout.TrayCircle - 2f * w, BodegaLayout.TrayCircle - 2f * w);
                v.Obj.gameObject.SetActive(landed);
                v.Check.gameObject.SetActive(landed && fd.First);
                if (landed)
                {
                    float la = _landAt[i] > 0f ? now - _landAt[i] : 999f;
                    float b = la < 0.38f && deco ? 1f + 0.22f * Mathf.Sin(Mathf.PI * la / 0.38f) * (1f - la / 0.38f) : 1f;
                    v.Obj.sprite = BodegaSprites.Object(fd.Obj);
                    SetChild(v.Obj.rectTransform, 0f, 0f, 26f * b, 26f * b);
                }
            }
        }

        private void LaunchFlyer(int slot, int obj, int hatch, bool first)
        {
            foreach (var f in _fly)
            {
                if (f.On) continue;
                f.On = true;
                f.Slot = slot;
                f.ObjId = obj;
                f.First = first;
                f.At = GameClock.Time;
                var p = HatchPos(hatch);
                f.From = new Vector2(p.x, p.y);
                f.Out = false;
                f.Obj.sprite = BodegaSprites.Object(obj);
                f.Obj.gameObject.SetActive(true);
                f.Glow.gameObject.SetActive(Motion.Decorative);
                f.Glow.color = first ? new Color(Gold.r, Gold.g, Gold.b, 0.7f) : new Color(200f / 255f, 200f / 255f, 1f, 0.7f);
                return;
            }
        }

        /// <summary>El objeto encontrado viaja por DENTRO del anillo hasta la esclusa, que destella con un soplido (sale de la nave), y de ahí baja en arco al carro (0,95 s en total).</summary>
        private void AnimateFlyers(float now)
        {
            if (_order == null) return;
            bool deco = Motion.Decorative;
            var lk = LockPt();
            foreach (var f in _fly)
            {
                if (!f.On) continue;
                float k = (now - f.At) / (deco ? 0.95f : 0.01f);
                float tx = BodegaLayout.TrayX(f.Slot, _order.Objects), ty = _lay.TrayCenterY;
                if (k >= 1f)
                {
                    f.On = false;
                    f.Obj.gameObject.SetActive(false);
                    f.Glow.gameObject.SetActive(false);
                    _landAt[f.Slot] = now;
                    Emit(new Vector2(tx, ty), f.First ? 14 : 6, 80f, 0.5f, f.First ? Mint : Lavender, 20f);
                    continue;
                }
                Vector2 pos;
                float e;
                if (k < 0.5f)
                {
                    e = BodegaMotion.EaseInOut(k / 0.5f);
                    var mid = (f.From + new Vector2(lk.x, lk.y)) * 0.5f;
                    var ctrl = mid + (new Vector2(BodegaLayout.CenterX, BodegaLayout.CenterY) - mid) * 0.45f;
                    var bp = (1f - e) * (1f - e) * f.From + 2f * (1f - e) * e * ctrl + e * e * new Vector2(lk.x, lk.y);
                    pos = L(bp.x, bp.y);
                }
                else
                {
                    if (!f.Out)
                    {
                        f.Out = true;
                        _lockOutAt = now;
                        PlayClip(BodegaSounds.Whoosh(), 0.5f);
                    }
                    e = BodegaMotion.EaseOut((k - 0.5f) / 0.5f);
                    var start = L(lk.x, lk.y);
                    pos = new Vector2(start.x + (tx - start.x) * e, start.y + (ty - start.y) * e - Mathf.Sin(Mathf.PI * e) * 40f);
                }
                float size = (k < 0.5f ? 34f : 36f - 8f * e) * (k < 0.5f ? _lay.Scale : 1f);
                f.Obj.rectTransform.anchoredPosition = f.Glow.rectTransform.anchoredPosition = P(pos);
                f.Obj.rectTransform.sizeDelta = Vector2.one * (size * _s);
                f.Glow.rectTransform.sizeDelta = Vector2.one * (48f * _s);
                f.Obj.rectTransform.localRotation = Quaternion.Euler(0f, 0f, deco ? -Mathf.Sin(Mathf.PI * (k < 0.5f ? k / 0.5f : e)) * 0.5f * Mathf.Rad2Deg : 0f);
            }
        }

        /// <summary>Lo que dice el aviso de abajo: <paramref name="good"/> = verde, si no ámbar; <paramref name="sub"/> (el truco) va en dorado debajo.</summary>
        private void Tell(string title, string sub, bool good, float seconds)
        {
            _toastTitleFull = title ?? "";
            _toastSubFull = sub ?? "";
            _toastGood = good;
            _toastAt = GameClock.Time;
            _toastSeconds = seconds;
        }

        private void AnimateToast(float now)
        {
            float k = (now - _toastAt) / _toastSeconds;
            bool on = k >= 0f && k < 1f && _toastTitleFull.Length > 0 && _toastLayer.gameObject.activeSelf;
            if (_toastRoot.gameObject.activeSelf != on) _toastRoot.gameObject.SetActive(on);
            if (!on) return;
            _toastGroup.alpha = Mathf.Clamp01(Mathf.Min(k * 8f, (1f - k) * 4f));
            _toastTitle.text = _toastTitleFull;
            _toastSub.text = _toastSubFull;
            _toastTitle.color = _toastGood ? Mint : WarnText;
            _toastRim.color = _toastGood ? new Color(159f / 255f, 245f / 255f, 214f / 255f, 0.5f) : new Color(1f, 214f / 255f, 160f / 255f, 0.5f);
            LayoutToast(false);
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

        private void AnimateIntro(float now)
        {
            if (!_introLayer.gameObject.activeSelf) return;
            bool deco = Motion.Decorative;
            float k = deco ? 1f - Mathf.Pow(1f - Mathf.Clamp01((now - _introAt) / 0.35f), 3f) : 1f;
            _dim.color = new Color(2f / 255f, 3f / 255f, 15f / 255f, 0.78f * k);
            _introGroup.alpha = k;
            // la ilustración: una escotilla que se abre y se cierra con un objeto adentro
            float t = deco ? (Mathf.Sin(now * 1000f / 600f) + 1f) / 2f : 1f;
            int obj = _introKind == Intro.Gira ? 8 : _introKind == Intro.Mueve ? 7 : 1;
            _introObj.sprite = BodegaSprites.Object(obj);
            SetRect(_introObj.rectTransform, _introIllustration.x, _introIllustration.y, 38f * t + 2f, 38f * t + 2f);
            SetRect(_introDoor.rectTransform, _introIllustration.x, _introIllustration.y, Mathf.Max(0.5f, (1f - t) * 2f * (BodegaSprites.HatchR - 1f)), 2f * (BodegaSprites.HatchR - 1f));
            _introTap.color = deco ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f + 0.3f * Mathf.Sin(now * 4f)) : Cyan;
        }

        // ------------------------------------------------------------------ verificación de que lo dibujado se ve

        /// <summary>Para las pruebas: arma un pedido de cada etapa y devuelve lo que NO se vería (pieza apagada, sin imagen o sin opacidad): el casco, la esclusa, las escotillas con su marco, interior, puertas y aro,
        /// el robot, la caja, la tarjeta de arriba, el carro y el aviso. Una pieza horneada sin sprite se dibuja como un cuadrado blanco (lo que pasó con las fichas de Punta el 3-oct).</summary>
        public List<string> AuditVisibility()
        {
            var problems = new List<string>();
            _s = 3f; _playW = 1080f; _playH = 1920f; _logicalH = 640f;
            _lay = BodegaLayout.Compute(_logicalH);
            if (_rng == null) _rng = new System.Random(1);
            var bake = BodegaSprites.Prewarm();
            while (bake.MoveNext()) { }
            AssignSprites();
            void Check(string what, Graphic g, bool needsSprite, bool needsAlpha = true)
            {
                if (g == null) { problems.Add(what + ": no existe"); return; }
                if (!g.gameObject.activeInHierarchy) problems.Add(what + ": apagada");
                else if (needsAlpha && g.color.a <= 0.01f) problems.Add(what + ": transparente");
                if (needsSprite && g is Image im && im.sprite == null) problems.Add(what + ": sin imagen");
            }
            Check("casco", _hull, true);
            Check("esclusa.vidrio", _lock.Body, true);
            Check("esclusa.aro de sello", _lock.Seal, true);
            Check("robot.cuerpo", _robotBody, true);
            Check("robot.visor", _visor, true);
            Check("robot.ojo izquierdo", _eyeL, false);
            Check("robot.ojo derecho", _eyeR, false);
            Check("robot.antena", _antBall, true);
            Check("robot.sombra", _robotShadow, true);
            _toastRoot.gameObject.SetActive(true);
            for (int level = 1; level <= BodegaContract.MaxLevel; level++)
            {
                var o = BodegaContract.Generate(level, _rng);
                SetUpOrder(o);
                if (o.Hatches > HatchPool) problems.Add("etapa " + level + ": " + o.Hatches + " escotillas y solo hay " + HatchPool);
                if (o.Objects > TrayPool) problems.Add("etapa " + level + ": " + o.Objects + " objetos y el carro solo tiene " + TrayPool + " lugares");
                string tag = "etapa " + level + ".";
                for (int i = 0; i < o.Hatches; i++)
                {
                    var v = _hv[i];
                    Check(tag + "escotilla " + i + ".marco", v.Frame, true);
                    Check(tag + "escotilla " + i + ".interior", v.Interior, true, false);
                    Check(tag + "escotilla " + i + ".aro", v.Rim, true);
                    Check(tag + "escotilla " + i + ".hoja A", v.LeafA, true);
                    Check(tag + "escotilla " + i + ".hoja B", v.LeafB, true);
                    Check(tag + "escotilla " + i + ".brillo", v.Glow, true, false);
                }
                for (int i = 0; i < o.Objects; i++)
                {
                    var v = _tray[i];
                    Check(tag + "carro " + i + ".borde", v.Border, true);
                    Check(tag + "carro " + i + ".fondo", v.Inner, true);
                }
                if (level == 1 || level == BodegaContract.MaxLevel)
                {
                    Check(tag + "tarjeta.fondo", _cardBg, true);
                    Check(tag + "tarjeta.borde", _cardRim, true, false);
                    Check(tag + "aviso.fondo", _toastBg, true, false);
                    for (int obj = 0; obj < BodegaContract.ObjectCount; obj++)
                        if (BodegaSprites.Object(obj) == null) problems.Add("objeto " + obj + ": sin imagen");
                }
            }
            ResetBoard();
            return problems;   // nada se destruye aquí (en modo edición no se puede): quien llama destruye el objeto entero
        }
    }
}
