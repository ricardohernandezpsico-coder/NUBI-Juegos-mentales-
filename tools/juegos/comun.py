# -*- coding: utf-8 -*-
"""Las piezas comunes de los juegos en plasticina (fase 0 de la Tarea 94), lo que NO sale ya hecho del kit de la app:
- las losas «noche» de las casillas de respuesta (normal, presionada, acierto, error y apagada): losas md (radio 22 dp) oscuras, para que los símbolos de color de los juegos resalten;
- una losa md crema de CONTROL (se compara con `kit_losa_md_crema` de la app para comprobar que este pipeline da el mismo kit);
- las marcas de respuesta ✓ (disco lima) y ✗ (disco coral) con el glifo de churro tinta;
- el anillo de la cuenta regresiva.
Las losas de botones y paneles (sol, crema, cielo, lila, lima, coral; ch, md, gr) NO se vuelven a renderizar: `empaqueta.py` las toma de los WebP del kit de la app (`app/src/main/res/drawable-nodpi/kit_losa_*.webp`).

Uso: blender -b -P tools/juegos/comun.py -- [--solo losas|marcas|anillo] [--samples N] [--out DIR]   (salida por defecto: tools/juegos/out/comun)
"""
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from plasticina import *

SOLO = arg('--solo', 'todo')
# los colores de diseño de las casillas de Rescate (ButtonLive, ButtonHit, ButtonWrong, ButtonDim)
DISENO = {'noche': '#2A2566', 'acierto': '#3D6A2C', 'error': '#6B2E3A', 'apagada': '#1C1A48'}

def render_losas():
    losa('losa_md_crema_control', KIT_CREMA)
    # Un color oscuro con el brillo normal del kit sale lechoso (el sol sobre la cara plana le suma un gris de ~45/255): con poco especular y sin «sheen» el centro queda a ±8/255 del color de diseño
    with material_kit(spec=0.05, sheen=0.0):
        for clave, dis in DISENO.items():
            losa('losa_md_' + clave, dis)
            if clave == 'noche': losa('losa_md_' + clave + '_p', dis, presionada=True)

# ------------------------------------------------------------------ marcas ✓ y ✗ (AnswerMarkSprite: lienzo ±1,1, disco de radio 0,8, glifo de churro tinta)
def marca(color, glifo):
    H = 0.16
    def construir(r):
        disco(0.8, H, 0.07, color, loc=(0, 0, 0), nombre='marca_disco', padre=r, verts=72)
        z = H + 0.07
        for seg in glifo: churro_c(seg[0], seg[1], 0.105, z, tinta(), 'marca_glifo')
    return construir

VISTO = (((-0.36, 0.02), (-0.10, -0.26)), ((-0.10, -0.26), (0.38, 0.30)))
ASPA = (((-0.30, -0.30), (0.30, 0.30)), ((-0.30, 0.30), (0.30, -0.30)))

def render_marcas():
    una('marca_ok', marca(KIT_LIMA, VISTO), 2.2, 2.2, 128 / 2.2, centro=(0.0, sube(0.16) / 2))
    una('marca_no', marca(KIT_CORAL, ASPA), 2.2, 2.2, 128 / 2.2, centro=(0.0, sube(0.16) / 2))

# ------------------------------------------------------------------ el anillo de la cuenta regresiva (RingSprite: radios 0,39 a 0,48 del lado; se tiñe con Image.color y se llena en radial)
def anillo_cuenta(r):
    aro_plano(0.435, 0.047, '#F3F0FF', loc=(0, 0, 0.047), nombre='cuenta_anillo', padre=r)

def render_anillo():
    una('anillo_cuenta', anillo_cuenta, 1.0, 1.0, 640.0, centro=(0.0, sube(0.094) / 2))

preparar()
if SOLO in ('todo', 'losas'): render_losas()
if SOLO in ('todo', 'marcas'): render_marcas()
if SOLO in ('todo', 'anillo'): render_anillo()
termina()
