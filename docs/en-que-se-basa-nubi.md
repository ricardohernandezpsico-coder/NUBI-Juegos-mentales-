# «En qué se basa Nubi»: las fuentes, sus DOI y cómo se comprobaron

Tarea 185, 28-oct-2026. Ficha técnica de la pantalla que abre la losa «En qué se basa Nubi» de Tú (`ui/screens/FuentesScreen.kt`, datos en `data/Fuentes.kt`). El diseño es de Fable (`auditoria-fable/tu-pestana.md` §3); el nombre lo eligió Ricardo el 27-oct (no «La ciencia de Nubi»: sugiere que Nubi está probada, y no tiene estudios propios).

## Qué es y qué no es

De qué estudio sale la TAREA de cada juego, en una línea y con el estudio a un toque. **No** es una prueba de que Nubi funcione y no promete mejorar nada: la pantalla lo dice arriba («Nubi no promete mejorar nada: es un juego.») y al final («Lo que no decimos»: que jugar mejore la memoria o la atención fuera del juego no está demostrado; Simons y otros, 2016; más el aviso de salud de siempre, `HealthNotice`).

- **Por área, una losa por juego:** emblema, nombre, UNA línea de «qué tomamos» y «Autor, año · Ver el estudio ›». Tocarla abre `https://doi.org/<DOI>` en el navegador con `Intent(ACTION_VIEW)`; si el teléfono no tiene navegador, una línea plana lo dice. **La app sigue SIN permiso de internet** (`AndroidManifest.xml`: solo notificaciones y vibración): abrir un enlace lo hace el navegador. Sin `resolveActivity`, así que no hace falta `<queries>` en el manifiesto.
- **Solo autor y año y nuestra línea en español.** Ni el título en inglés ni la revista: `VisibleTextsGuardTest` prohíbe «cognitiv…», «entren…», «cerebro»… en cualquier texto del código, y varios títulos y revistas los traen. La página del DOI los muestra a un toque.
- **Las 17 líneas de «qué tomamos» las APROBÓ Ricardo el 28-oct** (con un cambio: la de Constelaciones dice «…lo que más cambia con los años», porque eso trata Old y Naveh-Benjamin, 2008: el déficit de la memoria asociativa con la edad). `FuentesTest` lo vigila.
- Después de las cuatro áreas: **Cómo se ajusta la dificultad** (Levitt 1971, Kaernbach 1991, Salthouse 1996), **Las palabras** (SPALEX y EsPal viven en «Licencias y créditos») y **Lo que no decimos**.

## Cómo se comprobó cada DOI (28-oct-2026)

Con `https://doi.org/api/handles/<DOI>` (`responseCode 1` = el DOI existe y a dónde redirige) y con Crossref (`https://api.crossref.org/works/<DOI>`: primer autor y año contra la cita que ya trae la app en «Sobre este juego»). **Los 24 responden y los 24 coinciden en autor y año.** Los cinco que Fable había marcado «por confirmar» (Levitt, Kaernbach, Salthouse, Sweller y EsPal) están confirmados. Dos no entran en la pantalla: Sweller 1988 (es de diseño, no del método de dificultad) y EsPal/SPALEX (van en «Licencias»); y **Cooper y Shepard (1973) no tiene DOI** (es un capítulo de libro) y tampoco entra: Acoplamiento cita a Uttal y otros, 2013.

| Área | Juego | Cita (la misma de «Sobre este juego») | DOI | Línea de «qué tomamos» (aprobada el 28-oct) |
|---|---|---|---|---|
| Memoria | Constelaciones | Old y Naveh-Benjamin, 2008 | 10.1037/0882-7974.23.1.104 | Recordar qué va con qué es lo que más cambia con los años. |
| Memoria | Rastro de luz | Baddeley, 2003 | 10.1038/nrn1201 | Guardar un orden un rato mientras lo usamos: la memoria de trabajo. |
| Memoria | Bodega de carga | Postma, Kessels y van Asselen, 2008 | 10.1016/j.neubiorev.2008.05.001 | Recordar dónde quedó cada cosa: la cosa, el lugar y lo que las une. |
| Memoria | Correo Estelar | Hering y otros, 2014 | 10.1007/s00426-014-0566-4 | Acordarse de hacer algo más tarde sostiene el día a día. |
| Atención | Tinta o Palabra | Diamond, 2013 | 10.1146/annurev-psych-113011-143750 | Frenar la lectura automática y cambiar de regla. |
| Atención | Piloto Estelar | Beurskens y Bock, 2012 | 10.1155/2012/131608 | Hacer dos cosas a la vez se vuelve más difícil con los años. |
| Atención | Freno de Emergencia | Verbruggen y Logan, 2008 | 10.1016/j.tics.2008.07.005 | Frenar una acción que ya empezó, justo a tiempo. |
| Atención | Satélites | Trick, Perl y Sethi, 2005 | 10.1093/geronb/60.2.p102 | Seguir varios objetos que se mueven a la vez. |
| Atención | Rescate relámpago | Owsley, 2013 | 10.1016/j.visres.2012.11.014 | Qué tan rápido vemos y reconocemos algo que aparece un instante. |
| Razonamiento | Carga exacta | Reyna y otros, 2009 | 10.1037/a0017327 | Entender las cantidades ayuda a comprender riesgos y a decidir. |
| Razonamiento | Engranajes | Hegarty, 2004 | 10.1016/j.tics.2004.04.001 | Imaginar cómo se mueven las piezas de una máquina antes de moverlas. |
| Razonamiento | Acoplamiento | Uttal y otros, 2013 | 10.1037/a0028446 | Girar formas en la mente: una habilidad espacial. |
| Razonamiento | Aterrizaje Lunar | Schley y Peters, 2014 | 10.1177/0956797613515485 | Ubicar números a ojo guía cómo valoramos montos. |
| Lenguaje | En la punta de la lengua | Shafto y otros, 2007 | 10.1162/jocn.2007.19.12.2060 | Tener una palabra en la punta de la lengua es normal y crece con los años. |
| Lenguaje | Lluvia de meteoros | Verhaeghen, 2003 | 10.1037/0882-7974.18.2.332 | El vocabulario se mantiene, y hasta crece, con los años. |
| Lenguaje | ¿Verdad o disparate? | Xiang, Kramer y Nordmeyer, 2020 | 10.1037/xlm0000851 | Las frases con «no» piden más esfuerzo a todas las personas. |
| Lenguaje | La estrella intrusa | Mirman, Landrigan y Britt, 2017 | 10.1037/bul0000092 | Ordenamos lo que sabemos por tipo de cosa y por lo que va junto. |
| Método | Levitt, 1971 | — | 10.1121/1.1912375 | (la escalera «sube y baja» que converge a un porcentaje de aciertos) |
| Método | Kaernbach, 1991 | — | 10.3758/BF03214307 | (la escalera ponderada: la regla central de la dificultad) |
| Método | Salthouse, 1996 | — | 10.1037/0033-295x.103.3.403 | (el tiempo pesa menos con los años) |
| Lo que no decimos | Simons y otros, 2016 | — | 10.1177/1529100616661983 | (revisión de los estudios en que se apoyan los programas de «entrenamiento mental») |
| Palabras | SPALEX (Aguasvivas y otros, 2018) y EsPal (Duchon y otros, 2013) | — | 10.3389/fpsyg.2018.02156 · 10.3758/s13428-013-0326-1 | (ya en «Licencias y créditos»; no entran en esta pantalla) |

## Pruebas

`FuentesTest` (los 17 juegos de la app y solo ellos; la cita de cada uno es la misma de «Sobre este juego»; los DOI tienen forma `10.xxxx/…` y no se repiten; ninguna cita trae título en inglés ni revista; el encuadre; el navegador y la ausencia de permiso de internet) y `FuentesYContactoTest` (la pantalla: las cuatro áreas y las 17 losas se alcanzan deslizando, cada una abre el SUYO, sin navegador lo dice, con letra 1,3 y 2,0 nada se sale del ancho, «Lo que no decimos» con el aviso de salud al final).

## Pendiente (hoja de ruta, Etapa 4)

El correo de Nubi (`NubiContact.EMAIL`, hoy `null`: la losa dice «Correo por definir»); la política de privacidad (Google Play la pide para todas las apps; no hay página ni enlace todavía); y la declaración de salud de Google Play («Mental Acuity»).
