"""Comprueba que una paleta de colores se distinga también con daltonismo.

Simula protanopía, deuteranopía y tritanopía (Machado, Oliveira y Fernandes 2009, severidad 1.0, en RGB lineal) y mide
la diferencia mínima entre pares con CIEDE2000. Regla de Nubi: ≥ 20 en visión típica, protanopía y deuteranopía para
colores que el jugador tiene que distinguir (ver docs/diseno-tinta-o-palabra.md).

Uso: python tools/paleta_daltonismo.py C93C3C 4A86E8 F2CC1D F4F4F4 [--fondo 141C33]
"""
import itertools, sys
import numpy as np
# Machado 2009 severity 1.0 (linear RGB)
M={'normal':np.eye(3),
'protan':np.array([[0.152286,1.052583,-0.204868],[0.114503,0.786281,0.099216],[-0.003882,-0.048116,1.051998]]),
'deutan':np.array([[0.367322,0.860646,-0.227968],[0.280085,0.672501,0.047413],[-0.011820,0.042940,0.968881]]),
'tritan':np.array([[1.255528,-0.076749,-0.178779],[-0.078411,0.930809,0.147602],[0.004733,0.691367,0.303900]])}
def lin(c): c=c/255; return np.where(c<=0.04045,c/12.92,((c+0.055)/1.055)**2.4)
def lab(rgbl):
    rgbl=np.clip(rgbl,0,1)
    X=np.array([[0.4124,0.3576,0.1805],[0.2126,0.7152,0.0722],[0.0193,0.1192,0.9505]])@rgbl
    X=X/np.array([0.95047,1,1.08883])
    f=np.where(X>0.008856,np.cbrt(X),7.787*X+16/116)
    return np.array([116*f[1]-16,500*(f[0]-f[1]),200*(f[1]-f[2])])
def de2000(a,b):
    L1,a1,b1=a;L2,a2,b2=b
    C1=np.hypot(a1,b1);C2=np.hypot(a2,b2);Cb=(C1+C2)/2
    G=0.5*(1-np.sqrt(Cb**7/(Cb**7+25**7)))
    a1p,a2p=(1+G)*a1,(1+G)*a2
    C1p,C2p=np.hypot(a1p,b1),np.hypot(a2p,b2)
    h1=np.degrees(np.arctan2(b1,a1p))%360;h2=np.degrees(np.arctan2(b2,a2p))%360
    dL=L2-L1;dC=C2p-C1p
    dh=h2-h1
    if C1p*C2p==0: dh=0
    elif dh>180: dh-=360
    elif dh<-180: dh+=360
    dH=2*np.sqrt(C1p*C2p)*np.sin(np.radians(dh/2))
    Lb=(L1+L2)/2;Cbp=(C1p+C2p)/2
    hb=(h1+h2)/2 if abs(h1-h2)<=180 else (h1+h2+360)/2
    if C1p*C2p==0: hb=h1+h2
    T=1-0.17*np.cos(np.radians(hb-30))+0.24*np.cos(np.radians(2*hb))+0.32*np.cos(np.radians(3*hb+6))-0.2*np.cos(np.radians(4*hb-63))
    SL=1+0.015*(Lb-50)**2/np.sqrt(20+(Lb-50)**2);SC=1+0.045*Cbp;SH=1+0.015*Cbp*T
    dt=30*np.exp(-((hb-275)/25)**2);RC=2*np.sqrt(Cbp**7/(Cbp**7+25**7));RT=-np.sin(np.radians(2*dt))*RC
    return np.sqrt((dL/SL)**2+(dC/SC)**2+(dH/SH)**2+RT*(dC/SC)*(dH/SH))
def rgb(h): return np.array([int(h[i:i+2], 16) for i in (0, 2, 4)], float)
def lum(h):
    l = lin(rgb(h)); return 0.2126*l[0] + 0.7152*l[1] + 0.0722*l[2]
def contraste(a, b):
    x, y = sorted([lum(a), lum(b)], reverse=True); return (x + 0.05) / (y + 0.05)

if __name__ == '__main__':
    args = [a.strip('#').upper() for a in sys.argv[1:]]
    fondo = None
    if '--FONDO' in args:
        i = args.index('--FONDO'); fondo = args[i+1]; args = args[:i] + args[i+2:]
    if len(args) < 2: sys.exit(__doc__)
    ok = True
    for v, m in M.items():
        labs = {h: lab(m @ lin(rgb(h))) for h in args}
        d = min(de2000(labs[a], labs[b]) for a, b in itertools.combinations(args, 2))
        regla = v != 'tritan'
        ok &= (d >= 20) or not regla
        print(f'{v:7s} diferencia mínima {d:5.1f}' + ('' if regla else '  (informativo)'))
    if fondo:
        for h in args: print(f'contraste #{h} sobre #{fondo}: {contraste(h, fondo):.1f}:1')
    print('OK' if ok else 'NO CUMPLE (mínimo 20)'); sys.exit(0 if ok else 1)
