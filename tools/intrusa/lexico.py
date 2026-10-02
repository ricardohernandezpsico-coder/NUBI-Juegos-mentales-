"""Base de significados de "La estrella intrusa" (1-oct): palabras comunes con TODAS las propiedades de sentido común que se
usan para agruparlas. Alimenta al generador (grupos.py) y, sobre todo, al VERIFICADOR DE UNICIDAD: un grupo solo vale si ninguna
otra palabra podría ser "la intrusa" con una regla que se pueda nombrar. Por eso conviene llenar de propiedades de más: cada
propiedad extra solo puede descartar grupos dudosos, nunca dejar pasar uno malo.

Criterio (igual de estricto que tools/frases/conocimiento.py): una propiedad entra solo si un adulto razonable, de cualquier país,
no dudaría. Si podría dudar ("los gorilas tienen cola", "una gallina vuela"), NO entra; si una palabra tiene "a veces" una
propiedad que importa, se anota en DUDOSO (el grupo que dependa de ella se descarta). Palabras con doble sentido ("ratón",
"banco", "planta", "sierra", "cola", "mango", "cometa", "goma"...) NO están: ver AMBIGUAS.

Formato de cada palabra:    palabra|género (m/f)|propiedades separadas por coma[|sec:propiedades de otro sentido]
Las propiedades que IMPLICA una propiedad (IMPLICA) se suman solas.
"""

# ------------------------------------------------------------------ lo que implica cada propiedad
IMPLICA = {
    "mamífero": ["animal"], "ave": ["animal", "plumas", "alas", "pico", "pone huevos", "dos patas"],
    "pez": ["animal", "vive en el agua", "aletas", "nada"], "insecto": ["animal", "seis patas"],
    "reptil": ["animal", "escamas"], "anfibio": ["animal"], "arácnido": ["animal", "ocho patas"],
    "animal": ["ser vivo"], "planta": ["ser vivo"], "árbol": ["planta"], "flor": ["planta"],
    "fruta": ["comida", "dulce o ácido"], "verdura": ["comida", "planta comestible"], "bebida": ["se bebe"],
    "comida": ["se come"],
    "herramienta": ["objeto"], "utensilio de cocina": ["objeto", "se usa en la cocina"], "mueble": ["objeto", "se usa en la casa"],
    "prenda": ["objeto", "se viste"], "calzado": ["objeto", "se viste", "se pone en los pies"], "vehículo": ["objeto", "sirve para transportar"],
    "instrumento": ["objeto", "suena"], "material escolar": ["objeto", "se usa en la escuela"], "juguete": ["objeto"],
    "aparato": ["objeto", "se usa en la casa"],
    "parte del cuerpo": ["cuerpo"], "oficio": ["persona"], "lugar": ["lugar"], "astro": ["natural", "está en el cielo", "brilla"], "clima": ["natural"],
    "paisaje": ["natural"], "natural": [],
}

# ------------------------------------------------------------------ las palabras
PALABRAS = """
# ---- mamíferos
perro|m|mamífero,cuatro patas,pelo,cola,mascota,doméstico,come carne,ladra,orejas,garras
gato|m|mamífero,cuatro patas,pelo,cola,mascota,doméstico,come carne,maúlla,orejas,bigotes,garras
vaca|f|mamífero,cuatro patas,pelo,cola,granja,doméstico,come hierba,da leche,orejas,muge
caballo|m|mamífero,cuatro patas,pelo,cola,granja,doméstico,come hierba,orejas,relincha
oveja|f|mamífero,cuatro patas,pelo,cola,granja,doméstico,come hierba,orejas,lana,bala
cerdo|m|mamífero,cuatro patas,cola,granja,doméstico,orejas,gruñe
conejo|m|mamífero,cuatro patas,pelo,cola,mascota,doméstico,come hierba,orejas,salta
cabra|f|mamífero,cuatro patas,pelo,cola,granja,doméstico,come hierba,da leche,orejas
burro|m|mamífero,cuatro patas,pelo,cola,granja,doméstico,come hierba,orejas
león|m|mamífero,cuatro patas,pelo,cola,salvaje,come carne,garras,ruge,orejas
tigre|m|mamífero,cuatro patas,pelo,cola,salvaje,come carne,garras,ruge,rayas,orejas
oso|m|mamífero,cuatro patas,pelo,cola,salvaje,garras,orejas
lobo|m|mamífero,cuatro patas,pelo,cola,salvaje,come carne,garras,aúlla,orejas
zorro|m|mamífero,cuatro patas,pelo,cola,salvaje,garras,orejas
elefante|m|mamífero,cuatro patas,cola,salvaje,come hierba,trompa,orejas
jirafa|f|mamífero,cuatro patas,pelo,cola,salvaje,come hierba,cuello largo,orejas
cebra|f|mamífero,cuatro patas,pelo,cola,salvaje,come hierba,rayas,orejas
canguro|m|mamífero,pelo,cola,salvaje,come hierba,salta,orejas
ardilla|f|mamífero,pelo,cola,salvaje,trepa,orejas,cuatro patas
murciélago|m|mamífero,alas,vuela,salvaje,pelo,orejas
delfín|m|mamífero,vive en el agua,vive en el mar,aletas,nada,salvaje
ballena|f|mamífero,vive en el agua,vive en el mar,aletas,nada,salvaje
# ---- aves
águila|f|ave,vuela,salvaje,come carne,garras
paloma|f|ave,vuela
loro|m|ave,vuela,mascota
canario|m|ave,vuela,mascota,canta
búho|m|ave,vuela,salvaje,come carne,garras
cuervo|m|ave,vuela,salvaje
gaviota|f|ave,vuela,salvaje
gallina|f|ave,granja,doméstico,cresta
gallo|m|ave,granja,doméstico,cresta,canta
pato|m|ave,granja,doméstico,nada
ganso|m|ave,granja,doméstico,nada
cisne|m|ave,nada
pingüino|m|ave,nada,salvaje
avestruz|f|ave,salvaje,corre
# ---- peces y animales del agua
pez|m|pez,mascota
tiburón|m|pez,vive en el mar,come carne,dientes,salvaje
atún|m|pez,vive en el mar
sardina|f|pez,vive en el mar
trucha|f|pez
salmón|m|pez
pulpo|m|animal,vive en el agua,vive en el mar,nada,salvaje,brazos
medusa|f|animal,vive en el agua,vive en el mar,nada,salvaje
calamar|m|animal,vive en el agua,vive en el mar,nada,salvaje,brazos
# ---- insectos y otros bichos
abeja|f|insecto,alas,vuela,pica,zumba,hace miel
hormiga|f|insecto,vive en grupos
mosca|f|insecto,alas,vuela,zumba
mariposa|f|insecto,alas,vuela
mosquito|m|insecto,alas,vuela,pica,zumba
libélula|f|insecto,alas,vuela
avispa|f|insecto,alas,vuela,pica,zumba
grillo|m|insecto,salta
araña|f|arácnido,teje telarañas
caracol|m|animal,concha,lento
# ---- reptiles y anfibios
serpiente|f|reptil,se arrastra,salvaje
tortuga|f|reptil,caparazón,lento,mascota,cuatro patas
cocodrilo|m|reptil,dientes,nada,salvaje,cuatro patas,cola
lagarto|m|reptil,cuatro patas,cola
rana|f|anfibio,salta,croa,nada,cuatro patas
sapo|m|anfibio,salta,croa,cuatro patas
# ---- frutas
manzana|f|fruta,crece en árbol,rojo,verde,dulce
pera|f|fruta,crece en árbol,verde,dulce
uva|f|fruta,crece en racimo,dulce
plátano|m|fruta,amarillo,dulce,crece en racimo
naranja|f|fruta,crece en árbol,anaranjado,cítrico,dulce
limón|m|fruta,crece en árbol,amarillo,cítrico
fresa|f|fruta,rojo,dulce,crece en el suelo
sandía|f|fruta,verde,dulce,crece en el suelo
cereza|f|fruta,crece en árbol,rojo,dulce
piña|f|fruta,amarillo,dulce,crece en el suelo
melón|m|fruta,dulce,crece en el suelo
mandarina|f|fruta,crece en árbol,anaranjado,cítrico,dulce
kiwi|m|fruta,verde,crece en racimo
ciruela|f|fruta,crece en árbol,dulce
# ---- verduras
zanahoria|f|verdura,crece bajo la tierra,anaranjado
lechuga|f|verdura,verde,hojas
cebolla|f|verdura,crece bajo la tierra
ajo|m|verdura,crece bajo la tierra
espinaca|f|verdura,verde,hojas
brócoli|m|verdura,verde
coliflor|f|verdura
repollo|m|verdura,verde,hojas
apio|m|verdura,verde
rábano|m|verdura,crece bajo la tierra
# ---- otras comidas y bebidas
pan|m|comida,harina
queso|m|comida,lácteo,se hace con leche
yogur|m|comida,lácteo,se hace con leche
leche|f|bebida,lácteo,blanco,líquido
huevo|m|comida,cáscara
arroz|m|comida,grano
miel|f|comida,dulce,líquido
azúcar|m|comida,dulce,blanco
sal|f|comida,salado,blanco
galleta|f|comida,dulce,harina
pastel|m|comida,dulce,harina
helado|m|comida,dulce,frío
chocolate|m|comida,dulce
sopa|f|comida,líquido,caliente
jugo|m|bebida,líquido
agua|f|bebida,líquido,transparente
té|m|bebida,líquido,caliente
café|m|bebida,líquido,caliente
# ---- herramientas
martillo|m|herramienta,sirve para clavar,mango,metal
destornillador|m|herramienta,sirve para atornillar,mango,metal
serrucho|m|herramienta,sirve para cortar,mango,metal,dientes
alicate|m|herramienta,sirve para sujetar,mango,metal
pala|f|herramienta,sirve para cavar,mango,metal,se usa en el jardín
hacha|f|herramienta,sirve para cortar,mango,metal
taladro|m|herramienta,sirve para perforar,metal
tijera|f|herramienta,sirve para cortar,metal,material escolar
brocha|f|herramienta,sirve para pintar,mango
pincel|m|herramienta,sirve para pintar,mango
# ---- utensilios de cocina y mesa
cuchillo|m|utensilio de cocina,sirve para cortar,metal,se usa en la mesa,mango
cuchara|f|utensilio de cocina,sirve para comer,metal,se usa en la mesa,mango
tenedor|m|utensilio de cocina,sirve para comer,metal,se usa en la mesa,puntas
plato|m|objeto,se usa en la cocina,se usa en la mesa,sirve para servir la comida,redondo
vaso|m|objeto,se usa en la cocina,sirve para beber,se usa en la mesa,vidrio
taza|f|objeto,se usa en la cocina,sirve para beber,se usa en la mesa,asa
jarra|f|objeto,se usa en la cocina,se usa en la mesa,asa,sirve para servir líquidos
olla|f|utensilio de cocina,sirve para cocinar,va al fuego,metal,asa
sartén|f|utensilio de cocina,sirve para cocinar,va al fuego,metal,mango,redondo
tetera|f|utensilio de cocina,va al fuego,metal,asa,sirve para servir líquidos
cacerola|f|utensilio de cocina,sirve para cocinar,va al fuego,metal,asa
colador|m|utensilio de cocina,sirve para colar,metal
abrelatas|m|utensilio de cocina,sirve para abrir,metal
# ---- muebles y cosas de la casa
silla|f|mueble,sirve para sentarse,madera,patas
mesa|f|mueble,sirve para apoyar cosas,madera,patas
cama|f|mueble,sirve para dormir,patas
sofá|m|mueble,sirve para sentarse,patas
armario|m|mueble,sirve para guardar cosas,puertas,madera
estante|m|mueble,sirve para guardar cosas,madera
escritorio|m|mueble,se escribe en él,patas,madera
cómoda|f|mueble,sirve para guardar cosas,cajones,madera
almohada|f|objeto,sirve para dormir,blando,tela,se usa en la casa
alfombra|f|objeto,cubre el suelo,tela,se usa en la casa
cortina|f|objeto,cubre la ventana,tela,se usa en la casa
espejo|m|objeto,refleja,vidrio,se usa en la casa
lámpara|f|aparato,sirve para alumbrar,da luz
linterna|f|aparato,sirve para alumbrar,da luz
televisión|f|aparato,pantalla,suena,sirve para ver programas
radio|f|aparato,suena,sirve para escuchar
teléfono|m|aparato,suena,sirve para llamar
reloj|m|aparato,sirve para saber la hora,redondo
lavadora|f|aparato,sirve para lavar,agua
aspiradora|f|aparato,sirve para limpiar,sirve para aspirar
ventilador|m|aparato,sirve para dar aire,gira
plancha|f|aparato,sirve para planchar,caliente,metal
escoba|f|objeto,sirve para barrer,sirve para limpiar,mango
jabón|m|objeto,sirve para lavar,sirve para limpiar,se usa en el baño
toalla|f|objeto,sirve para secar,tela,se usa en el baño
paraguas|m|objeto,sirve para protegerse de la lluvia,se abre
llave|f|objeto,sirve para abrir,metal
candado|m|objeto,sirve para cerrar,metal
puerta|f|objeto,se abre,sirve para entrar,se usa en la casa
ventana|f|objeto,vidrio,se abre,se usa en la casa
# ---- prendas
camisa|f|prenda,tela,botones,mangas,se pone en el cuerpo
pantalón|m|prenda,tela,se pone en las piernas,se pone en el cuerpo
chaqueta|f|prenda,tela,mangas,sirve para abrigarse,se pone en el cuerpo
abrigo|m|prenda,tela,mangas,sirve para abrigarse,se pone en el cuerpo
vestido|m|prenda,tela,se pone en el cuerpo
falda|f|prenda,tela,se pone en el cuerpo
bufanda|f|prenda,tela,sirve para abrigarse,se pone en el cuello
guante|m|prenda,tela,sirve para abrigarse,se pone en las manos
sombrero|m|objeto,se viste,se pone en la cabeza
gorro|m|objeto,se viste,tela,se pone en la cabeza,sirve para abrigarse
calcetín|m|prenda,tela,se pone en los pies
zapato|m|calzado,se pone en los pies,cuero
bota|f|calzado,se pone en los pies,cuero
sandalia|f|calzado,se pone en los pies
# ---- vehículos
auto|m|vehículo,ruedas,motor,va por tierra
bicicleta|f|vehículo,ruedas,pedales,va por tierra,sin motor
moto|f|vehículo,ruedas,motor,va por tierra
camión|m|vehículo,ruedas,motor,va por tierra
autobús|m|vehículo,ruedas,motor,va por tierra
tren|m|vehículo,va por tierra,vagones,metal,ruedas
avión|m|vehículo,alas,vuela,va por el aire,motor,metal
helicóptero|m|vehículo,vuela,va por el aire,hélices,motor,metal
cohete|m|vehículo,vuela,va por el aire,motor,metal
barco|m|vehículo,flota,va por el agua,navega,motor
bote|m|vehículo,flota,va por el agua,navega,sin motor
submarino|m|vehículo,navega,va por el agua,metal,motor
# ---- instrumentos
guitarra|f|instrumento,cuerdas,madera
piano|m|instrumento,cuerdas,teclas,madera
violín|m|instrumento,cuerdas,madera
arpa|f|instrumento,cuerdas,madera
tambor|m|instrumento,se golpea
flauta|f|instrumento,se sopla
trompeta|f|instrumento,se sopla,metal
xilófono|m|instrumento,se golpea
campana|f|objeto,suena,se golpea,metal
# ---- cuerpo
mano|f|parte del cuerpo,dedos,sirve para agarrar
pie|m|parte del cuerpo,dedos,sirve para caminar
ojo|m|parte del cuerpo,sirve para ver
oreja|f|parte del cuerpo,sirve para oír
nariz|f|parte del cuerpo,sirve para oler,sirve para respirar
boca|f|parte del cuerpo,sirve para comer,sirve para hablar,sirve para respirar,dientes
diente|m|parte del cuerpo,sirve para masticar,duro
lengua|f|parte del cuerpo
codo|m|parte del cuerpo
rodilla|f|parte del cuerpo
brazo|m|parte del cuerpo,sirve para agarrar
pierna|f|parte del cuerpo,sirve para caminar
dedo|m|parte del cuerpo,sirve para agarrar
cabeza|f|parte del cuerpo
corazón|m|parte del cuerpo,late
pulmón|m|parte del cuerpo,sirve para respirar
cerebro|m|parte del cuerpo
estómago|m|parte del cuerpo
hueso|m|parte del cuerpo,duro,blanco
# ---- cielo, clima y paisaje
sol|m|astro,calienta,da luz
luna|f|astro
estrella|f|astro
planeta|m|astro
nube|f|clima,está en el cielo,blanco
lluvia|f|clima,agua,cae del cielo,moja
nieve|f|clima,agua,cae del cielo,frío,blanco
viento|m|clima,aire
trueno|m|clima,suena,está en el cielo
relámpago|m|clima,da luz,está en el cielo
arcoíris|m|clima,está en el cielo,colores
río|m|paisaje,agua,corre
lago|m|paisaje,agua
mar|m|paisaje,agua,salado
montaña|f|paisaje,alto
volcán|m|paisaje,alto,montaña
isla|f|paisaje,rodeada de agua
playa|f|paisaje,arena
desierto|m|paisaje,arena,seco
bosque|m|paisaje,árboles
cueva|f|paisaje
arena|f|natural,sirve para construir
piedra|f|natural,duro
fuego|m|natural,caliente,quema,da luz
hielo|m|natural,frío,agua
# ---- plantas
árbol|m|árbol,raíces,tronco,hojas
rosa|f|flor,pétalos,espinas
margarita|f|flor,pétalos
girasol|m|flor,pétalos,amarillo
tulipán|m|flor,pétalos
hierba|f|planta,verde,hojas
palmera|f|árbol,raíces,tronco,hojas
pino|m|árbol,raíces,tronco,hojas
cactus|m|planta,espinas,verde
# ---- escuela y oficina
lápiz|m|material escolar,sirve para escribir,madera
bolígrafo|m|material escolar,sirve para escribir
cuaderno|m|material escolar,páginas,se escribe en él
libro|m|material escolar,páginas,sirve para leer
regla|f|material escolar,sirve para medir
mochila|f|objeto,sirve para llevar cosas,tela,se usa en la escuela
tiza|f|material escolar,sirve para escribir,blanco
sacapuntas|m|material escolar,sirve para afilar lápices,metal
pegamento|m|material escolar,sirve para pegar
# ---- juguetes y otros
pelota|f|juguete,redondo,rebota
muñeca|f|juguete
globo|m|juguete,redondo,se infla
botella|f|objeto,sirve para guardar líquidos,vidrio
caja|f|objeto,sirve para guardar cosas
cuerda|f|objeto,sirve para atar
moneda|f|objeto,sirve para pagar,redondo,metal
imán|m|objeto,atrae el hierro,metal
# ---- cosas que "van con" otras (solo se usan como intrusas de las trampas; también tienen sus propiedades)
correa|f|objeto,sirve para pasear al perro
ovillo|m|objeto,lana
colmena|f|objeto,se hacen abejas
telaraña|f|natural,fina
nido|m|natural,está en los árboles
pecera|f|objeto,vidrio,agua
herradura|f|objeto,metal
establo|m|objeto,se usa en la granja
corral|m|objeto,se usa en la granja
madriguera|f|natural
cerradura|f|objeto,metal,sirve para cerrar
hilo|m|objeto,fino,sirve para coser
aguja|f|objeto,metal,sirve para coser
cuenco|m|objeto,sirve para servir la comida
miga|f|comida,pan
pluma|f|natural,suave
cuello|m|parte del cuerpo
# ---- más cosas por su uso (para tener variedad de grupos)
sillón|m|mueble,sirve para sentarse,patas,blando
taburete|m|mueble,sirve para sentarse,patas
mecedora|f|mueble,sirve para sentarse,madera
colchón|m|mueble,sirve para dormir,blando
cuna|f|mueble,sirve para dormir,madera
litera|f|mueble,sirve para dormir,patas
manta|f|objeto,sirve para abrigarse,sirve para cubrir la cama,tela,blando,se usa en la casa
pijama|m|prenda,sirve para dormir,tela,se pone en el cuerpo
suéter|m|prenda,sirve para abrigarse,tela,mangas,se pone en el cuerpo
cantimplora|f|objeto,sirve para beber,sirve para guardar líquidos,metal
termo|m|objeto,sirve para beber,sirve para guardar líquidos,metal
biberón|m|objeto,sirve para beber,sirve para guardar líquidos
cucharita|f|utensilio de cocina,sirve para comer,metal,se usa en la mesa,mango
palillos|m|objeto,se usa en la cocina,sirve para comer,se usa en la mesa
farol|m|objeto,sirve para alumbrar,da luz,metal
faro|m|objeto,sirve para alumbrar,da luz
antorcha|f|objeto,sirve para alumbrar,da luz,caliente,mango
esponja|f|objeto,sirve para limpiar,sirve para lavar,blando,se usa en la cocina,se usa en el baño
detergente|m|objeto,sirve para limpiar,sirve para lavar,líquido
trapo|m|objeto,sirve para limpiar,tela,blando
champú|m|objeto,sirve para lavar,líquido,se usa en el baño
sacacorchos|m|utensilio de cocina,sirve para abrir,metal,se usa en la cocina
abrecartas|m|objeto,sirve para abrir,metal
maleta|f|objeto,sirve para guardar cosas,sirve para llevar cosas,asa
baúl|m|mueble,sirve para guardar cosas,madera
cofre|m|objeto,sirve para guardar cosas,madera
bolso|m|objeto,sirve para guardar cosas,sirve para llevar cosas,asa,tela
cajón|m|objeto,sirve para guardar cosas,madera,asa
rodillo|m|herramienta,sirve para pintar,mango
crayón|m|material escolar,sirve para pintar,sirve para escribir,colores
marcador|m|material escolar,sirve para pintar,sirve para escribir,colores
lupa|f|objeto,sirve para ver,vidrio,mango
telescopio|m|aparato,sirve para ver,vidrio,metal
microscopio|m|aparato,sirve para ver,vidrio,metal
branquia|f|parte del cuerpo,sirve para respirar
parrilla|f|utensilio de cocina,sirve para cocinar,va al fuego,metal
microondas|m|aparato,sirve para cocinar,se usa en la cocina,caliente
silbato|m|instrumento,se sopla,metal
trombón|m|instrumento,se sopla,metal
clarinete|m|instrumento,se sopla,madera
armónica|f|instrumento,se sopla,metal
pandereta|f|instrumento,se golpea
bombo|m|instrumento,se golpea
platillos|m|instrumento,se golpea,metal,redondo
maceta|f|objeto,se usa en el jardín
ducha|f|objeto,se usa en el baño,agua
bañera|f|objeto,se usa en el baño,agua
lancha|f|vehículo,flota,va por el agua,navega,motor
velero|m|vehículo,flota,va por el agua,navega,sin motor
canoa|f|vehículo,flota,va por el agua,navega,sin motor
kayak|m|vehículo,flota,va por el agua,navega,sin motor
dron|m|aparato,vuela,va por el aire,motor,hélices
planeador|m|vehículo,vuela,va por el aire,alas,sin motor
casco|m|objeto,se viste,se pone en la cabeza,duro
gorra|f|objeto,se viste,se pone en la cabeza,tela
boina|f|objeto,se viste,tela,se pone en la cabeza
diadema|f|objeto,se pone en la cabeza
zapatilla|f|calzado,se pone en los pies,tela
pantufla|f|calzado,se pone en los pies,tela,blando
peine|m|objeto,dientes
erizo|m|animal,espinas,salvaje,cuatro patas
# ---- cosas que solo sirven de intrusas en las trampas (con sus propiedades)
jaula|f|objeto,metal,sirve para guardar animales
clavo|m|objeto,metal,punta,duro
tornillo|m|objeto,metal,punta
pared|f|objeto,se usa en la casa,duro
papel|m|material escolar,fino,blanco
pintura|f|objeto,líquido,colores
sábana|f|objeto,tela,sirve para cubrir la cama,se usa en la casa
mantel|m|objeto,tela,se usa en la mesa
lente|m|objeto,vidrio,sirve para ver
anillo|m|objeto,metal,redondo,se pone en el dedo
ancla|f|objeto,metal,pesado
riel|m|objeto,metal
calle|f|objeto,duro
sombra|f|natural,oscuro
sombrilla|f|objeto,tela,sirve para protegerse del sol,se abre
puente|m|objeto,duro,sirve para cruzar
lava|f|natural,caliente,quema
horno|m|aparato,caliente,sirve para cocinar,se usa en la cocina
mantequilla|f|comida,lácteo,se hace con leche
pizarra|f|material escolar,se escribe en ella,se usa en la escuela
ladrillo|m|objeto,duro,sirve para construir
estetoscopio|m|objeto,metal,sirve para escuchar
manguera|f|objeto,sirve para regar,sirve para llevar agua,se usa en el jardín
caparazón|m|natural,duro
cortaúñas|m|objeto,sirve para cortar,metal,pequeño
cortacésped|m|herramienta,sirve para cortar,se usa en el jardín,motor
ceniza|f|natural,polvo,gris
lana|f|natural,suave,tela,abriga
madera|f|natural,duro,sirve para construir
flor|f|flor,pétalos
puerto|m|lugar,construido
garaje|m|lugar,construido
remo|m|objeto,madera,sirve para mover el bote
roble|m|árbol,raíces,tronco,hojas
olivo|m|árbol,raíces,tronco,hojas
peluche|m|juguete,blando,tela
trompo|m|juguete,madera,gira
rompecabezas|m|juguete,piezas
# ---- oficios
médico|m|oficio
bombero|m|oficio
cocinero|m|oficio
panadero|m|oficio
pintor|m|oficio
carpintero|m|oficio
maestro|m|oficio
pescador|m|oficio
jardinero|m|oficio
peluquero|m|oficio
músico|m|oficio
granjero|m|oficio
dentista|m|oficio
albañil|m|oficio
zapatero|m|oficio
astronauta|m|oficio
marinero|m|oficio
piloto|m|oficio
conductor|m|oficio
# ---- lugares
escuela|f|lugar,construido
hospital|m|lugar,construido
biblioteca|f|lugar,construido
granja|f|lugar
cocina|f|lugar,se usa en la casa,construido
baño|m|lugar,se usa en la casa,construido
dormitorio|m|lugar,se usa en la casa,construido
jardín|m|lugar
museo|m|lugar,construido
mercado|m|lugar,construido
aeropuerto|m|lugar,construido
estación|f|lugar,construido
"""

# palabras con doble sentido o poco claras: NO entran en ningún grupo
AMBIGUAS = {
    "banqueta", "regadera", "rastrillo", "borrador",
    "ratón", "banco", "planta", "sierra", "cola", "mango", "cometa", "goma", "hoja", "tierra", "vela", "copa", "carta", "bolsa",
    "lima", "pie de", "gato hidráulico", "llama", "pila", "radio de", "nota", "tabla", "bomba", "cabo", "cuerno", "dado",
    "estrella de mar", "mono de", "papa", "patata", "durazno", "melocotón", "frutilla", "tomate", "pepino", "pimiento", "calabaza",
}

# (palabra, propiedad): "a veces" o según el país. Si un grupo depende de esa propiedad (como regla o como lo que le falta a la
# intrusa), el grupo se descarta.
DUDOSO = {
    ("gallina", "vuela"), ("gallo", "vuela"), ("pato", "vuela"), ("ganso", "vuela"), ("cisne", "vuela"), ("pingüino", "vuela"),
    ("avestruz", "vuela"), ("escarabajo", "vuela"), ("grillo", "vuela"), ("canguro", "cuatro patas"), ("ardilla", "vive en el agua"),
    ("cangrejo", "vive en el mar"), ("salmón", "vive en el mar"), ("trucha", "vive en el mar"), ("oso", "come carne"),
    ("zorro", "come carne"), ("cerdo", "come hierba"), ("tortuga", "vive en el agua"), ("rana", "vive en el agua"), ("sapo", "vive en el agua"),
    ("pato", "vive en el agua"), ("cisne", "vive en el agua"), ("ganso", "vive en el agua"), ("paloma", "salvaje"), ("loro", "salvaje"),
    ("pez", "vive en el mar"), ("mono", "cuatro patas"), ("ballena", "nada"), ("ardilla", "salvaje"), ("pingüino", "vive en el mar"),
    ("oveja", "da leche"), ("hormiga", "salvaje"), ("tortuga", "mascota"), ("pez", "salvaje"), ("lagarto", "salvaje"),
    ("pulpo", "ocho patas"), ("araña", "salvaje"), ("elefante", "pelo"), ("cebra", "salvaje"), ("jirafa", "salvaje"),
    ("lengua", "sirve para hablar"), ("cabeza", "sirve para ver"), ("pantalón", "sirve para abrigarse"), ("bota", "sirve para abrigarse"),
    ("tetera", "metal"), ("cuchara", "metal"), ("tenedor", "metal"), ("cuchillo", "metal"),
    ("sofá", "cuatro patas"), ("armario", "madera"), ("estante", "madera"), ("cómoda", "madera"), ("escritorio", "madera"),
    ("lámpara", "da luz"), ("hielo", "agua"), ("huevo", "comida"), ("tren", "metal"), ("avión", "metal"), ("cohete", "metal"),
    ("vestido", "tela"), ("zapato", "cuero"), ("bota", "cuero"),
}

# más "a veces / según quién": se suman a DUDOSO
DUDOSO |= {
    ("lana", "pelo"),
    ("limón", "dulce"), ("kiwi", "dulce"), ("zanahoria", "dulce"), ("cebolla", "dulce"), ("kiwi", "crece en árbol"),
    ("dedo", "sirve para comer"), ("mano", "sirve para comer"), ("camión", "sirve para guardar cosas"), ("auto", "sirve para guardar cosas"),
    ("dron", "sirve para transportar"), ("perro", "granja"), ("gato", "granja"), ("leche", "dulce"), ("café", "dulce"), ("té", "dulce"),
    ("libélula", "vive en el agua"), ("mosquito", "vive en el agua"), ("bolígrafo", "herramienta"), ("lápiz", "herramienta"),
    ("marcador", "herramienta"), ("tiza", "herramienta"), ("crayón", "herramienta"), ("sacapuntas", "herramienta"), ("abrecartas", "herramienta"),
    ("cortaúñas", "herramienta"), ("llave", "herramienta"), ("aguja", "herramienta"), ("tijera", "mueble"), ("pelota", "material escolar"),
    ("pandereta", "material escolar"), ("tambor", "material escolar"), ("xilófono", "material escolar"), ("flauta", "material escolar"),
    ("mochila", "material escolar"), ("pizarra", "mueble"), ("escritorio", "material escolar"), ("silla", "material escolar"),
    ("mesa", "material escolar"), ("lente", "herramienta"), ("lupa", "herramienta"), ("jarra", "utensilio de cocina"),
    ("rosa", "hojas"), ("margarita", "hojas"), ("girasol", "hojas"), ("tulipán", "hojas"), ("flor", "hojas"),
    ("loro", "salvaje"), ("canario", "salvaje"), ("tortuga", "salvaje"), ("tortuga", "granja"), ("conejo", "granja"), ("pez", "granja"),
    ("lluvia", "está en el cielo"), ("nieve", "está en el cielo"), ("pingüino", "vive en el agua"), ("lluvia", "caliente"),
    ("pato", "salvaje"), ("ganso", "salvaje"), ("cisne", "mascota"), ("ardilla", "mascota"), ("serpiente", "mascota"), ("lagarto", "mascota"),
    ("rana", "mascota"), ("cerdo", "mascota"), ("caballo", "mascota"), ("paloma", "mascota"), ("araña", "mascota"), ("caballo", "salvaje"),
    ("cerdo", "salvaje"), ("oveja", "salvaje"), ("vaca", "salvaje"), ("cabra", "salvaje"), ("gallina", "salvaje"), ("gallo", "salvaje"),
    ("perro", "salvaje"), ("gato", "salvaje"), ("conejo", "salvaje"), ("loro", "granja"), ("canario", "granja"),
    ("avión", "ruedas"), ("helicóptero", "ruedas"), ("barco", "sin motor"), ("bote", "motor"), ("planeador", "motor"),
    ("pingüino", "alas"), ("murciélago", "cuatro patas"), ("delfín", "pelo"), ("ballena", "pelo"), ("cerdo", "pelo"), ("elefante", "pelo"),
    ("canguro", "dos patas"), ("fuego", "sirve para cocinar"), ("fuego", "sirve para alumbrar"), ("sol", "sirve para alumbrar"),
    ("luna", "sirve para alumbrar"), ("estrella", "sirve para alumbrar"), ("relámpago", "sirve para alumbrar"),
    ("cama", "sirve para sentarse"), ("baúl", "sirve para sentarse"), ("mesa", "sirve para sentarse"), ("escritorio", "sirve para sentarse"),
    ("cajón", "sirve para sentarse"), ("maleta", "sirve para sentarse"), ("cofre", "sirve para sentarse"), ("colchón", "sirve para sentarse"),
    ("alfombra", "sirve para sentarse"), ("almohada", "sirve para sentarse"), ("estante", "sirve para sentarse"), ("cómoda", "sirve para sentarse"),
    ("sofá", "sirve para dormir"), ("sillón", "sirve para dormir"), ("alfombra", "sirve para dormir"), ("manta", "sirve para dormir"),
    ("sábana", "sirve para dormir"), ("silla", "sirve para dormir"),
    ("botella", "sirve para beber"), ("jarra", "sirve para beber"), ("tetera", "sirve para beber"), ("plato", "sirve para beber"),
    ("cuchillo", "sirve para comer"), ("plato", "sirve para comer"), ("boca", "sirve para beber"),
    ("cuchillo", "sirve para cocinar"), ("cuchara", "sirve para cocinar"), ("colador", "sirve para cocinar"), ("tenedor", "sirve para cocinar"),
    ("abrelatas", "sirve para cocinar"), ("sacacorchos", "sirve para cocinar"), ("plancha", "sirve para cocinar"), ("tostadora", "sirve para cocinar"),
    ("tetera", "sirve para cocinar"), ("cuchara", "sirve para servir líquidos"),
    ("cuchillo", "sirve para abrir"), ("tijera", "sirve para abrir"), ("destornillador", "sirve para abrir"), ("martillo", "sirve para abrir"),
    ("llave", "sirve para cortar"), ("hacha", "sirve para cortar"), ("serrucho", "sirve para abrir"), ("pala", "sirve para cortar"),
    ("cuchara", "sirve para cortar"), ("tenedor", "sirve para cortar"), ("regla", "sirve para cortar"), ("sacapuntas", "sirve para cortar"),
    ("abrecartas", "sirve para cortar"), ("rastrillo", "sirve para cortar"),
    ("lápiz", "sirve para pintar"), ("bolígrafo", "sirve para pintar"), ("tiza", "sirve para pintar"), ("pizarra", "sirve para pintar"),
    ("cuaderno", "sirve para pintar"), ("pegamento", "sirve para pintar"),
    ("pincel", "sirve para escribir"), ("brocha", "sirve para escribir"), ("crayón", "sirve para escribir"), ("pizarra", "sirve para escribir"),
    ("teléfono", "sirve para escribir"), ("televisión", "sirve para escribir"),
    ("pantalón", "sirve para abrigarse"), ("calcetín", "sirve para abrigarse"), ("zapato", "sirve para abrigarse"), ("camisa", "sirve para abrigarse"),
    ("falda", "sirve para abrigarse"), ("vestido", "sirve para abrigarse"), ("sombrero", "sirve para abrigarse"), ("alfombra", "sirve para abrigarse"),
    ("cortina", "sirve para abrigarse"), ("sábana", "sirve para abrigarse"), ("toalla", "sirve para abrigarse"), ("fuego", "sirve para abrigarse"),
    ("estufa", "sirve para abrigarse"), ("horno", "sirve para abrigarse"), ("plancha", "sirve para abrigarse"),
    ("jabón", "sirve para limpiar"), ("toalla", "sirve para limpiar"), ("agua", "sirve para limpiar"), ("lavadora", "sirve para limpiar"),
    ("esponja", "sirve para limpiar"), ("lluvia", "sirve para limpiar"), ("ducha", "sirve para limpiar"), ("bañera", "sirve para limpiar"),
    ("manguera", "sirve para limpiar"), ("cepillo", "sirve para limpiar"), ("papel", "sirve para limpiar"), ("servilleta", "sirve para limpiar"),
    ("maleta", "sirve para guardar cosas"), ("mochila", "sirve para guardar cosas"), ("botella", "sirve para guardar cosas"),
    ("armario", "sirve para guardar cosas"), ("caja", "sirve para guardar cosas"), ("jaula", "sirve para guardar cosas"),
    ("pecera", "sirve para guardar cosas"), ("olla", "sirve para guardar cosas"), ("jarra", "sirve para guardar cosas"),
    ("plato", "sirve para guardar cosas"), ("cantimplora", "sirve para guardar cosas"), ("termo", "sirve para guardar cosas"),
    ("sombrilla", "sirve para alumbrar"), ("televisión", "sirve para alumbrar"), ("reloj", "sirve para alumbrar"), ("espejo", "sirve para alumbrar"),
    ("estrella", "sirve para ver"), ("espejo", "sirve para ver"), ("ventana", "sirve para ver"), ("televisión", "sirve para ver"),
    ("cabeza", "sirve para ver"), ("puerta", "sirve para abrir"), ("ventana", "sirve para abrir"), ("mano", "sirve para abrir"),
    ("dedo", "sirve para abrir"), ("llave", "sirve para guardar cosas"), ("candado", "sirve para abrir"),
    ("moto", "sirve para sentarse"), ("bicicleta", "sirve para sentarse"), ("auto", "sirve para sentarse"), ("camión", "sirve para sentarse"),
    ("autobús", "sirve para sentarse"), ("tren", "sirve para sentarse"), ("avión", "sirve para sentarse"), ("barco", "sirve para sentarse"),
    ("globo", "sirve para transportar"), ("mochila", "sirve para transportar"), ("maleta", "sirve para transportar"), ("caballo", "sirve para transportar"),
    ("burro", "sirve para transportar"), ("camello", "sirve para transportar"), ("elefante", "sirve para transportar"), ("río", "sirve para transportar"),
    ("viento", "sirve para transportar"), ("puente", "sirve para transportar"), ("calle", "sirve para transportar"), ("riel", "sirve para transportar"),
    ("estación", "sirve para transportar"), ("aeropuerto", "sirve para transportar"), ("ancla", "sirve para transportar"),
    ("bolso", "sirve para transportar"), ("cuerda", "sirve para transportar"),
}

# palabras con la propiedad "va con" otras: ver asociaciones.py
