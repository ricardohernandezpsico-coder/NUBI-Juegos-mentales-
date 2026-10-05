# Hoja de ruta (30-sep)

Acordada con Ricardo el 29/30-sep. Lo que manda es `CLAUDE.md` y el código; esto ordena lo que viene.

## 1. Juegos que faltan (4 áreas desde el 30-sep)

| Área | Hoy | Faltan |
|---|---|---|
| Memoria | 5 (Parejas, Secuencia, Bitácora, Rumbo a Casa, Correo Estelar; Ruta del Tesoro se retiró el 4-oct) | – |
| Atención y velocidad | 5 (Tinta o Palabra, Piloto, Freno, Satélites, Rescate relámpago; Cambio de Chip se retiró el 3-oct y Comparación Instantánea el 4-oct) | – |
| Razonamiento y números | 4 (Acoplamiento, Carga exacta —antes Cálculo Sereno—, Aterrizaje Lunar y **Engranajes**, nuevo el 5-oct, que ocupa el lugar de Tráfico Estelar; Detective de Series y Tráfico Estelar se retiraron el 4-oct; idea para llegar a 5: «Código secreto», en `docs/ideas-guardadas.md`) | – |
| Lenguaje | 2 (Anagramas, Lluvia de meteoros) | 1 (La palabra intrusa) |

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
5. Ricardo prueba en el teléfono los estrella pendientes (Rescate relámpago, Satélites, Bitácora, Rumbo, Correo,
   Tráfico "lento y lleno").

## Repositorios externos

Regla de Ricardo (30-sep): si aparece un repositorio de GitHub que pueda potenciar la app, se le comenta y ÉL decide si
se agrega. Descartados: Zenject / Extenject (inyección de dependencias: el original sin cambios desde 2021; obligaría a
reescribir los 20 juegos, trae riesgos con IL2CPP en Android y los juegos ya están separados en reglas + pantalla) y
awesome-unity (es una lista de enlaces, archivada en enero de 2025; sirve solo para consultar).
