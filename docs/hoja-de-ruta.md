# Hoja de ruta (aprobada por Ricardo el 8-oct-2026)

Reemplaza a la del 30-sep. Lo que manda es `CLAUDE.md` y el código; esto ordena lo que viene y en qué orden. Cada etapa termina cuando sus tareas están hechas, probadas y verificadas; no se empieza la siguiente por entusiasmo.

## Origen y roles

- **Origen**: la auditoría externa del 8-oct (Fable; el informe está fuera del repositorio y no se cita aquí). De ahí salen el orden de las etapas y la lista de «Lo que NO hacemos ahora».
- **Roles**: Opus coordina y diseña; Sonnet es el único que toca código; Fable es segunda opinión crítica, en solo lectura. Ricardo decide y prueba en el teléfono.

## Dónde estamos (8-oct)

19 juegos en 4 áreas. Tienen tutorial guiado 11 (`UnityGameLauncher.TUTORIAL_GAMES`); faltan 8: Piloto, Satélites, Radar, Rumbo, Acoplamiento, ¿Verdad o disparate?, Cosecha y La estrella intrusa. Los retirados están en `docs/juegos/descartados.md`.

| Área | Juegos |
|---|---|
| Memoria | Constelaciones, Rastro de luz, Bodega de carga, Rumbo a Casa, Correo Estelar («La estación de correo») |
| Atención | Tinta o Palabra, Piloto Estelar, Freno de Emergencia, Satélites, Radar («Rescate relámpago») |
| Razonamiento | Acoplamiento, Carga exacta, Aterrizaje Lunar, Engranajes («Taller de reparación») |
| Lenguaje | En la punta de la lengua, Lluvia de meteoros, ¿Verdad o disparate?, Cosecha de palabras, La estrella intrusa |

## Etapa 0 — Limpiar y destrabar

- **Hecho (Tarea 51)**: textos sin promesas de salud (guardia `VisibleTextsGuardTest`), sin percentiles ni varios perfiles, licencia OFL completa y documentos al día.
- **Hecho (Tarea 52)**: el camino diario se elige por avance real y con variedad (`data/DailyPath.kt`: 3 juegos de 3 áreas, el área que quedó fuera ayer entra hoy, sin repetir los de ayer y anteayer, sin juegos que la persona nunca jugó y no tienen tutorial, determinístico por fecha); el día en que se completa el Primer vuelo, ese día queda cumplido con el vuelo (cuenta para la racha, la meta semanal y los desafíos de días, sin duplicar puntos ni partidas), y «dominio» pasó a «área» en lo que se ve.
- **Falta**: capturas reales de los juegos aprobados (`--capturas`, hoy solo Correo) y tutoriales guiados de Cosecha de palabras, ¿Verdad o disparate? y La estrella intrusa.

## Etapa 1 — Atención

Cada juego sale con su tutorial (Freno ya lo tiene; los otros tres lo reciben aquí). Orden:

1. **Freno de Emergencia**: el SSRT (lo que tarda la persona en frenar) sale del puntaje y queda solo como medida; el velocímetro muestra rangos, no un número exacto; las medidas del final salen del promedio de varias partidas, no de una sola.
2. **Satélites**: probarlo con personas y decidir si sigue o se retira.
3. **Piloto Estelar y Rescate relámpago**: cambios para esquivar patentes (`docs/nombre-marca-y-riesgos.md`). **Pendiente de aprobación de Ricardo** antes de programar.

## Etapa 2 — Memoria y razonamiento

- **Rumbo a Casa**: antes de pulirlo, probar con una persona mayor si marea y si el dial se entiende.
- **Acoplamiento** y **Aterrizaje Lunar**: revisión y, donde falte, tutorial (Aterrizaje ya lo tiene; Acoplamiento y Rumbo, no).
- Con esto los 19 juegos tienen tutorial y la regla del camino diario «nunca un juego sin tutorial que nunca se jugó» queda inerte.

## Etapa 2b — Probar con personas

- Una versión firmada y optimizada (no la de depuración).
- **8 a 12 personas de 60 años o más, durante 14 días**; que no sean pacientes de Ricardo.
- Qué se mira: qué entienden, qué abandonan y si vuelven. Con lo aprendido se ajusta el DDA (calibrarlo con datos reales) y se decide qué juegos siguen.

## Etapa 3 — Avance de fondo

- **Avance**: dos números visibles, logros por conducta (lo que la persona hace, no cuánto rinde), meta semanal como número principal y racha que perdona.
- Revisar los juegos del Primer vuelo (y de la evaluación inicial) y el re-chequeo mensual del punto de partida.
- Marcas ✓/✗ de arcilla sobre las respuestas: el ✗ en aspa diagonal, no en cruz (`docs/simbolos-neutros.md`).

## Etapa 4 — Publicar

Solo cuando la app esté casi lista.

- **Abogado y especialista en lanzamiento.**
- **Marca**: búsqueda oficial (clases 9 y 41) y `applicationId` propio (cambiarlo después = app nueva).
- **Ficha de la tienda sin promesas de salud.** Declaración de salud de Google Play («Mental Acuity»); privacidad y seguridad de datos.
- Página «La ciencia de Nubi» (qué mide cada medida y qué no).
- Firma de lanzamiento, i18n completo (hoy `ui/i18n/AppStrings` cubre pocos textos) y **prueba cerrada**.
- **Países**: evaluar empezar por Chile y Latinoamérica.

## Etapa 5 — Solo si la gente vuelve (retención a 30 días)

- Un juego nuevo: Torre de Lunas, o razonamiento con patrones a la vista.
- Cuentas y nube (servidores).

## Lo que NO hacemos ahora

- Juegos nuevos antes de la prueba con personas.
- Percentiles ni comparaciones con una referencia supuesta.
- «Entrenamiento cognitivo» (ni «cerebro», ni promesas de salud) en ningún texto.

Las ideas en espera (rangos de tripulación, «Tu astronauta») están en `docs/ideas-guardadas.md`.

## Candidatos de juegos, para después de la prueba

Tareas clásicas con décadas de literatura pública; cada una pasa por la revisión de patentes de `docs/nombre-marca-y-riesgos.md` ANTES de programarse; nombre, arte y sonidos propios, táctiles y sin voz.

- **Razonamiento**: Torre de Lunas (planificar; Torre de Londres) · Matriz Perdida (patrones generados por programa, sin copiar ítems de ningún test).
- **Cálculo**: ¿Cuántas estrellas? (comparar cantidades sin contar) · Balanza de carga.
- **Velocidad** (el área más patentada por la competencia, sobre todo Posit): Decodificador (símbolos propios con su clave) · Unir la constelación (tarea de trazar caminos, 1944) · La nave distinta (búsqueda visual).
- **Lenguaje**: Frase rota (ordenar una frase) · Sinónimos en órbita. Dependen del idioma (listas de palabras por idioma).

## Repositorios externos

Regla de Ricardo (30-sep): si aparece un repositorio de GitHub que pueda potenciar la app, se le comenta y ÉL decide si
se agrega. Descartados: Zenject / Extenject (inyección de dependencias: el original sin cambios desde 2021; obligaría a
reescribir los 20 juegos, trae riesgos con IL2CPP en Android y los juegos ya están separados en reglas + pantalla) y
awesome-unity (es una lista de enlaces, archivada en enero de 2025; sirve solo para consultar).
