# ¿Verdad o disparate? — muestra de 100 frases para revisar (1-oct)

**Marca las que estén mal, sean dudosas o suenen raras.** La ambigüedad es el mayor riesgo de este juego: una frase
dudosa frustra. Cada frase trae su respuesta (✔ verdad / ✘ disparate) y, en los disparates, su clase y la corrección
que se muestra al errar. Salen al azar con semilla fija de `docs/frases-muestra-disparate.md` (generador:
`python tools/frases/disparate.py`); las frases salen de una base propia en `tools/frases/conocimiento.py`.

Tipos: 1 corta · 2 con complemento o adjetivo · 3 negación · 4 frase con pausa («, que …,») · 5 todos / algunos / ningún · 6 comparación y orden.

## Tipo 1 · corta

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 1 | Los perros ladran | ✔ verdad | — | — |
| 2 | Las alfombras comen | ✘ disparate | evidente | Las alfombras no comen |
| 3 | Los elefantes barritan | ✔ verdad | — | — |
| 4 | Las libélulas vuelan | ✔ verdad | — | — |
| 5 | Las mariposas ladran | ✘ disparate | evidente | Las mariposas no ladran |
| 6 | Las mariposas vuelan | ✔ verdad | — | — |
| 7 | Los barcos navegan | ✔ verdad | — | — |
| 8 | Las libélulas ladran | ✘ disparate | evidente | Las libélulas no ladran |
| 9 | Las gaviotas vuelan | ✔ verdad | — | — |
| 10 | Las toallas piensan | ✘ disparate | evidente | Las toallas no piensan |
| 11 | Los canarios ladran | ✘ disparate | evidente | Los canarios no ladran |
| 12 | Las avestruces ladran | ✘ disparate | evidente | Las avestruces no ladran |
| 13 | Las lámparas duermen | ✘ disparate | evidente | Las lámparas no duermen |
| 14 | Las avispas vuelan | ✔ verdad | — | — |
| 15 | Las campanas suenan | ✔ verdad | — | — |
| 16 | Las águilas planean | ✔ verdad | — | — |
| 17 | Las ranas vuelan | ✘ disparate | evidente | Las ranas no vuelan |

## Tipo 2 · con complemento

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 18 | Las avestruces tienen pico | ✔ verdad | — | — |
| 19 | Las truchas viven en el agua | ✔ verdad | — | — |
| 20 | Los loros tienen alas | ✔ verdad | — | — |
| 21 | La nieve es blanca | ✔ verdad | — | — |
| 22 | Los pingüinos tienen plumas | ✔ verdad | — | — |
| 23 | Las escobas tienen sueño | ✘ disparate | evidente | Las escobas no tienen sueño |
| 24 | Los leopardos pilotan aviones | ✘ disparate | evidente | Los leopardos no pilotan aviones |
| 25 | Los paraguas sirven para escribir | ✘ disparate | evidente | Los paraguas no sirven para escribir |
| 26 | Las cebras tienen cuatro patas | ✔ verdad | — | — |
| 27 | Los pinceles tienen hambre | ✘ disparate | evidente | Los pinceles no tienen hambre |
| 28 | Los canarios cocinan la cena | ✘ disparate | evidente | Los canarios no cocinan la cena |
| 29 | La nariz sirve para oír | ✘ disparate | sutil | La nariz no sirve para oír |
| 30 | Las escobas tienen hambre | ✘ disparate | evidente | Las escobas no tienen hambre |
| 31 | Las arañas tienen ocho patas | ✔ verdad | — | — |
| 32 | Las mariposas leen libros | ✘ disparate | evidente | Las mariposas no leen libros |
| 33 | La miel es dulce | ✔ verdad | — | — |
| 34 | Los peces tienen aletas | ✔ verdad | — | — |

## Tipo 3 · negación

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 35 | Los cocodrilos no ladran | ✔ verdad | — | — |
| 36 | Los aviones no vuelan | ✘ disparate | sutil | Los aviones sí vuelan |
| 37 | Los canarios no dan leche | ✔ verdad | — | — |
| 38 | Las esponjas no lloran | ✔ verdad | — | — |
| 39 | Las aspiradoras no aspiran | ✘ disparate | sutil | Las aspiradoras sí aspiran |
| 40 | Las galletas no hablan | ✔ verdad | — | — |
| 41 | Los gansos no comen | ✘ disparate | evidente | Los gansos sí comen |
| 42 | Los escarabajos no cocinan la cena | ✔ verdad | — | — |
| 43 | Los platos no sirven para dormir | ✔ verdad | — | — |
| 44 | La luna no gira alrededor de la tierra | ✘ disparate | sutil | La luna sí gira alrededor de la tierra |
| 45 | La lluvia no es seca | ✔ verdad | — | — |
| 46 | Las flautas no suenan | ✘ disparate | sutil | Las flautas sí suenan |
| 47 | Las camas no sirven para dormir | ✘ disparate | sutil | Las camas sí sirven para dormir |
| 48 | Las arañas no cocinan la cena | ✔ verdad | — | — |
| 49 | Las mariposas no respiran | ✘ disparate | evidente | Las mariposas sí respiran |
| 50 | Los grillos no dan leche | ✔ verdad | — | — |
| 51 | Los sombreros no se ponen en la cabeza | ✘ disparate | sutil | Los sombreros sí se ponen en la cabeza |

## Tipo 4 · frase con pausa (, que …,)

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 52 | Las escobas, que barren, piensan | ✘ disparate | evidente | Las escobas, que barren, no piensan |
| 53 | Las hachas, que cortan, duermen | ✘ disparate | evidente | Las hachas, que cortan, no duermen |
| 54 | El agua, que se bebe, moja | ✔ verdad | — | — |
| 55 | Los elefantes, que son grandes, conducen trenes | ✘ disparate | evidente | Los elefantes, que son grandes, no conducen trenes |
| 56 | Los hipopótamos, que viven cerca del agua, escriben cartas | ✘ disparate | evidente | Los hipopótamos, que viven cerca del agua, no escriben cartas |
| 57 | Los loros, que tienen alas, tienen pico | ✔ verdad | — | — |
| 58 | Las gaviotas, que tienen pico, vuelan | ✔ verdad | — | — |
| 59 | La boca, que sirve para comer, sirve para hablar | ✔ verdad | — | — |
| 60 | Los murciélagos, que tienen alas, vuelan | ✔ verdad | — | — |
| 61 | Los pies, que sirven para caminar, tienen dedos | ✔ verdad | — | — |
| 62 | Los libros, que sirven para leer, tienen páginas | ✔ verdad | — | — |
| 63 | Los libros, que tienen páginas, hablan | ✘ disparate | evidente | Los libros, que tienen páginas, no hablan |
| 64 | Las olas, que se mueven, ríen | ✘ disparate | evidente | Las olas, que se mueven, no ríen |
| 65 | Los camiones, que tienen ruedas, tienen motor | ✔ verdad | — | — |
| 66 | Los atunes, que tienen aletas, viven en el agua | ✔ verdad | — | — |
| 67 | Los vasos, que sirven para beber, sueñan | ✘ disparate | evidente | Los vasos, que sirven para beber, no sueñan |
| 68 | Las truchas, que viven en el agua, dan leche | ✘ disparate | evidente | Las truchas, que viven en el agua, no dan leche |

## Tipo 5 · todos / algunos / ningún

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 69 | Todos los triángulos tienen cinco lados | ✘ disparate | evidente | Ningún triángulo tiene cinco lados |
| 70 | Todos los triángulos tienen cuatro lados | ✘ disparate | evidente | Ningún triángulo tiene cuatro lados |
| 71 | Ningún planeta tiene anillos | ✘ disparate | sutil | Solo algunos planetas tienen anillos |
| 72 | Ningún insecto tiene ocho patas | ✔ verdad | — | — |
| 73 | Todas las herramientas tienen mango | ✘ disparate | sutil | Solo algunas herramientas tienen mango |
| 74 | Ningún cuadrado tiene cuatro esquinas | ✘ disparate | evidente | Todos los cuadrados tienen cuatro esquinas |
| 75 | Todos los muebles respiran | ✘ disparate | evidente | Ningún mueble respira |
| 76 | Algunas personas saben nadar | ✔ verdad | — | — |
| 77 | Algunas verduras son anaranjadas | ✔ verdad | — | — |
| 78 | Algunas personas tienen mascotas | ✔ verdad | — | — |
| 79 | Algunos mamíferos vuelan | ✔ verdad | — | — |
| 80 | Ningún árbol tiene ruedas | ✔ verdad | — | — |
| 81 | Ningún vehículo tiene alas | ✘ disparate | sutil | Solo algunos vehículos tienen alas |
| 82 | Algunos pájaros dan leche | ✘ disparate | evidente | Ningún pájaro da leche |
| 83 | Ninguna verdura es de metal | ✔ verdad | — | — |
| 84 | Ningún pájaro tiene cuatro patas | ✔ verdad | — | — |

## Tipo 6 · comparación y orden

| # | Frase | Respuesta | Clase | Corrección |
|---|---|---|---|---|
| 85 | Una semana dura menos que un segundo | ✘ disparate | evidente | Un segundo dura menos que una semana |
| 86 | La bicicleta es más rápida que el guepardo | ✘ disparate | sutil | El guepardo es más rápido que la bicicleta |
| 87 | Una manzana es más pequeña que un perro | ✔ verdad | — | — |
| 88 | Un edificio pesa menos que un conejo | ✘ disparate | evidente | Un conejo pesa menos que un edificio |
| 89 | El guepardo es más lento que la bicicleta | ✘ disparate | sutil | La bicicleta es más lenta que el guepardo |
| 90 | Un mes dura más que un año | ✘ disparate | sutil | Un año dura más que un mes |
| 91 | El cohete es más rápido que el caballo | ✔ verdad | — | — |
| 92 | Una taza pesa menos que un elefante | ✔ verdad | — | — |
| 93 | El cohete es más rápido que la bicicleta | ✔ verdad | — | — |
| 94 | El helado es más frío que el sol | ✔ verdad | — | — |
| 95 | El caracol es más lento que el guepardo | ✔ verdad | — | — |
| 96 | El caracol es más rápido que el caballo | ✘ disparate | evidente | El caballo es más rápido que el caracol |
| 97 | El caballo es más rápido que el avión | ✘ disparate | evidente | El avión es más rápido que el caballo |
| 98 | El fuego es más frío que el helado | ✘ disparate | evidente | El helado es más frío que el fuego |
| 99 | Una semana dura menos que un siglo | ✔ verdad | — | — |
| 100 | Un barco pesa más que un conejo | ✔ verdad | — | — |
