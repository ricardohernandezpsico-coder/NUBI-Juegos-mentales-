# Documentación de Nubi

Qué hay en esta carpeta y para qué sirve cada cosa. El estado actual del proyecto vive en
[`../CLAUDE.md`](../CLAUDE.md); lo de aquí es el detalle de cada tema.

## Cómo funciona la app

| Documento | De qué trata |
|---|---|
| [`DDA-comun.md`](DDA-comun.md) | El motor de dificultad adaptativa que comparten casi todos los juegos |
| [`dificultad-y-avance.md`](dificultad-y-avance.md) | Los modos Suave / A tu medida / Desafío / Experto y cómo se calcula tu avance |
| [`medidas-juegos-estrella.md`](medidas-juegos-estrella.md) | Qué mide de verdad cada juego estrella al final y con qué respaldo |

## Para probar y para decidir

| Documento | De qué trata |
|---|---|
| [`prueba-manual.md`](prueba-manual.md) | Lista para recorrer la app completa en el teléfono antes de marcar una versión |
| [`nombre-marca-y-riesgos.md`](nombre-marca-y-riesgos.md) | Revisión de nombres y de patentes: por qué Nubi y qué reglas no romper |
| [`ideas-guardadas.md`](ideas-guardadas.md) | Ideas en espera, que no se implementan hasta que se pidan |
| [`plan-mejoras-arquitectura.md`](plan-mejoras-arquitectura.md) | Revisión de la arquitectura: qué se hizo y qué falta, con pasos concretos |
| [`diseno-lluvia-de-meteoros.md`](diseno-lluvia-de-meteoros.md) | Diseño del próximo juego de Lenguaje (decisión léxica): reglas, dificultad, medidas, léxico y ciencia; maqueta en `previews/meteoros.png` |
| [`diseno-verdad-o-disparate.md`](diseno-verdad-o-disparate.md) | Diseño del segundo juego estrella de Lenguaje (verificación de frases «¿verdad o disparate?»): reglas, dificultad por forma de la frase, medidas, generador de frases y ciencia; maqueta en `previews/disparate.png` |
| [`diseno-cosecha-de-palabras.md`](diseno-cosecha-de-palabras.md) | Diseño (aprobado el 1-oct) del tercer juego estrella de Lenguaje, formar palabras con 7 letras en órbita: reglas, rondas, dificultad, medidas y ciencia; maqueta en `previews/cosecha.png` |
| [`diseno-estrella-intrusa.md`](diseno-estrella-intrusa.md) | Diseño del quinto juego de Lenguaje, «La estrella intrusa» (elegir la palabra que no pertenece a una constelación de cinco): reglas, dificultad por tipo de relación, banco de grupos con verificador de unicidad, medidas y «Tu cielo»; maqueta en `previews/intrusa.png` |
| [`intrusa-muestra.md`](intrusa-muestra.md) | 60 grupos del generador `tools/intrusa/` (10 por tipo) con intrusa, regla, opciones del bonus y la pareja de las trampas, para que Ricardo marque los dudosos |
| [`cosecha-muestra.md`](cosecha-muestra.md) | 10 rondas de Cosecha de palabras (letras, estrella y todas sus palabras comunes) del generador `tools/cosecha/` para que Ricardo marque las que sobren o falten |
| [`frases-muestra-disparate.md`](frases-muestra-disparate.md) | 100 frases al azar del generador (`tools/frases/`) para que Ricardo marque las dudosas o raras |
| [`analisis-competencia.md`](analisis-competencia.md) | Lumosity, NeuroNation y Peak vistos en capturas: qué tomar, qué no copiar y en qué orden |
| [`auditoria-30-sep.md`](auditoria-30-sep.md) | Auditoría general: áreas (6 → 4), fondo más oscuro, ciencia, marcas y lista para Google Play |

## Historia

| Documento | De qué trata |
|---|---|
| [`historial-desarrollo.md`](historial-desarrollo.md) | Diario de decisiones y cambios (21 al 26 de septiembre) |
| [`NeuroVida_PRD_v1.0.pdf`](NeuroVida_PRD_v1.0.pdf) | Documento de producto inicial (v1.0), de cuando la app aún se llamaba NeuroVida |

## Imágenes

Las vistas previas del diseño y del arte están en [`previews/`](previews/README.md), agrupadas por tema.
