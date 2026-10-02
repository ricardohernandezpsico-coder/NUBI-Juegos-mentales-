# Muestra del banco de grupos de La estrella intrusa (para revisar)

**Marca los grupos dudosos (más de una intrusa posible, regla rara) o palabras que sacarías.**

Cada grupo trae 4 palabras que comparten una regla y una intrusa, el nombre de la constelación y 3 opciones para
«¿Qué las une?» (la correcta va con ✓; las otras dos son plausibles pero falsas). En las trampas se dice con qué palabra del
grupo «va» la intrusa. Generada con `python tools/intrusa/grupos.py --muestra` (semilla fija).

## Tipo 1 · amplia (niveles 1-2)

1. camisa, bufanda, calcetín, guante → **calamar** · regla: Prendas de vestir · opciones: Viven en el agua · Animales · ✓ Prendas de vestir
   - Todos son prendas de vestir; el calamar no.
2. tiburón, atún, sardina, pez → **nariz** · regla: Peces · opciones: Partes del cuerpo · ✓ Peces · Sirven para respirar
   - Todos son peces; la nariz no.
3. cuello, ojo, corazón, estómago → **cerradura** · regla: Partes del cuerpo · opciones: ✓ Partes del cuerpo · Calzado · Árboles
   - Todos son partes del cuerpo; la cerradura no.
4. sandalia, pantufla, zapatilla, bota → **flor** · regla: Calzado · opciones: ✓ Calzado · Flores · Oficios
   - Todas son calzado; la flor no.
5. estación, cocina, jardín, baño → **salmón** · regla: Lugares · opciones: Animales · Viven en el mar · ✓ Lugares
   - Todos son lugares; el salmón no.
6. mandarina, fresa, pera, cereza → **mosquito** · regla: Frutas · opciones: Insectos · ✓ Frutas · Viven en el agua
   - Todas son frutas; el mosquito no.
7. rodillo, taladro, serrucho, pincel → **lagarto** · regla: Herramientas · opciones: Insectos · Reptiles · ✓ Herramientas
   - Todos son herramientas; el lagarto no.
8. cisne, paloma, ganso, gaviota → **esponja** · regla: Aves · opciones: Viven en el agua · ✓ Aves · Sirven para lavar
   - Todos son aves; la esponja no.
9. conejo, cabra, lobo, canguro → **auto** · regla: Mamíferos · opciones: Vehículos · Viven en la granja · ✓ Mamíferos
   - Todos son mamíferos; el auto no.
10. maestro, granjero, pescador, piloto → **moto** · regla: Oficios · opciones: ✓ Oficios · Vehículos · Van por tierra
   - Todos son oficios; la moto no.

## Tipo 2 · vecina (niveles 3-4)

1. tigre, perro, ballena, conejo → **grillo** · regla: Mamíferos · opciones: ✓ Mamíferos · Viven en la granja · Insectos
   - Todos son mamíferos; el grillo no.
2. trueno, lluvia, relámpago, nube → **río** · regla: Cosas del clima · opciones: Sirven para transportar · Lugares de la naturaleza · ✓ Cosas del clima
   - Todos son cosas del clima; el río no.
3. libélula, hormiga, avispa, grillo → **ardilla** · regla: Insectos · opciones: Mamíferos · ✓ Insectos · Viven en el agua
   - Todos son insectos; la ardilla no.
4. sardina, atún, pez, salmón → **avestruz** · regla: Peces · opciones: Tienen plumas · Aves · ✓ Peces
   - Todos son peces; la avestruz no.
5. tiburón, tigre, águila, búho → **conejo** · regla: Comen carne · opciones: ✓ Comen carne · Tienen cuatro patas · Tienen pelo
   - Todos comen carne; el conejo no.
6. cómoda, baúl, mesa, mecedora → **linterna** · regla: Muebles · opciones: Aparatos de la casa · ✓ Muebles · Material escolar
   - Todos son muebles; la linterna no.
7. tetera, sartén, cacerola, cucharita → **rodillo** · regla: Utensilios de cocina · opciones: ✓ Utensilios de cocina · Sirven para pintar · Herramientas
   - Todas son utensilios de cocina; el rodillo no.
8. naranja, pera, ciruela, cereza → **uva** · regla: Crecen en los árboles · opciones: Crecen bajo la tierra · Bebidas · ✓ Crecen en los árboles
   - Todas crecen en los árboles; la uva no.
9. playa, mar, bosque, volcán → **lluvia** · regla: Lugares de la naturaleza · opciones: Sirven para limpiar · ✓ Lugares de la naturaleza · Cosas del clima
   - Todos son lugares de la naturaleza; la lluvia no.
10. árbol, olivo, roble, palmera → **girasol** · regla: Árboles · opciones: Tienen pétalos · ✓ Árboles · Flores
   - Todos son árboles; el girasol no.

## Tipo 3 · uso (niveles 5-6)

1. olla, sartén, cacerola, microondas → **suéter** · regla: Sirven para cocinar · opciones: Sirven para abrigarse · ✓ Sirven para cocinar · Sirven para guardar cosas
   - Todos sirven para cocinar; el suéter no.
2. cohete, barco, lancha, canoa → **sacapuntas** · regla: Sirven para transportar · opciones: Sirven para cortar · Sirven para sentarse · ✓ Sirven para transportar
   - Todos sirven para transportar; el sacapuntas no.
3. taza, cantimplora, termo, vaso → **peine** · regla: Sirven para beber · opciones: Sirven para guardar cosas · ✓ Sirven para beber · Se usan en la cocina
   - Todos sirven para beber; el peine no.
4. tiza, lápiz, marcador, bolígrafo → **campana** · regla: Sirven para escribir · opciones: ✓ Sirven para escribir · Se golpean · Sirven para lavar
   - Todos sirven para escribir; la campana no.
5. cortaúñas, cuchillo, cortacésped, tijera → **cuaderno** · regla: Sirven para cortar · opciones: Material escolar · ✓ Sirven para cortar · Sirven para abrir
   - Todos sirven para cortar; el cuaderno no.
6. jabón, lavadora, detergente, champú → **hilo** · regla: Sirven para lavar · opciones: ✓ Sirven para lavar · Se golpean · Sirven para escribir
   - Todos sirven para lavar; el hilo no.
7. brocha, pincel, crayón, rodillo → **maceta** · regla: Sirven para pintar · opciones: ✓ Sirven para pintar · Se usan en el jardín · Sirven para abrir
   - Todos sirven para pintar; la maceta no.
8. silla, mecedora, sillón, sofá → **correa** · regla: Sirven para sentarse · opciones: ✓ Sirven para sentarse · Material escolar · Se golpean
   - Todos sirven para sentarse; la correa no.
9. manta, chaqueta, suéter, gorro → **almohada** · regla: Sirven para abrigarse · opciones: Sirven para sentarse · Sirven para dormir · ✓ Sirven para abrigarse
   - Todos sirven para abrigarse; la almohada no.
10. farol, linterna, antorcha, faro → **platillos** · regla: Sirven para alumbrar · opciones: Se golpean · Instrumentos musicales · ✓ Sirven para alumbrar
   - Todos sirven para alumbrar; el platillos no.

## Tipo 4 · material / lugar / parte (niveles 7-8)

1. submarino, autobús, camión, auto → **velero** · regla: Tienen motor · opciones: Van por el agua · ✓ Tienen motor · Sirven para guardar cosas
   - Todos tienen motor; el velero no.
2. oso, búho, gato, águila → **delfín** · regla: Tienen garras · opciones: Mamíferos · ✓ Tienen garras · Tienen pelo
   - Todos tienen garras; el delfín no.
3. pato, águila, ganso, gallina → **mosquito** · regla: Tienen plumas · opciones: Viven en el agua · ✓ Tienen plumas · Insectos
   - Todos tienen plumas; el mosquito no.
4. trucha, sardina, ballena, tiburón → **cebra** · regla: Tienen aletas · opciones: Mamíferos · Tienen pelo · ✓ Tienen aletas
   - Todos tienen aletas; la cebra no.
5. colador, cuchillo, vaso, esponja → **trompeta** · regla: Se usan en la cocina · opciones: ✓ Se usan en la cocina · Se soplan · Instrumentos musicales
   - Todos se usan en la cocina; la trompeta no.
6. maceta, cortacésped, manguera, pala → **teléfono** · regla: Se usan en el jardín · opciones: ✓ Se usan en el jardín · Herramientas · Sirven para cortar
   - Todos se usan en el jardín; el teléfono no.
7. loro, gaviota, murciélago, mariposa → **trucha** · regla: Tienen alas · opciones: Tienen plumas · ✓ Tienen alas · Tienen aletas
   - Todos tienen alas; la trucha no.
8. zapatilla, pantufla, bota, zapato → **bufanda** · regla: Se ponen en los pies · opciones: Están en el cielo · ✓ Se ponen en los pies · Van por el agua
   - Todos se ponen en los pies; la bufanda no.
9. girasol, flor, rosa, margarita → **roble** · regla: Tienen pétalos · opciones: ✓ Tienen pétalos · Árboles · Están en el cielo
   - Todos tienen pétalos; el roble no.
10. planeador, cohete, avión, helicóptero → **bote** · regla: Van por el aire · opciones: Viven en el mar · ✓ Van por el aire · Se usan en el jardín
   - Todos van por el aire; el bote no.

## Tipo 5 · trampa de asociación (niveles 9-10)

1. gaviota, cuervo, canario, avestruz → **jaula** · regla: Aves · opciones: Sirven para guardar cosas · Instrumentos musicales · ✓ Aves · jaula va con canario, pero no es un ave
   - Todos son aves; la jaula no.
2. cerdo, murciélago, perro, cabra → **leche** · regla: Mamíferos · opciones: Son mascotas · ✓ Mamíferos · Bebidas · leche va con cabra, pero no es un mamífero
   - Todos son mamíferos; la leche no.
3. pie, oreja, cerebro, ojo → **calcetín** · regla: Partes del cuerpo · opciones: ✓ Partes del cuerpo · Prendas de vestir · Se ponen en los pies · calcetín va con pie, pero no es una parte del cuerpo
   - Todos son partes del cuerpo; el calcetín no.
4. auto, tren, moto, velero → **garaje** · regla: Vehículos · opciones: ✓ Vehículos · Lugares · Partes del cuerpo · garaje va con auto, pero no es un vehículo
   - Todos son vehículos; el garaje no.
5. dron, microondas, horno, lámpara → **galleta** · regla: Aparatos de la casa · opciones: Se usan en la cocina · ✓ Aparatos de la casa · Sirven para cocinar · galleta va con horno, pero no es un aparato de la casa
   - Todos son aparatos de la casa; la galleta no.
6. sacapuntas, marcador, tijera, bolígrafo → **hilo** · regla: Material escolar · opciones: ✓ Material escolar · Sirven para cortar · Muebles · hilo va con tijera, pero no es material escolar
   - Todos son material escolar; el hilo no.
7. zapatero, marinero, astronauta, bombero → **manguera** · regla: Oficios · opciones: Sirven para limpiar · ✓ Oficios · Se usan en el jardín · manguera va con bombero, pero no es un oficio
   - Todos son oficios; la manguera no.
8. playa, isla, lago, mar → **sombrilla** · regla: Lugares de la naturaleza · opciones: Sirven para alumbrar · Vehículos · ✓ Lugares de la naturaleza · sombrilla va con playa, pero no es un lugar de la naturaleza
   - Todos son lugares de la naturaleza; la sombrilla no.
9. café, leche, té, agua → **gato** · regla: Bebidas · opciones: Sirven para limpiar · Mamíferos · ✓ Bebidas · gato va con leche, pero no es una bebida
   - Todos son bebidas; el gato no.
10. tenedor, colador, cuchillo, olla → **cocina** · regla: Utensilios de cocina · opciones: ✓ Utensilios de cocina · Sirven para cortar · Lugares · cocina va con olla, pero no es un utensilio de cocina
   - Todos son utensilios de cocina; la cocina no.

## Tipo 6 · regla + trampa (niveles 11-12)

1. cortacésped, tijera, cortaúñas, cuchillo → **hilo** · regla: Sirven para cortar · opciones: ✓ Sirven para cortar · Sirven para comer · Sirven para abrir · hilo va con tijera, pero no sirve para cortar
   - Todos sirven para cortar; el hilo no.
2. cebra, oveja, canguro, caballo → **corral** · regla: Comen hierba · opciones: ✓ Comen hierba · Son mascotas · Viven en la granja · corral va con oveja, pero no come hierba
   - Todos comen hierba; el corral no.
3. abrigo, bufanda, suéter, gorro → **cuello** · regla: Sirven para abrigarse · opciones: Partes del cuerpo · Sirven para lavar · ✓ Sirven para abrigarse · cuello va con bufanda, pero no sirve para abrigarse
   - Todos sirven para abrigarse; el cuello no.
4. conejo, gato, pez, perro → **hueso** · regla: Son mascotas · opciones: Tienen garras · Partes del cuerpo · ✓ Son mascotas · hueso va con perro, pero no es una mascota
   - Todos son mascotas; el hueso no.
5. búho, abeja, dron, mosquito → **colmena** · regla: Vuelan · opciones: ✓ Vuelan · Insectos · Viven en el agua · colmena va con abeja, pero no vuela
   - Todos vuelan; la colmena no.
6. pez, delfín, tiburón, sardina → **pecera** · regla: Tienen aletas · opciones: ✓ Tienen aletas · Sirven para guardar cosas · Mamíferos · pecera va con pez, pero no tiene aletas
   - Todos tienen aletas; la pecera no.
7. autobús, tren, bicicleta, auto → **riel** · regla: Tienen ruedas · opciones: Sirven para guardar cosas · ✓ Tienen ruedas · Se usan en el jardín · riel va con tren, pero no tiene ruedas
   - Todos tienen ruedas; el riel no.
8. gallo, gaviota, pato, canario → **jaula** · regla: Tienen plumas · opciones: Son mascotas · Sirven para guardar cosas · ✓ Tienen plumas · jaula va con canario, pero no tiene plumas
   - Todos tienen plumas; la jaula no.
9. ardilla, cabra, murciélago, cebra → **leche** · regla: Tienen pelo · opciones: Viven en la granja · Vuelan · ✓ Tienen pelo · leche va con cabra, pero no tiene pelo
   - Todos tienen pelo; la leche no.
10. atún, pez, calamar, pulpo → **pecera** · regla: Viven en el agua · opciones: ✓ Viven en el agua · Peces · Sirven para guardar cosas · pecera va con pez, pero no vive en el agua
   - Todos viven en el agua; la pecera no.
