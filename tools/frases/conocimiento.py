"""Base de conocimiento de "¿Verdad o disparate?" (1-oct): entidades comunes y universales con propiedades CATEGÓRICAS.

Criterio estricto: una propiedad entra solo si un adulto razonable, de cualquier país, no dudaría ("los gatos tienen
bigotes"). Si podría dudar ("los perros nadan", "los peces duermen"), NO entra. Nada regional ni metafórico.

Formato de las entidades (una por línea, campos separados por "|"):

    categoría | artículo + sustantivo | siempre (verdad) | nunca (falso)

  - El artículo (el / la / los / las) da el género y el número: "los peces" = plural genérico; "el sol" = singular.
  - Las propiedades se escriben SIEMPRE con el verbo en 3.ª persona del PLURAL ("brillan", "tienen alas", "son una
    estrella"); para sujetos en singular el generador lo conjuga ("brilla", "tiene alas", "es una estrella").
  - "~frío" = adjetivo con ser ("es frío / son fríos"); concuerda en género y número con el sujeto.
  - En la lista "nunca", un "*" delante marca un disparate SUTIL (se parece a algo cierto: "*vuelan" en pingüinos,
    "*~caliente" en el hielo); sin "*" es EVIDENTE.
  - Las propiedades de una sola palabra ("nadan") forman frases cortas (tipo 1); las demás, frases con complemento.
"""

# categoría | sujeto | siempre | nunca
ENTIDADES = """
animal|los peces|nadan;viven en el agua;tienen aletas|ladran;tienen plumas;tienen pelo;maúllan;dan leche
animal|los gatos|maúllan;tienen bigotes;tienen cola;tienen cuatro patas;ronronean;trepan|vuelan;ladran;ponen huevos;tienen plumas;tienen aletas;tienen alas
animal|los perros|ladran;tienen cuatro patas;tienen cola;aúllan;olfatean|maúllan;vuelan;ponen huevos;tienen plumas;tienen aletas;tienen alas
animal|las gallinas|ponen huevos;tienen plumas;tienen alas;tienen pico;cacarean;picotean|ladran;tienen pelo;dan leche;tienen dientes;tienen aletas
animal|los patos|nadan;tienen plumas;tienen pico;tienen alas;ponen huevos;graznan|ladran;tienen pelo;dan leche;tienen dientes
animal|las vacas|dan leche;mugen;tienen cuatro patas;comen hierba;pastan|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|los caballos|relinchan;tienen cuatro patas;tienen cola;comen hierba;galopan|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|las ovejas|balan;tienen lana;tienen cuatro patas;comen hierba;pastan|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|los cerdos|gruñen;tienen cuatro patas|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|los conejos|saltan;tienen orejas largas;tienen cuatro patas;roen|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|las ranas|saltan;croan;ponen huevos;nadan|vuelan;ladran;tienen plumas;tienen pelo;dan leche
animal|las serpientes|se arrastran;tienen escamas;reptan;silban|vuelan;ladran;tienen plumas;tienen alas;tienen cuatro patas
animal|las abejas|vuelan;zumban;tienen alas;hacen miel;tienen seis patas;pican|ladran;tienen plumas;dan leche;*tienen cuatro patas
animal|las hormigas|tienen seis patas;viven en grupos|ladran;tienen plumas;dan leche;tienen aletas;*tienen ocho patas
animal|las mariposas|vuelan;tienen alas;tienen seis patas;revolotean|ladran;tienen plumas;dan leche;*tienen cuatro patas
animal|las moscas|vuelan;zumban;tienen alas;tienen seis patas|ladran;tienen plumas;dan leche;*tienen cuatro patas
animal|los elefantes|tienen trompa;~grande;barritan|vuelan;ladran;tienen plumas;ponen huevos;tienen alas;~pequeño
animal|los leones|rugen;tienen cuatro patas;tienen garras;cazan|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|las jirafas|tienen el cuello largo;tienen cuatro patas|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|los pingüinos|nadan;tienen plumas;ponen huevos;tienen alas|*vuelan;ladran;dan leche;tienen pelo;tienen cuatro patas;maúllan
animal|los delfines|nadan;tienen aletas;saltan;silban|vuelan;ladran;*ponen huevos;tienen plumas;tienen patas
animal|las ballenas|nadan;viven en el agua;tienen aletas;~enorme|vuelan;ladran;*ponen huevos;tienen plumas;tienen patas;~pequeño
animal|los tiburones|nadan;tienen aletas;viven en el agua;cazan|vuelan;ladran;tienen plumas;tienen pelo
animal|las arañas|tienen ocho patas;tejen telarañas|vuelan;ladran;tienen plumas;*tienen seis patas;*tienen alas;dan leche
animal|las tortugas|tienen caparazón;ponen huevos;~lento|vuelan;ladran;tienen plumas;tienen pelo;dan leche;~rápido
animal|las águilas|vuelan;tienen plumas;tienen pico;tienen alas;ponen huevos;cazan;planean|ladran;tienen pelo;dan leche;tienen aletas
animal|los loros|tienen plumas;tienen pico;tienen alas|ladran;tienen pelo;dan leche;tienen aletas
animal|los murciélagos|vuelan;tienen alas|*ponen huevos;*tienen plumas;tienen pico;ladran;tienen aletas
animal|los ratones|tienen cola;tienen bigotes;roen|vuelan;ladran;tienen plumas;ponen huevos;tienen pico
animal|las cebras|tienen rayas;tienen cuatro patas|vuelan;tienen plumas;ladran;ponen huevos
animal|los cocodrilos|ponen huevos;tienen dientes;tienen cola;nadan|vuelan;tienen plumas;dan leche;ladran;tienen pelo
animal|los pulpos|viven en el mar;tienen ocho brazos;nadan|vuelan;ladran;tienen plumas;tienen alas;dan leche
animal|los caracoles|tienen concha;~lento|vuelan;ladran;tienen plumas;tienen alas;~rápido
animal|las ardillas|trepan;tienen cola;saltan|ladran;tienen plumas;ponen huevos;tienen aletas
animal|los búhos|vuelan;tienen plumas;tienen pico;tienen alas;ululan|ladran;tienen pelo;dan leche;tienen aletas
animal|los pájaros|tienen plumas;tienen pico;tienen alas;ponen huevos;cantan|ladran;dan leche;tienen pelo;tienen aletas
objeto|las sillas|sirven para sentarse|sirven para comer;vuelan
objeto|las mesas|sirven para apoyar cosas|sirven para dormir
objeto|las camas|sirven para dormir|sirven para cocinar
objeto|las puertas|se abren;se cierran|nadan
objeto|las ventanas|tienen vidrio|tienen plumas
objeto|los zapatos|se ponen en los pies|*se ponen en la cabeza
objeto|los libros|tienen páginas;sirven para leer|sirven para beber
objeto|los lápices|escriben;sirven para escribir|*cortan
objeto|las cucharas|sirven para tomar sopa|sirven para escribir
objeto|los tenedores|tienen puntas;sirven para comer|sirven para escribir
objeto|los vasos|sirven para beber|sirven para escribir
objeto|los platos|sirven para servir la comida|sirven para dormir
objeto|las botellas|sirven para guardar líquidos|sirven para escribir
objeto|las llaves|abren;sirven para abrir puertas|*cortan
objeto|las lámparas|alumbran;dan luz|se comen
objeto|los espejos|reflejan|sirven para comer
objeto|las pelotas|ruedan;rebotan|se comen
objeto|los paraguas|protegen de la lluvia|sirven para escribir
objeto|las escobas|barren;sirven para barrer|*escriben
objeto|las tijeras|cortan|*escriben
objeto|los cuchillos|cortan|*escriben
objeto|los sombreros|se ponen en la cabeza|*se ponen en los pies
objeto|las camisas|se usan para vestirse|se comen
objeto|los calcetines|se ponen en los pies|*se ponen en la cabeza
objeto|las almohadas|sirven para apoyar la cabeza|sirven para cortar
objeto|las toallas|sirven para secarse;secan|*sirven para mojarse
objeto|los jabones|sirven para lavar;limpian|*sirven para ensuciar
objeto|los peines|sirven para peinarse;peinan|sirven para escribir
objeto|las mochilas|sirven para llevar cosas|vuelan
objeto|las cajas|sirven para guardar cosas|nadan
objeto|las alfombras|cubren el suelo|cubren el cielo
objeto|las cortinas|cubren la ventana|se comen
objeto|las sábanas|cubren la cama|se comen
objeto|las ollas|sirven para cocinar|sirven para escribir
objeto|los martillos|golpean;sirven para clavar|*cortan
objeto|los clavos|tienen punta;se clavan|se beben
objeto|las cuerdas|sirven para atar|se beben
objeto|los globos|se inflan|se comen
objeto|las velas|alumbran|se comen
objeto|las campanas|suenan|se comen
objeto|los aviones|vuelan;tienen alas|nadan
objeto|los barcos|flotan;navegan|*vuelan
objeto|los trenes|viajan;tienen vagones|nadan
objeto|los camiones|tienen ruedas;tienen motor|tienen alas
objeto|las bicicletas|tienen ruedas;tienen pedales|tienen alas
objeto|los imanes|atraen el hierro|se comen
objeto|las monedas|se usan para pagar|se comen
objeto|los ladrillos|~duro;sirven para construir|~blando
comida|el pan|=un alimento/alimentos;se hacen con harina|~líquido
comida|la leche|se beben;~blanco|*~sólido
comida|las manzanas|crecen en los árboles;tienen semillas|*crecen bajo la tierra
comida|las zanahorias|crecen bajo la tierra|*crecen en los árboles
comida|los limones|~ácido;crecen en los árboles|*~dulce
comida|el azúcar|~dulce|*~salado
comida|la sal|~salado|*~dulce
comida|el helado|~frío|*~caliente
comida|la miel|~dulce|*~salado
comida|los huevos|tienen cáscara;se rompen|son de metal
comida|el queso|se hacen con leche|crecen en los árboles
comida|la sopa|se toman con cuchara|*se toman con tenedor
comida|las naranjas|crecen en los árboles|*crecen bajo la tierra
comida|las uvas|crecen en racimos|*crecen bajo la tierra
comida|el jugo|~líquido;se beben|*~sólido
comida|las galletas|se hacen con harina|se hacen con piedra
natural|el sol|brillan;calientan la tierra;~caliente;=una estrella/estrellas;alumbran|*enfrían la tierra;*~frío;=un animal/animales;son de metal
natural|la luna|giran alrededor de la tierra;están en el cielo|=una fruta/frutas;~cuadrado
natural|la nieve|~blanco;~frío;se derriten con el calor|*~caliente;*~negro;~verde
natural|el fuego|queman;~caliente;dan calor;arden|*~frío;mojan;*se encienden con agua
natural|el hielo|~frío;se derriten con el calor;flotan en el agua;enfrían|*~caliente;~blando
natural|la lluvia|mojan;caen del cielo;caen|suben desde el suelo;~seco
natural|las nubes|flotan en el cielo;están en el cielo|son de piedra;están bajo la tierra
natural|los ríos|llevan agua|llevan fuego;ladran
natural|el mar|~salado;tienen agua|*~dulce;~seco;son de piedra
natural|los árboles|tienen raíces;tienen tronco|caminan;ladran;tienen ruedas;tienen patas
natural|las flores|tienen pétalos|ladran;tienen ruedas;caminan
natural|las piedras|~duro|~blando;ríen;cantan;duermen;tienen hambre
natural|las estrellas|brillan;están en el cielo|ladran;maúllan;caminan
natural|el viento|soplan;mueven las hojas|~sólido;ladran
natural|el agua|mojan;se beben|~seco;arden
natural|las plantas|tienen raíces|caminan;ladran;tienen patas
cuerpo|las manos|tienen dedos;sirven para agarrar|sirven para ver;sirven para oír
cuerpo|los ojos|sirven para ver|*sirven para oír;*sirven para oler
cuerpo|las orejas|sirven para oír|*sirven para ver;*sirven para oler
cuerpo|la nariz|sirven para oler|*sirven para ver;*sirven para oír
cuerpo|la boca|sirven para comer;sirven para hablar|sirven para ver;sirven para oír
cuerpo|los dientes|sirven para masticar|sirven para ver;sirven para oír
cuerpo|los pies|sirven para caminar;tienen dedos|sirven para oír;sirven para ver
cuerpo|los pulmones|sirven para respirar|sirven para ver;*laten
cuerpo|el corazón|laten|*sirven para respirar;sirven para ver
animal|los lobos|aúllan;cazan;tienen cuatro patas|vuelan;maúllan;ponen huevos;tienen plumas;tienen aletas
animal|los gallos|cantan;tienen plumas;tienen pico;tienen alas|ladran;tienen pelo;dan leche;tienen dientes
animal|las palomas|arrullan;vuelan;tienen plumas;tienen pico|ladran;tienen pelo;dan leche;tienen aletas
animal|los cisnes|nadan;tienen plumas;tienen pico;tienen alas|ladran;tienen pelo;dan leche;tienen dientes
animal|las gaviotas|vuelan;graznan;tienen plumas;tienen pico|ladran;tienen pelo;dan leche;tienen aletas
animal|los mosquitos|pican;zumban;vuelan;tienen alas;tienen seis patas|ladran;tienen plumas;dan leche;*tienen cuatro patas
animal|las libélulas|vuelan;tienen alas;tienen seis patas|ladran;tienen plumas;dan leche;*tienen ocho patas
animal|los sapos|saltan;croan;ponen huevos|vuelan;ladran;tienen plumas;tienen pelo;dan leche
animal|los canarios|cantan;vuelan;tienen plumas;tienen pico|ladran;tienen pelo;dan leche;tienen aletas
animal|las avestruces|corren;tienen plumas;tienen pico;ponen huevos|*vuelan;ladran;dan leche;tienen pelo
animal|las cabras|balan;tienen cuatro patas;comen hierba|vuelan;ponen huevos;tienen plumas;ladran
animal|los monos|trepan;saltan;tienen manos|vuelan;ponen huevos;tienen plumas;ladran
objeto|los timbres|suenan|ríen;hablan;lloran;duermen
objeto|los teléfonos|suenan|ríen;lloran;duermen;comen
objeto|los ventiladores|giran;mueven el aire|ríen;lloran;duermen;comen
objeto|las ruedas|giran|ríen;hablan;lloran;duermen
objeto|las esponjas|absorben|ríen;hablan;lloran;duermen
objeto|los helicópteros|vuelan;tienen hélices|*nadan;ríen
objeto|los cohetes|despegan;vuelan|*nadan;ríen
objeto|los submarinos|navegan bajo el agua|*vuelan
objeto|las linternas|alumbran;dan luz|ríen;hablan;duermen
objeto|los faros|alumbran|ríen;hablan;duermen
objeto|las bombillas|alumbran;dan luz|ríen;hablan;duermen
objeto|las estufas|calientan|ríen;hablan;duermen
objeto|los hornos|calientan;sirven para cocinar|ríen;hablan;duermen
objeto|los congeladores|enfrían|ríen;hablan;duermen
objeto|las hachas|cortan|ríen;hablan;duermen
objeto|las sierras|cortan|ríen;hablan;duermen
objeto|los bolígrafos|escriben|*cortan
objeto|las tizas|escriben|*cortan
objeto|los borradores|borran|*escriben
objeto|los pinceles|pintan|*cortan
objeto|las aspiradoras|aspiran|ríen;hablan;duermen
cosa|los tambores|suenan|se comen
cosa|las guitarras|suenan|se comen
cosa|los pianos|suenan|se comen
cosa|las trompetas|suenan|se comen
cosa|las flautas|suenan|se comen
cosa|los relojes|marcan la hora|se comen
natural|las olas|se mueven|ríen;ladran
natural|los truenos|suenan|ríen;ladran
animal|las truchas|nadan;viven en el agua;tienen aletas|vuelan;ladran;tienen plumas;tienen pelo;dan leche
animal|los salmones|nadan;viven en el agua;tienen aletas|vuelan;ladran;tienen plumas;tienen pelo;dan leche
animal|las sardinas|nadan;viven en el agua;tienen aletas|vuelan;ladran;tienen plumas;tienen pelo;dan leche
animal|los atunes|nadan;viven en el agua;tienen aletas|vuelan;ladran;tienen plumas;tienen pelo;dan leche
animal|las anguilas|nadan;viven en el agua|vuelan;ladran;tienen plumas;tienen alas;tienen patas
animal|las orcas|nadan;cazan;tienen aletas|vuelan;ladran;*ponen huevos;tienen plumas;tienen patas
animal|las medusas|flotan;viven en el agua|vuelan;ladran;tienen plumas;tienen alas;tienen patas
animal|los tigres|rugen;cazan;tienen cuatro patas|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|los leopardos|cazan;tienen cuatro patas|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|los osos|gruñen;tienen cuatro patas;tienen garras|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|los koalas|trepan;tienen cuatro patas|vuelan;ponen huevos;tienen plumas;ladran;tienen alas
animal|los chimpancés|trepan;saltan;tienen manos|vuelan;ponen huevos;tienen plumas;ladran
animal|los cuervos|graznan;vuelan;tienen plumas;tienen pico|ladran;tienen pelo;dan leche;tienen aletas
animal|los gansos|graznan;nadan;tienen plumas;tienen pico|ladran;tienen pelo;dan leche;tienen dientes
animal|las cigüeñas|vuelan;tienen plumas;tienen pico;tienen alas|ladran;tienen pelo;dan leche;tienen aletas
animal|las lechuzas|vuelan;ululan;tienen plumas;tienen pico|ladran;tienen pelo;dan leche;tienen aletas
animal|los grillos|chirrían;saltan;tienen seis patas|ladran;tienen plumas;dan leche;*tienen ocho patas
animal|las cigarras|chirrían;vuelan;tienen alas;tienen seis patas|ladran;tienen plumas;dan leche;*tienen cuatro patas
animal|los escarabajos|tienen seis patas|ladran;tienen plumas;dan leche;*tienen ocho patas
animal|las avispas|vuelan;zumban;pican;tienen alas;tienen seis patas|ladran;tienen plumas;dan leche;*tienen cuatro patas
animal|los zorros|corren;tienen cola;tienen cuatro patas|vuelan;ponen huevos;tienen plumas;maúllan
animal|los hipopótamos|tienen cuatro patas;viven cerca del agua|vuelan;ponen huevos;tienen plumas;ladran
animal|los camellos|tienen jorobas|vuelan;ponen huevos;tienen plumas;ladran
animal|los canguros|saltan;tienen cola|vuelan;ponen huevos;tienen plumas;ladran
"""

# Lo que SIEMPRE es cierto de todo animal (se suma a cada animal; frases cortas del tipo 1).
DEFECTO_ANIMAL = ["comen", "respiran", "crecen"]

# Disparates EVIDENTES por categoría (cruce de categorías imposible). Cada uno: plural; el singular se conjuga.
POOL_VIVO = ["ríen", "hablan", "lloran", "duermen", "comen", "sueñan", "piensan", "tienen hambre", "tienen sueño"]
POOL_HUMANO = ["leen libros", "conducen trenes", "pagan cuentas", "escriben cartas", "cocinan la cena", "pilotan aviones",
               "firman documentos", "trabajan en una oficina"]
# categoría -> pool de disparates evidentes (naturaleza y cuerpo llevan los suyos escritos arriba)
POOL_POR_CATEGORIA = {"objeto": POOL_VIVO, "comida": POOL_VIVO, "animal": POOL_HUMANO}

# Grupos para los cuantificadores (tipo 5): (singular, plural, género, [todos], [ninguno], [algunos]).
#  todos   = lo cumplen TODOS los miembros del mundo real
#  ninguno = no lo cumple NINGUNO
#  algunos = lo cumplen unos y otros no (así "algunos" es cierto sin dudas: hay de los dos)
GRUPOS = [
    ("animal", "animales", "m", ["comen", "respiran", "crecen"], ["son de metal", "son de piedra", "tienen ruedas"],
     ["vuelan", "nadan", "ponen huevos", "tienen plumas", "tienen cuatro patas", "viven en el agua", "tienen cola"]),
    ("pájaro", "pájaros", "m", ["tienen plumas", "tienen pico", "tienen alas", "ponen huevos"],
     ["dan leche", "tienen pelo", "ladran", "tienen cuatro patas"], ["~azul", "~negro", "comen peces", "~blanco"]),
    ("pez", "peces", "m", ["nadan", "viven en el agua", "tienen aletas"], ["ladran", "tienen plumas", "tienen pelo", "dan leche"],
     ["viven en el mar", "viven en los ríos", "~rojo", "~azul"]),
    ("insecto", "insectos", "m", ["tienen seis patas", "tienen antenas"], ["ladran", "dan leche", "tienen plumas", "tienen ocho patas"],
     ["vuelan", "pican", "zumban", "viven en grupos"]),
    ("mamífero", "mamíferos", "m", ["dan leche a sus crías"], ["tienen plumas"],
     ["vuelan", "nadan", "ladran", "comen carne", "viven en el mar", "tienen cola"]),
    ("fruta", "frutas", "f", ["salen de una planta"], ["ladran", "=un animal/animales", "son de metal", "tienen patas"],
     ["~rojo", "~dulce", "~ácido", "~amarillo", "crecen en los árboles"]),
    ("verdura", "verduras", "f", ["salen de una planta"], ["ladran", "=un animal/animales", "son de metal"],
     ["~verde", "~anaranjado", "crecen bajo la tierra", "se comen crudas"]),
    ("mueble", "muebles", "m", [], ["ladran", "=un animal/animales", "respiran"],
     ["tienen patas", "tienen cajones", "son de madera", "sirven para sentarse"]),
    ("vehículo", "vehículos", "m", ["sirven para transportar"], ["ladran", "=un animal/animales", "comen hierba"],
     ["vuelan", "flotan", "tienen alas", "tienen ruedas", "tienen motor"]),
    ("herramienta", "herramientas", "f", [], ["ladran", "=un animal/animales"],
     ["cortan", "golpean", "tienen mango", "son de metal"]),
    ("prenda de vestir", "prendas de vestir", "f", ["se ponen en el cuerpo"], ["ladran", "comen"],
     ["tienen botones", "tienen bolsillos", "~azul", "tienen mangas"]),
    ("triángulo", "triángulos", "m", ["tienen tres lados", "tienen tres esquinas"], ["tienen cuatro lados", "tienen cinco lados", "~redondo"], []),
    ("cuadrado", "cuadrados", "m", ["tienen cuatro lados", "tienen cuatro esquinas"], ["tienen tres lados", "~redondo", "tienen cinco esquinas"], []),
    ("círculo", "círculos", "m", ["~redondo"], ["tienen esquinas", "tienen lados rectos"], []),
    ("planeta", "planetas", "m", ["giran alrededor del sol"], ["=un animal/animales", "tienen ruedas"], ["tienen anillos", "tienen lunas"]),
    ("persona", "personas", "f", ["respiran", "tienen corazón", "necesitan comer", "necesitan dormir"],
     ["tienen alas", "tienen plumas", "tienen cola", "tienen aletas"],
     ["usan lentes", "saben nadar", "tienen hermanos", "tienen mascotas", "hablan más de un idioma"]),
    ("árbol", "árboles", "m", ["tienen raíces", "tienen tronco"], ["ladran", "caminan", "tienen ruedas", "vuelan"],
     ["pierden las hojas en otoño", "tienen espinas"]),
    ("flor", "flores", "f", ["salen de una planta"], ["ladran", "caminan", "=un animal/animales"], ["~rojo", "~amarillo", "~blanco", "tienen espinas"]),
    ("reptil", "reptiles", "m", [], ["dan leche", "tienen plumas", "tienen pelo"],
     ["tienen patas", "tienen caparazón", "viven en el agua", "se arrastran"]),
    ("instrumento musical", "instrumentos musicales", "m", ["suenan"], ["ladran", "=un animal/animales"],
     ["tienen cuerdas", "se soplan", "tienen teclas"]),
]

# Comparaciones y orden (tipo 6). Las escalas por NIVELES: solo se compara con una distancia de al menos 2 niveles
# (así nunca hay dudas: "una hormiga es más pequeña que un elefante").
# (sustantivo, género, nivel)
TAMANO = [
    ("hormiga", "f", 0), ("mosca", "f", 0), ("botón", "m", 0), ("moneda", "f", 0), ("abeja", "f", 0),
    ("ratón", "m", 1), ("taza", "f", 1), ("huevo", "m", 1), ("manzana", "f", 1),
    ("gato", "m", 2), ("conejo", "m", 2), ("zapato", "m", 2), ("sombrero", "m", 2),
    ("perro", "m", 3), ("oveja", "f", 3), ("cerdo", "m", 3), ("silla", "f", 3),
    ("vaca", "f", 4), ("caballo", "m", 4), ("cama", "f", 4), ("mesa", "f", 4),
    ("elefante", "m", 5), ("casa", "f", 5),
    ("ballena", "f", 6), ("edificio", "m", 6), ("barco", "m", 6),
    ("montaña", "f", 7),
]
DURACION = [("segundo", "m", 0), ("minuto", "m", 1), ("hora", "f", 2), ("día", "m", 3), ("semana", "f", 4),
            ("mes", "m", 5), ("año", "m", 6), ("siglo", "m", 7)]
VELOCIDAD = [("caracol", "m", 0), ("tortuga", "f", 1), ("bicicleta", "f", 3), ("caballo", "m", 4), ("guepardo", "m", 5),
             ("avión", "m", 7), ("cohete", "m", 8)]
TEMPERATURA = [("hielo", "m", 0), ("nieve", "f", 0), ("helado", "m", 0), ("fuego", "m", 3), ("sol", "m", 4)]
NUMEROS = ["cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve", "diez", "once", "doce",
           "trece", "catorce", "quince", "dieciséis", "diecisiete", "dieciocho", "diecinueve", "veinte", "treinta",
           "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa", "cien"]
LETRAS = list("BCDEFGHIJLMNOPQRSTUVXY")  # sin A (la primera) ni letras con dudas de orden (K, W, Z, Ñ)
