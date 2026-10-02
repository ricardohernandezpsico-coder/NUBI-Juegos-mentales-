"""Asociaciones temáticas "va con" de "La estrella intrusa" (1-oct): pares (A, B) que cualquier adulto asocia SIN dudar y que NO
son del mismo tipo (perro–hueso, abeja–miel, lluvia–paraguas). Sirven para las TRAMPAS: en un grupo de animales, "hueso" va con
"perro" pero no es un animal; el juego enseña la diferencia entre "es del mismo tipo" y "suele ir junto".

Reglas (revisadas a mano):
  - la relación es universal y sin doble sentido (nada regional, nada de chistes, nada sensible);
  - A y B son de tipos distintos (si no, no sería una trampa);
  - A es la palabra del GRUPO con la que "va" B: se dice "va con perro, pero no es un animal".
Cada línea: A|B.  El generador descarta las trampas en que B también "va con" OTRA palabra del mismo grupo (la trampa sería doble).
"""

PARES = """
# ---- animales y sus cosas
perro|hueso
perro|correa
gato|ovillo
vaca|leche
vaca|establo
caballo|herradura
caballo|establo
oveja|lana
oveja|corral
cerdo|corral
gallina|huevo
gallina|corral
abeja|miel
abeja|colmena
araña|telaraña
paloma|nido
cuervo|nido
canario|jaula
loro|jaula
pez|pecera
tortuga|caparazón
conejo|madriguera
oso|miel
jirafa|cuello
caracol|caparazón
pingüino|hielo
hormiga|miga
# ---- flores y abejas, árboles y nidos
rosa|abeja
margarita|abeja
girasol|abeja
tulipán|abeja
árbol|nido
pino|nido
palmera|playa
cactus|desierto
# ---- comida
naranja|jugo
limón|jugo
miel|abeja
huevo|gallina
leche|vaca
leche|gato
queso|pan
pan|mantequilla
pan|horno
café|taza
té|tetera
té|taza
sopa|cuchara
sopa|plato
galleta|leche
azúcar|café
agua|vaso
jugo|vaso
mantequilla|pan
# ---- herramientas y cosas
martillo|clavo
destornillador|tornillo
taladro|pared
pincel|pintura
brocha|pared
pala|jardín
rastrillo|jardín
regadera|jardín
regadera|manguera
serrucho|madera
cuchillo|pan
cuchara|sopa
vaso|agua
taza|café
taza|té
olla|fuego
sartén|fuego
tetera|té
cacerola|fuego
colador|sopa
llave|cerradura
llave|candado
puerta|llave
puerta|cerradura
ventana|cortina
cortina|ventana
cama|almohada
cama|sábana
mesa|mantel
paraguas|lluvia
toalla|baño
jabón|baño
libro|biblioteca
mochila|libro
# ---- cuerpo y ropa
pie|zapato
pie|calcetín
mano|guante
mano|anillo
dedo|anillo
cabeza|sombrero
cabeza|gorro
cuello|bufanda
ojo|lente
boca|cuchara
diente|dentista
# ---- vehículos
barco|ancla
barco|mar
tren|riel
tren|estación
avión|aeropuerto
auto|calle
camión|calle
bicicleta|calle
cohete|astronauta
helicóptero|piloto
# ---- naturaleza
lluvia|paraguas
sol|sombra
sol|sombrilla
mar|arena
mar|barco
mar|ancla
playa|sombrilla
playa|arena
río|puente
lago|bote
montaña|nieve
volcán|lava
desierto|cactus
bosque|árbol
fuego|ceniza
# más parejas (sirven sobre todo para detectar las trampas dobles)
cortacésped|jardín
manguera|jardín
maceta|jardín
rodillo|pared
pincel|pared
olla|sopa
cucharita|té
cucharita|café
agua|tetera
plato|sopa
# ---- más
cuchillo|queso
cuchara|helado
cuchara|miel
taza|leche
vaso|leche
jarra|jugo
jarra|agua
botella|agua
botella|jugo
olla|arroz
horno|pastel
horno|galleta
cabra|leche
barco|puerto
auto|garaje
bote|remo
canoa|remo
kayak|remo
tetera|agua
pala|arena
rastrillo|hierba
cortacésped|hierba
regadera|flor
regadera|agua
aguja|hilo
tijera|hilo
guante|mano
sombrero|cabeza
bufanda|cuello
calcetín|pie
zapato|pie
mochila|escuela
sartén|cocina
olla|cocina
jabón|ducha
toalla|ducha
champú|ducha
almohada|cama
sábana|cama
# ---- oficios y sus cosas
bombero|manguera
cocinero|olla
cocinero|sartén
cocinero|cocina
panadero|pan
panadero|horno
pintor|pincel
pintor|pintura
carpintero|martillo
carpintero|serrucho
carpintero|madera
maestro|tiza
maestro|pizarra
maestro|escuela
pescador|pez
pescador|bote
jardinero|regadera
jardinero|rastrillo
jardinero|pala
jardinero|jardín
peluquero|tijera
músico|guitarra
músico|piano
músico|violín
músico|flauta
músico|tambor
granjero|vaca
granjero|gallina
granjero|cerdo
granjero|oveja
granjero|granja
dentista|diente
albañil|ladrillo
albañil|pala
zapatero|zapato
astronauta|cohete
astronauta|luna
marinero|barco
marinero|ancla
piloto|avión
conductor|auto
conductor|camión
# ---- lugares y sus cosas
cocina|olla
cocina|sartén
cocina|horno
baño|jabón
baño|toalla
dormitorio|cama
dormitorio|almohada
jardín|regadera
jardín|rastrillo
escuela|lápiz
escuela|cuaderno
escuela|tiza
escuela|mochila
biblioteca|libro
granja|vaca
granja|gallina
granja|establo
aeropuerto|avión
estación|tren
"""


def pares():
    out = []
    for linea in PARES.splitlines():
        linea = linea.strip()
        if not linea or linea.startswith("#"):
            continue
        a, b = linea.split("|")
        out.append((a.strip(), b.strip()))
    return out
