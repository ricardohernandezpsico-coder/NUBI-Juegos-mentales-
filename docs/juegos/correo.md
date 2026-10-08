# Correo Estelar (`correo`) — ficha técnica

> **RENOVADO el 8-oct-2026 como «La estación de correo».** La ficha vigente (cómo funciona, etapas, reglas, medidas, tutorial y cómo quedó hecho) es [`docs/diseno-correo-estacion.md`](../diseno-correo-estacion.md); la medida está en
> [`docs/medidas-juegos-estrella.md`](../medidas-juegos-estrella.md) (§ «Correo Estelar»). Lo de abajo describe el VUELO anterior (28-sep) y se conserva solo como historia: ya no existe en el código (ni el pilotaje, ni los planetas
> a los costados de la ruta, ni los asteroides, ni el escudo, ni la radio cada 30 s).

> Ficha técnica movida TAL CUAL desde `CLAUDE.md` el 2-oct (CLAUDE.md quedó como índice). Lo que manda es el código; esta ficha explica cómo y por qué.

**Correo Estelar** (`Games/Correo/`, id `correo`, dominio MEMORIA; 28-sep, elegido por Ricardo entre 4 propuestas con el
movimiento de Piloto Estelar; maqueta aprobada antes de programar: `tools/art-preview/correo.py` →
`docs/previews/correo-estelar.png`): MEMORIA PROSPECTIVA, acordarse de hacer algo en el momento justo (Rummel y
Kvavilashvili, 2023). Nadie en la competencia la mide.
- Hoja de ruta antes de salir (sin recuadros: ícono + texto; "durante el vuelo no los verás"; truco de intención de
  implementación, Chen et al. 2015) y botón "¡A volar!". Vuelo de 150 s con la ruta de Piloto (reusa `PilotContract`:
  ancho, curvas, velocidad × 0,85; sin piloto automático) y sobres sobre la ruta (+10, tarea en curso).
- Por lugar: planetas-puerto de Tráfico (`TrafficSprites.Port`) a los costados de la ruta; tocar el del color del
  encargo = el paquete vuela en arco, ✓ "¡Entregado!"; otro = ✗ "No es de tu encargo"; si se va = "Se fue sin su
  paquete" (suave). Por hora: radio abajo a la derecha, ventana ±5 s ("¡Aviso recibido!" / "Aún no es la hora" / "Ya
  pasó la hora"; al cerrarse sin aviso, "Se pasó la hora del aviso"); reloj tapado arriba a la derecha, se destapa 1,6 s.
- Reglas y pruebas: `MailContract` / `MailContractTests` (6): 10 niveles; los dos tipos de encargo desde el nivel 1;
  2 colores desde el 4; radio cada 30 s (25 desde el 5, 20 desde el 8); parecidos desde el 2 (15% → 45%; pares
  coral/amarillo → naranjo, celeste → menta, lila → rosado); ~23% de planetas del encargo, nunca dos seguidos; un
  planeta cada 3,6 → 2,4 s. DOS dificultades, como en Piloto: la de encargos (`stepUp` 0.5, cada entrega, planeta
  perdido, error u hora con o sin aviso; los encargos se fijan al salir) y la de pilotaje (`stepUp` 0.14, ventanas de
  1,5 s con ≥ 85% en la ruta y sin chocar: velocidad, curvas, ancho, asteroides).
- Versión "con aceleración" (28-sep, Ricardo lo probó: "demasiado lento, monótono, sin dificultad; a nivel gráfico,
  excelente"; y un punto blanco tapaba "¡A volar!" = la píldora de estado vacía, ahora aparece al despegar): el vuelo
  tiene 3 TRAMOS (aviso "Tramo 2 de 3 · ¡La ruta se acelera!", destello e hiperespacio); a lo largo del vuelo la
  velocidad sube × 1 → × 1,5, las curvas × 1,5, el ancho × 0,85 y los planetas y asteroides salen 30-35% más seguido.
  **Asteroides** sobre la ruta (cargados a un lado, siempre hay por dónde pasar; `SymbolSprite` Asteroid): chocar =
  golpe sordo, la nave tiembla, "¡Asteroide!" (no quita encargos). El sobre suena siempre igual (la escala que subía
  y bajaba irritaba).
- **Escudo** (28-sep, idea de Ricardo: "que el cohete se vaya dañando", para cuidar la nave): 3 segmentos arriba a la
  izquierda (`ShipShield` en el contrato, 1 prueba). Cada choque rompe uno (cristal que se quiebra) y la nave se ve
  dañada (`MailSprites.ShipDamage`: grietas; con 1 segmento, humo); 20 s sin chocar reparan uno. Sin escudo:
  "¡Reparación de emergencia!" 3,5 s (nave a la mitad de velocidad y parpadeando, sin sobres, los asteroides la
  atraviesan, el DDA de pilotaje no cuenta) y sigue con 1 segmento. NUNCA termina el vuelo: los encargos necesitan los
  150 s para medirse igual (decisión razonada con Ricardo). Al final: "Nave intacta el N% del vuelo · reparaciones"
  (`Mail.shipMessage`; telemetría `mail_hull_intact_pct / mail_emergencies`). Lámina `docs/previews/correo-escudo.png`.
- Medida: **tu memoria para lo pendiente** (por lugar / por hora, `FilledSlots`), errores, **el reloj** (miradas y
  cuántas en el último 30% del intervalo) y lugar contra hora con consejo (`data/Mail.kt`, 4 pruebas). Telemetría
  `mail_*` → `GamePlayResult.mail*` → `GameResultScreen` (más "esquivaste N de M asteroides").
- Arte `MailSprites` (sobre, paquete, radio, reloj tapado/destapado), sonidos `MailSounds` (marimba y campanas: sobre,
  entrega, error, perdido, radio a tiempo/destiempo, tic-tac del reloj); muestra `docs/previews/correo-sonidos.wav`.
  Sin probar en el teléfono.
