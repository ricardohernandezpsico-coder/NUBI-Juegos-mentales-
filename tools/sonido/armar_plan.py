#!/usr/bin/env python3
"""Rellena las tablas medidas de docs/sonido-madera-plan.md (los marcadores __PAREJO__, __PALETA__, __AFINACION__, __LATENCIA__, __MEMORIA__, __INVENTARIO__ y __WAVS__) con lo que sale
del catálogo (tools/sonido/catalogo.json; ver tablas_plan.py). Uso (una vez que el documento tiene sus marcadores): python tools/sonido/armar_plan.py
El documento queda con las tablas ya escritas (los marcadores se reemplazan); para regenerar después, se vuelve a poner el marcador donde va la tabla."""
import io
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import tablas_plan as t  # noqa: E402

RAIZ = t.RAIZ
DOC = os.path.join(RAIZ, 'docs', 'sonido-madera-plan.md')


def main():
    with io.open(DOC, encoding='utf-8') as f:
        s = f.read()
    archivos = {c['file'] for c in t.CLIPS.values() if c['file']}
    peso = sum(c['wavBytes'] for c in t.CLIPS.values() if c['file'] and c['wavBytes'])
    reemplazos = {
        '__PAREJO__': t.parejo(),
        '__PALETA__': t.paleta(),
        '__AFINACION__': t.afinacion(),
        '__LATENCIA__': t.latencia(),
        '__MEMORIA__': t.memoria(),
        '__INVENTARIO__': t.inventario().strip(),
        '__WAVS__': '%d archivos, %.1f MB' % (len(archivos), peso / 1048576.0),
    }
    for marca, texto in reemplazos.items():
        if marca in s:
            s = s.replace(marca, texto)
    with io.open(DOC, 'w', encoding='utf-8', newline='\n') as f:
        f.write(s)
    print('listo: ' + os.path.relpath(DOC, RAIZ))


if __name__ == '__main__':
    main()
