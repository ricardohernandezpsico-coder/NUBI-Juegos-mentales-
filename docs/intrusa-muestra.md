# Muestra del banco de grupos de La estrella intrusa (para revisar)

**Marca los grupos dudosos (más de una intrusa posible, regla rara) o palabras que sacarías.**

Cada grupo trae 4 palabras que comparten una regla y una intrusa, el nombre de la constelación y 3 opciones para
«¿Qué las une?» (la correcta va con ✓; las otras dos son plausibles pero falsas). En las trampas se dice con qué palabra del
grupo «va» la intrusa. Generada con `python tools/intrusa/grupos.py --muestra` (semilla fija).

## Tipo 1 · amplia (niveles 1-2)

1. pie, boca, pierna, brazo → **remo** · regla: Partes del cuerpo · opciones: Calzado · ✓ Partes del cuerpo · Sirven para caminar
   - Todos son partes del cuerpo; el remo no.
2. melón, plátano, naranja, manzana → **pared** · regla: Frutas · opciones: Peces · ✓ Frutas · Calzado
   - Todos son frutas; la pared no.
3. zapatilla, pantufla, sandalia, bota → **pan** · regla: Calzado · opciones: Instrumentos musicales · ✓ Calzado · Juguetes
   - Todas son calzado; el pan no.
4. tenedor, cucharita, vaso, palillos → **telaraña** · regla: Utensilios de cocina · opciones: Flores · ✓ Utensilios de cocina · Insectos
   - Todos son utensilios de cocina; la telaraña no.
5. búho, gallo, cisne, pingüino → **cómoda** · regla: Aves · opciones: Viven en el agua · ✓ Aves · Muebles
   - Todos son aves; la cómoda no.
6. atún, pez, tiburón, salmón → **colmena** · regla: Peces · opciones: Muebles · Árboles · ✓ Peces
   - Todos son peces; la colmena no.
7. mar, cueva, volcán, isla → **auto** · regla: Lugares de la naturaleza · opciones: Vehículos · ✓ Lugares de la naturaleza · Sirven para guardar cosas
   - Todos son lugares de la naturaleza; el auto no.
8. brocha, martillo, pincel, taladro → **naranja** · regla: Herramientas · opciones: Sirven para escribir · ✓ Herramientas · Frutas
   - Todos son herramientas; la naranja no.
9. pegamento, crayón, tijera, tiza → **ceniza** · regla: Material escolar · opciones: Lugares · Muebles · ✓ Material escolar
   - Todos son material escolar; la ceniza no.
10. vaca, caballo, ardilla, jirafa → **puente** · regla: Mamíferos · opciones: ✓ Mamíferos · Son mascotas · Sirven para transportar
   - Todos son mamíferos; el puente no.

## Tipo 2 · vecina (niveles 3-4)

1. zapatilla, zapato, pantufla, bota → **vestido** · regla: Calzado · opciones: Sirven para abrigarse · Frutas · ✓ Calzado
   - Todos son calzado; el vestido no.
2. avispa, grillo, libélula, mosquito → **conejo** · regla: Tienen seis patas · opciones: ✓ Tienen seis patas · Viven en el agua · Tienen cuatro patas
   - Todos tienen seis patas; el conejo no.
3. zanahoria, repollo, ajo, apio → **cereza** · regla: Verduras · opciones: Frutas · ✓ Verduras · Flores
   - Todos son verduras; la cereza no.
4. vaca, tortuga, cocodrilo, oveja → **gaviota** · regla: Tienen cuatro patas · opciones: Tienen pelo · Vuelan · ✓ Tienen cuatro patas
   - Todos tienen cuatro patas; la gaviota no.
5. sandalia, zapatilla, sombrero, boina → **lápiz** · regla: Prendas de vestir · opciones: Herramientas · Calzado · ✓ Prendas de vestir
   - Todos son prendas de vestir; el lápiz no.
6. sardina, pez, tiburón, salmón → **avispa** · regla: Peces · opciones: Tienen seis patas · Insectos · ✓ Peces
   - Todos son peces; la avispa no.
7. pandereta, bombo, armónica, violín → **helicóptero** · regla: Instrumentos musicales · opciones: Material escolar · ✓ Instrumentos musicales · Vehículos
   - Todos son instrumentos musicales; el helicóptero no.
8. parrilla, tenedor, sacacorchos, cuchara → **zapato** · regla: Utensilios de cocina · opciones: Prendas de vestir · Sirven para cortar · ✓ Utensilios de cocina
   - Todos son utensilios de cocina; el zapato no.
9. vaca, pato, caballo, cerdo → **pulpo** · regla: Viven en la granja · opciones: Viven en el agua · ✓ Viven en la granja · Viven en el mar
   - Todos viven en la granja; el pulpo no.
10. cortacésped, pincel, taladro, serrucho → **pandereta** · regla: Herramientas · opciones: Instrumentos musicales · Material escolar · ✓ Herramientas
   - Todos son herramientas; la pandereta no.

## Tipo 3 · uso (niveles 5-6)

1. olla, horno, parrilla, microondas → **champú** · regla: Sirven para cocinar · opciones: ✓ Sirven para cocinar · Sirven para guardar cosas · Sirven para lavar
   - Todos sirven para cocinar; el champú no.
2. barco, submarino, canoa, velero → **escoba** · regla: Sirven para transportar · opciones: Sirven para limpiar · ✓ Sirven para transportar · Sirven para sentarse
   - Todos sirven para transportar; la escoba no.
3. vaso, cantimplora, taza, biberón → **piano** · regla: Sirven para beber · opciones: ✓ Sirven para beber · Sirven para guardar cosas · Se usan en la cocina
   - Todos sirven para beber; el piano no.
4. tiza, marcador, lápiz, bolígrafo → **serrucho** · regla: Sirven para escribir · opciones: Sirven para cortar · ✓ Sirven para escribir · Sirven para abrir
   - Todos sirven para escribir; el serrucho no.
5. serrucho, tijera, cortaúñas, cuchillo → **muñeca** · regla: Sirven para cortar · opciones: ✓ Sirven para cortar · Sirven para cocinar · Sirven para comer
   - Todos sirven para cortar; la muñeca no.
6. esponja, lavadora, champú, jabón → **kayak** · regla: Sirven para lavar · opciones: Vehículos · Sirven para transportar · ✓ Sirven para lavar
   - Todos sirven para lavar; el kayak no.
7. marcador, brocha, rodillo, pincel → **corral** · regla: Sirven para pintar · opciones: Sirven para lavar · ✓ Sirven para pintar · Sirven para sentarse
   - Todos sirven para pintar; el corral no.
8. sillón, taburete, silla, sofá → **cortaúñas** · regla: Sirven para sentarse · opciones: Herramientas · Sirven para cortar · ✓ Sirven para sentarse
   - Todos sirven para sentarse; el cortaúñas no.
9. bufanda, manta, abrigo, chaqueta → **abrecartas** · regla: Sirven para abrigarse · opciones: Sirven para abrir · Sirven para cortar · ✓ Sirven para abrigarse
   - Todos sirven para abrigarse; el abrecartas no.
10. faro, farol, antorcha, lámpara → **estante** · regla: Sirven para alumbrar · opciones: ✓ Sirven para alumbrar · Sirven para guardar cosas · Sirven para sentarse
   - Todos sirven para alumbrar; el estante no.

## Tipo 4 · material / lugar / parte (niveles 7-8)

1. repollo, árbol, pino, espinaca → **yogur** · regla: Tienen hojas · opciones: ✓ Tienen hojas · Verduras · Árboles
   - Todos tienen hojas; el yogur no.
2. gato, oso, perro, lobo → **atún** · regla: Tienen garras · opciones: Tienen aletas · ✓ Tienen garras · Son mascotas
   - Todos tienen garras; el atún no.
3. helicóptero, barco, camión, submarino → **kayak** · regla: Tienen motor · opciones: ✓ Tienen motor · Van por el agua · Sirven para sentarse
   - Todos tienen motor; el kayak no.
4. ballena, salmón, trucha, pez → **jirafa** · regla: Tienen aletas · opciones: Tienen pelo · Mamíferos · ✓ Tienen aletas
   - Todos tienen aletas; la jirafa no.
5. cacerola, jarra, esponja, abrelatas → **pegamento** · regla: Se usan en la cocina · opciones: Material escolar · ✓ Se usan en la cocina · Sirven para pintar
   - Todos se usan en la cocina; el pegamento no.
6. cortacésped, maceta, manguera, pala → **alfombra** · regla: Se usan en el jardín · opciones: Herramientas · ✓ Se usan en el jardín · Sirven para cortar
   - Todos se usan en el jardín; la alfombra no.
7. avispa, abeja, murciélago, libélula → **sapo** · regla: Tienen alas · opciones: Mamíferos · ✓ Tienen alas · Viven en el agua
   - Todos tienen alas; el sapo no.
8. pantufla, zapato, sandalia, zapatilla → **suéter** · regla: Se ponen en los pies · opciones: Viven en el agua · ✓ Se ponen en los pies · Están en el cielo
   - Todos se ponen en los pies; el suéter no.
9. gallina, pato, loro, avestruz → **libélula** · regla: Tienen plumas · opciones: ✓ Tienen plumas · Viven en el agua · Insectos
   - Todos tienen plumas; la libélula no.
10. avión, helicóptero, cohete, planeador → **lancha** · regla: Van por el aire · opciones: ✓ Van por el aire · Viven en el mar · Se ponen en los pies
   - Todos van por el aire; la lancha no.

## Tipo 5 · trampa de asociación (niveles 9-10)

1. barco, bicicleta, cohete, submarino → **astronauta** · regla: Vehículos · opciones: ✓ Vehículos · Sirven para sentarse · Oficios · astronauta va con cohete, pero no es un vehículo
   - Todos son vehículos; el astronauta no.
2. sardina, pez, trucha, atún → **pecera** · regla: Peces · opciones: ✓ Peces · Sirven para guardar cosas · Aves · pecera va con pez, pero no es un pez
   - Todos son peces; la pecera no.
3. olla, cuchara, cucharita, palillos → **miel** · regla: Utensilios de cocina · opciones: ✓ Utensilios de cocina · Juguetes · Frutas · miel va con cuchara, pero no es un utensilio de cocina
   - Todos son utensilios de cocina; la miel no.
4. cuervo, canario, águila, gallo → **jaula** · regla: Aves · opciones: Sirven para guardar cosas · Viven en la granja · ✓ Aves · jaula va con canario, pero no es un ave
   - Todos son aves; la jaula no.
5. cerebro, oreja, ojo, pie → **calcetín** · regla: Partes del cuerpo · opciones: Se ponen en los pies · Prendas de vestir · ✓ Partes del cuerpo · calcetín va con pie, pero no es una parte del cuerpo
   - Todos son partes del cuerpo; el calcetín no.
6. mar, isla, desierto, lago → **bote** · regla: Lugares de la naturaleza · opciones: Vehículos · Sirven para transportar · ✓ Lugares de la naturaleza · bote va con lago, pero no es un lugar de la naturaleza
   - Todos son lugares de la naturaleza; el bote no.
7. lápiz, bolígrafo, tijera, sacapuntas → **hilo** · regla: Material escolar · opciones: ✓ Material escolar · Muebles · Sirven para cortar · hilo va con tijera, pero no es material escolar
   - Todos son material escolar; el hilo no.
8. pantufla, bota, zapatilla, zapato → **pie** · regla: Calzado · opciones: Partes del cuerpo · ✓ Calzado · Sirven para abrigarse · pie va con zapato, pero no es calzado
   - Todos son calzado; el pie no.
9. litera, cama, mecedora, armario → **sábana** · regla: Muebles · opciones: ✓ Muebles · Sirven para sentarse · Sirven para dormir · sábana va con cama, pero no es un mueble
   - Todos son muebles; la sábana no.
10. limón, fresa, ciruela, pera → **jugo** · regla: Frutas · opciones: ✓ Frutas · Bebidas · Lugares de la naturaleza · jugo va con limón, pero no es una fruta
   - Todos son frutas; el jugo no.

## Tipo 6 · regla + trampa (niveles 11-12)

1. hormiga, mariposa, abeja, avispa → **colmena** · regla: Tienen seis patas · opciones: Tienen cuatro patas · Árboles · ✓ Tienen seis patas · colmena va con abeja, pero no tiene seis patas
   - Todas tienen seis patas; la colmena no.
2. águila, león, gato, búho → **ovillo** · regla: Comen carne · opciones: ✓ Comen carne · Tienen cuatro patas · Tienen pelo · ovillo va con gato, pero no come carne
   - Todos comen carne; el ovillo no.
3. gato, cebra, oso, ardilla → **ovillo** · regla: Tienen pelo · opciones: Son mascotas · Viven en la granja · ✓ Tienen pelo · ovillo va con gato, pero no tiene pelo
   - Todos tienen pelo; el ovillo no.
4. helicóptero, mosquito, libélula, murciélago → **piloto** · regla: Vuelan · opciones: Viven en el agua · ✓ Vuelan · Insectos · piloto va con helicóptero, pero no vuela
   - Todos vuelan; el piloto no.
5. pera, mandarina, limón, cereza → **jugo** · regla: Crecen en los árboles · opciones: Crecen bajo la tierra · ✓ Crecen en los árboles · Bebidas · jugo va con limón, pero no crece en los árboles
   - Todos crecen en los árboles; el jugo no.
6. tren, kayak, cohete, autobús → **remo** · regla: Sirven para transportar · opciones: ✓ Sirven para transportar · Sirven para abrir · Sirven para sentarse · remo va con kayak, pero no sirve para transportar
   - Todos sirven para transportar; el remo no.
7. pez, loro, perro, conejo → **jaula** · regla: Son mascotas · opciones: Tienen pelo · Tienen cuatro patas · ✓ Son mascotas · jaula va con loro, pero no es una mascota
   - Todos son mascotas; la jaula no.
8. helicóptero, lancha, auto, cohete → **astronauta** · regla: Tienen motor · opciones: Oficios · Van por el aire · ✓ Tienen motor · astronauta va con cohete, pero no tiene motor
   - Todos tienen motor; el astronauta no.
9. zapatilla, calcetín, bota, pantufla → **pie** · regla: Se ponen en los pies · opciones: Partes del cuerpo · Sirven para caminar · ✓ Se ponen en los pies · pie va con calcetín, pero no se pone en los pies
   - Todos se ponen en los pies; el pie no.
10. pingüino, avestruz, paloma, canario → **hielo** · regla: Tienen plumas · opciones: ✓ Tienen plumas · Viven en el agua · Viven en el mar · hielo va con pingüino, pero no tiene plumas
   - Todos tienen plumas; el hielo no.
