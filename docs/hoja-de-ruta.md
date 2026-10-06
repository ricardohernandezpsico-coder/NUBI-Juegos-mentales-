# Hoja de ruta (30-sep; tabla de áreas al día el 5-oct)

Acordada con Ricardo el 29/30-sep. Lo que manda es `CLAUDE.md` y el código; esto ordena lo que viene.

## 1. Juegos que faltan (4 áreas desde el 30-sep)

Hoy la app tiene **18 juegos** (es lo que dice `GameRegistry`; los retirados están en `docs/juegos/descartados.md`):

| Área | Hoy | Falta |
|---|---|---|
| Memoria | 4 (Parejas Ocultas, Rastro de luz —antes Secuencia Lumínica—, Rumbo a Casa y Correo Estelar). Retirados: Ruta del Tesoro (4-oct) y **Bitácora de Misión (5-oct)** | 1: un juego de Memoria que reemplace a Bitácora (id nuevo, en otra tarea) |
| Atención y velocidad | 5 (Tinta o Palabra, Piloto Estelar, Freno de Emergencia, Satélites y Radar —«Rescate relámpago»—). Retirados: Cambio de Chip (3-oct) y Comparación Instantánea (4-oct) | – |
| Razonamiento y números | 4 (Acoplamiento, Carga exacta —antes Cálculo Sereno—, Aterrizaje Lunar y Engranajes: «Taller de reparación», 5-oct). Retirados: Detective de Series y Tráfico Estelar (4-oct); idea para llegar a 5: «Código secreto», en `docs/ideas-guardadas.md` | – |
| Lenguaje | 5 (En la punta de la lengua —antes Anagramas—, Lluvia de meteoros, ¿Verdad o disparate?, Cosecha de palabras y La estrella intrusa) | – |

Dos tipos de juego para llegar sin perder calidad: **estrella** (medida propia al final; 1-2 por área) y **base**
(sobre `GameControllerBase` + DDA común, como Tinta o Palabra; ~un tercio del trabajo). Todos táctiles, sin voz y
que se entiendan al instante (lección de Constelación y Primer Contacto).

Candidatos (tareas clásicas con décadas de literatura pública; cada uno pasa por la revisión de patentes de
`docs/nombre-marca-y-riesgos.md` ANTES de programarse; nombre, arte y sonidos propios):

- **Lenguaje**: Lluvia de meteoros (decisión léxica: tocar solo las palabras reales; Meyer y Schvaneveldt, 1971) ·
  La palabra intrusa (categorías) · Frase rota (ordenar una frase) · Sinónimos en órbita. Ojo: dependen del idioma
  (listas de palabras por idioma para el mercado global).
- **Cálculo**: ¿Cuántas estrellas? (comparar cantidades sin contar) · Combustible exacto (llegar a un número con
  cartas) · Balanza de carga.
- **Velocidad** (el área más patentada por la competencia, sobre todo Posit: revisar con más cuidado): Decodificador
  (símbolos propios con su clave) · Unir la constelación (1-2-3… lo más rápido; tarea de trazar caminos, 1944) · La
  nave distinta (búsqueda visual).
- **Razonamiento**: Torre de Lunas (planificar; Torre de Londres) · Matriz Perdida (patrones generados por programa,
  sin copiar ítems de ningún test).

## 2. Pestaña Avance: menos filas, más significado

Problema (Ricardo): información innecesaria y pobre, muchas filas de datos que no dicen nada real. Propuesta: que
responda "¿qué descubrí de mí y cómo voy cambiando?". Maqueta: `docs/previews/avance-nuevo.png`.

## 3. Orden

1. Rehacer Avance (maqueta primero).
2. Plantilla propia de "juego base" (acelera los 12 juegos).
3. Lenguaje: 2 juegos. Luego Cálculo y Velocidad (3 cada una) y Razonamiento (2).
4. Cambiar los juegos de la evaluación inicial cuando haya más juegos estrella.
5. Ricardo prueba en el teléfono los estrella pendientes (Rescate relámpago, Satélites, Rumbo, Correo). Bitácora de
   Misión y Tráfico Estelar ya se retiraron.

## Repositorios externos

Regla de Ricardo (30-sep): si aparece un repositorio de GitHub que pueda potenciar la app, se le comenta y ÉL decide si
se agrega. Descartados: Zenject / Extenject (inyección de dependencias: el original sin cambios desde 2021; obligaría a
reescribir los 20 juegos, trae riesgos con IL2CPP en Android y los juegos ya están separados en reglas + pantalla) y
awesome-unity (es una lista de enlaces, archivada en enero de 2025; sirve solo para consultar).
