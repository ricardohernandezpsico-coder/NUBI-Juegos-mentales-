# Modelo de negocio y enganche: base Duolingo

**Estado:** idea en estudio, NO implementar hasta que Ricardo lo pida. Escrito el 14-oct-2026 a partir de la conversación de Ricardo con Opus y de la segunda opinión de Fable ([`auditoria-fable/negocio-duolingo.md`], fuera del repo). Ricardo lo retomará con más investigación.

## Decisiones de Ricardo (14-oct)

- **Base: Duolingo.** «No tenemos que inventar la rueda, sino adaptar lo que existe y mejorarlo». Nubi tiene un personaje, como el búho de Duolingo.
- **Descartado:**
  - el modelo de Calm y Headspace, por caro;
  - Lumosity y Elevate, que Ricardo probó y no le engancharon;
  - los desafíos de Strava, que tiene Premium y no lo motivan;
  - las competencias de Apple Watch;
  - las **licencias institucionales, al 100 %**, porque empujarían a lo clínico;
  - la **publicidad**;
  - los conceptos inventados («archipiélago», «temporadas», «Nubi Plus» como paquete).
- **Sí:**
  - gratis con un plan pagado;
  - prueba gratis;
  - plan mensual y anual;
  - pago único de por vida que se pueda regalar;
  - plan familiar y regalo familiar.
- **Idea de Ricardo para la liga:** cada área aporta puntos a un total semanal, y quien queda arriba sube de nivel con un premio.
- **Avatares retirados** (Tarea 93): basta con la inicial y el nombre.

## Qué se copia de Duolingo y qué se cambia

| Duolingo | En Nubi |
|---|---|
| XP por lección | Puntos por cada partida **jugada**, con su área. La liga muestra cuánto aportó cada una (idea de Ricardo) |
| Meta diaria | La ruta del día (ya existe) |
| Racha | La racha (ya existe) |
| Protector de racha para todos, recargable con gemas | Igual, con la moneda de Nubi. **Gratis para todos** |
| Reparación mensual de la racha (Super) | Igual, en el plan pagado |
| Liga semanal de ~30 personas con ascenso y descenso | Igual. El premio al subir es una decoración para la isla. **Cuenta solo la ruta del día** (o un tope diario), para que pagar no compre puestos |
| Misiones con amigos | Misión **cooperativa** con un amigo o familiar. Va antes que la liga |
| El búho en los avisos | Nubi en los avisos, **sin culpa** |
| Gemas que se ganan jugando | Una moneda que se gana jugando: protector y decoraciones |
| Resumen del año para compartir | «Tu año con Nubi» para WhatsApp |
| Plan familiar de 6 | Igual |

**Lo que NO se copia:**
- **Corazones o vidas**: castigan el error y contradicen «no es un examen».
- **Avisos con culpa.**
- **Puntos por desempeño**: comparar mentes rompe la regla de no comparar con otras personas.
- **Descensos duros**: aquí solo se baja si se deja de jugar.

**Liga opcional**, que se desbloquea tras unos días de uso. Evidencia: Altmeyer et al. 2018, con N=18 y edad media de 84,6, encontró que las personas de 75+ prefieren colaborar antes que competir.

## Gratis y pagado

- **Gratis siempre:**
  - la ruta del día, los 17 juegos en «A tu medida», la isla y la racha;
  - el protector de racha y la moneda;
  - la liga y la misión cooperativa, cuando existan;
  - el resumen para compartir.
- **Pagado:**
  - juego libre fuera de la ruta sin tope; el gratis tiene un **tope con número**, por definir;
  - el Desafío y «Al límite» (antes «Experto») **en cualquier juego y a cualquier hora**. **El Desafío de hoy es la excepción y es GRATIS** (decisión de Ricardo, 25-oct; Tarea 167): uno al día, elegido por Nubi, que se puede repetir si no se supera; no cuenta para el tope de partidas libres (`DailyChallenge.COUNTS_FOR_FREE_CAP = false`). El cobro de Plus no se construye todavía (etapa de publicar, con Play Billing y el abogado: regla permanente 3);
  - la reparación mensual de la racha;
  - decoraciones extra;
  - el plan familiar.
- **Nunca se bloquea la ruta del día.**
- Cómo se activa: lo pagado se activa dentro de la app con Google Play, **nunca desde un servidor**, por la regla permanente 3. Lo revisa el abogado.

## Planes y embudo

- **Prueba gratis larga**, de ~1 mes, con **aviso antes del primer cobro**. Duolingo alargó su prueba y dice que eso subió ingresos y usuarios diarios (carta a accionistas, Q2-2026).
- **Mensual y anual**, con descuento en el anual.
- **De por vida:** que cueste **≥ 3 anuales**, porque la liga y la familia necesitan un servidor que cuesta mantener, o que cubra solo lo que funciona sin servidor. Duolingo no lo vende.
- **Plan familiar de 6 y regalo.** Quien paga suele ser **el hijo o la hija**: son la puerta de entrada, no un extra. En Google Play, regalar se haría con códigos canjeables (supuesto; hay que comprobarlo).
- **Referencias de precio** (EE. UU., solo como orientación): Duolingo ~US$84 al año individual y ~US$120 el familiar (fuente secundaria); Calm, US$499,99 de por vida. Google se queda con ~15 % de las suscripciones.
- **Conversión:** Duolingo Q2-2026 tiene 12,7 M de pagos sobre 140,6 M de usuarios al mes (~9 %). Nubi debería asumir menos.

## Orden de construcción (cuando Ricardo lo pida)

1. **Sin servidor:** puntos por área, protector de racha, moneda, avisos de Nubi, «Tu año con Nubi» y el plan pagado con Google Play.
2. **Con servidor, cuentas y abogado** (al final, como decidió Ricardo): la misión cooperativa, la liga semanal y el plan familiar con su liga familiar.

## Riesgos y reglas

- **Patentes:** sin riesgo concreto encontrado. Fable revisó Google Patents (14-oct): nada sobre ligas con ascenso y descenso, protector de racha ni misiones. Las patentes de Duolingo son de pruebas de idioma. Antes de la liga con servidor, el abogado hace una **libertad de operación** con sinónimos y clases CPC.
- **Marcas:** no usar nombres de Duolingo («Streak Freeze», «Super», «Friends Quests») ni un búho. «Liga», «racha», «protector» y «misión» son genéricos.
- **Sin trampas de diseño:** nada de cobros escondidos, «solo por hoy» falsos ni cortar una partida para vender.
- **Datos personales:** solo el nombre y la inicial, con la opción de ocultarse de la liga.

## Preguntas abiertas (para la próxima vuelta con Fable)

1. El tope gratis de juego libre: ¿cuántas partidas extra al día?
2. Precios en Chile (CLP) y en otros mercados.
3. Los nombres propios: el plan pagado, la moneda, el protector y la reparación.
4. Cuántos protectores se pueden tener a la vez y cuánto cuesta recargarlos.
5. Cómo medir si funciona: en la Etapa 2b (8-12 personas de 60+, 14 días), si vuelven más días, y más adelante pruebas A/B como Duolingo.

## Fuentes

- Duolingo S-1 (2021): las ligas subieron casi 20 % el tiempo de aprendizaje. https://www.sec.gov/Archives/edgar/data/1562088/000162828021013065/duolingos-1.htm
- Carta a accionistas Q2-2026 (SEC). https://www.sec.gov/Archives/edgar/data/0001562088/000162828026053299/q2fy26duolingo6-30x26share.htm
- Blog de Duolingo: rachas, ligas y Friends Quests. https://blog.duolingo.com/how-duolingo-streak-builds-habit/ · https://blog.duolingo.com/duolingo-leagues-leaderboards/ · https://blog.duolingo.com/friends-quests/
- Altmeyer et al. 2018 (75+). https://www.dfki.de/fileadmin/user_upload/import/9808_p453-altmeyer.pdf
- Ensayo 2024 en mayores: gamificación y diferencias individuales. https://www.ncbi.nlm.nih.gov/pmc/articles/PMC11692182/
- Calm, de por vida y familiar. https://support.calm.com/hc/en-us/articles/30958037515419 · https://support.calm.com/hc/en-us/articles/4405882978331
