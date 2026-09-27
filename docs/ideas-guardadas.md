# Ideas guardadas (para más adelante)

Ideas conversadas con Ricardo que quedan en espera: NO implementar hasta que él lo pida.

## Rangos de tripulación y "Tu astronauta" (27-sep)

Origen: Ricardo propuso cambiar las ligas de metales (Bronce, Plata, Oro...) por nombres ligados al tema espacial
("cabo estelar", "capitán estelar"). Le gustó la propuesta de rangos y sumó una idea clave: la persona debe poder
**identificarse** con algo propio (un color, un avatar), porque a algunas personas les cuesta asumir ciertos roles o
les causa rechazo ver todo en colores con los que no se sienten representadas.

### 1. Rangos (reemplazan a las ligas actuales; misma mecánica de trofeos)

| Hoy (`RankTier`) | Nuevo rango | Idea |
|---|---|---|
| Bronce | **Cadete** | Recién llegas a la academia |
| Plata | **Tripulante** | Ya viajas en la nave |
| Oro | **Astronauta** | Sales al espacio |
| Platino | **Navegante** | Trazas las rutas |
| Esmeralda | **Comandante** | Diriges tu propia nave |
| Diamante | **Almirante** | Diriges una flota |
| Maestro | **Leyenda Estelar** | Tu nombre queda en las estrellas |

- Nombres que sirven igual con "el" y "la" (la app no pregunta género y no debe adivinarlo). Evitar
  "capitán/capitana". Si Ricardo lo prefiere, "Cabo" también es neutro (podría reemplazar a Tripulante).
- En pantalla, corto ("Navegante"); en ascensos, completo ("¡Ahora eres Navegante Estelar!").
- Insignias de arcilla (parches de misión) en vez de escudos de metal, con un dibujo que crece: estrella, cohete,
  casco, constelación, nave con alas, flota, sol con corona.
- Estrellas en vez del número de división ("Plata 3" no se entiende): la insignia se llena de estrellas y con 5 se
  asciende ("Navegante ★★★☆☆ · te faltan 2 para Comandante").
- Técnico: los ascensos guardados en `league_events` usan el nombre del enum (`PLATA`, `ORO`...) y hay logros como
  `general_oro`: al renombrar, traducir los nombres viejos al leerlos (nada se pierde; la liga sale de los trofeos).
  Toca `Models.kt`, `LeagueShield`, `LeaguePromotionOverlay`, `ShareCard`, `SpaceSections`, `Achievements`,
  onboarding y sus pruebas.

### 2. "Tu astronauta" (avatar propio)

- Un astronauta de arcilla **con el visor puesto**: sin cara, sin género, edad ni tono de piel; cualquiera se ve ahí.
- Se crea en un paso rápido del onboarding y se cambia en Perfil: color del traje (color favorito), parche del pecho
  (estrella, corazón, flor, rayo, planeta, nota musical, huella...), color del visor; más adelante un compañero
  desbloqueable con logros (gato espacial, robot...).
- **El color elegido pasa a ser el acento de la app** (insignia, camino del día, tarjeta para compartir), cuidando
  el contraste; el cielo nocturno se mantiene como base.
- Los rangos se ven en el astronauta: insignia en el hombro y el traje gana detalles al ascender (mochila
  propulsora, franjas de mando, halo de estrellas en Leyenda).
- Aparece grande en Perfil, chico en Hoy, en la celebración de ascenso y en la tarjeta para compartir.
- Respaldo: personalizar el avatar aumenta la identificación, la motivación intrínseca y la persistencia (Birk,
  Atkins, Bowey y Mandryk, CHI 2016); encaja con la teoría de la autodeterminación (autonomía, competencia,
  pertenencia).
- De paso: revisar los textos de la app y pasar a formas neutras los que tengan género ("¡Estás listo!" →
  "¡Todo listo!").

### Orden sugerido cuando se retome

1. Rangos + insignias + estrellas (primero una lámina para aprobar).
2. Tu astronauta (lámina → onboarding, Perfil, color de acento).
3. Desbloqueables (parches y compañeros por logros y rangos).
