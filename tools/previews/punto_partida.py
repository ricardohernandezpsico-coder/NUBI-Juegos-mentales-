# Réplica aproximada (PIL) de "Tu punto de partida": metas del onboarding (OnboardingScreen.kt, GoalsPage),
# presentación de la evaluación (StartingPointPage) y el mapa final (BaselineScreen.kt). 1 dp = 3 px.
import os, math, random, importlib.util
import numpy as np
from PIL import Image, ImageDraw, ImageFont
HERE=os.path.dirname(__file__); ROOT=os.path.abspath(os.path.join(HERE,'..','..'))
spec=importlib.util.spec_from_file_location('iconos',os.path.join(HERE,'iconos_juegos.py')); ic=importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
FB=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-Bold.ttf'; FS=ROOT+'/unity/NeuroVidaCore/Assets/Resources/Fonts/Fredoka-SemiBold.ttf'
DP=3; W,H=412*DP,915*DP; INK=(26,18,64); SUN=(255,201,60); CREAM=(255,255,255); DIM=(180,191,234); LIME=(155,229,100)
def F(sz,b=True): return ImageFont.truetype(FB if b else FS,int(sz*DP))
def sky():
    t=np.linspace(1,0,H)[:,None,None]; b=np.array([4,6,28.]); m=np.array([8,14,58.]); tp=np.array([16,26,88.])
    bg=np.repeat(np.where(t<0.5,b+(m-b)*(t*2),m+(tp-m)*((t-0.5)*2)),W,axis=1); Y,X=np.mgrid[0:H,0:W]
    for cx,cy,r,rgb,a in [(0.9*W,0.1*H,0.9*W,(76,201,240),.27),(0.05*W,0.95*H,0.85*W,(255,107,74),.24)]:
        d=np.clip(1-np.sqrt((X-cx)**2+(Y-cy)**2)/r,0,1)**2*a; bg[:]=bg*(1-d[...,None])+np.array(rgb)*d[...,None]
    im=Image.fromarray(bg.clip(0,255).astype(np.uint8)).convert('RGBA'); d=ImageDraw.Draw(im); rnd=random.Random(3)
    for i in range(110):
        x,y=rnd.random()*W,rnd.random()*H; r=rnd.random()*3+1; d.ellipse([x-r,y-r,x+r,y+r],fill=(255,255,255,int(90+rnd.random()*140)))
    return im
def ctext(d,y,s,font,fill,maxw=None):
    lines=[s]
    if maxw:
        lines=[]; cur=''
        for w_ in s.split():
            n=(cur+' '+w_).strip()
            if d.textlength(n,font=font)<=maxw: cur=n
            else: lines.append(cur); cur=w_
        lines.append(cur)
    for ln in lines: d.text((W/2-d.textlength(ln,font=font)/2,y),ln,font=font,fill=fill); y+=font.size*1.35
    return y
def button(d,y,label,fill):
    x0,x1=24*DP,W-24*DP; h=58*DP
    d.rounded_rectangle([x0,y+5*DP,x1,y+h+5*DP],radius=20*DP,fill=INK); d.rounded_rectangle([x0,y,x1,y+h],radius=20*DP,fill=fill,outline=INK,width=3*DP)
    d.text((W/2,y+h/2),label,font=F(20),fill=INK,anchor='mm')
def dots(d,page,n=9):
    x=W/2-(22+8*(n-1)+6*(n-1))*DP/2; y=30*DP
    for i in range(n):
        w=(22 if i==page else 8)*DP; d.rounded_rectangle([x,y,x+w,y+8*DP],radius=4*DP,fill=SUN if i<=page else (60,68,110)); x+=w+6*DP
def back(d): d.ellipse([24*DP,20*DP,68*DP,64*DP],fill=CREAM,outline=INK,width=3*DP); c=(46*DP,42*DP); w=int(3*DP); d.line([c[0]+9*DP,c[1],c[0]-8*DP,c[1]],fill=INK,width=w); d.line([c[0]-9*DP,c[1],c[0]-1*DP,c[1]-8*DP],fill=INK,width=w); d.line([c[0]-9*DP,c[1],c[0]-1*DP,c[1]+8*DP],fill=INK,width=w)
def planet(im,cx,cy,r,gid,dom):
    d=ImageDraw.Draw(im); d.ellipse([cx-r,cy-r+4*DP,cx+r,cy+r+4*DP],fill=INK); d.ellipse([cx-r,cy-r,cx+r,cy+r],fill=ic.DOM[dom],outline=INK,width=3*DP)
    k=r*1.3/100; ic.draw(ic.Icon(im,cx-50*k,cy-50*k,k),gid)
DOMS=[('MEMORIA','Memoria','memoria'),('ATENCION','Atención','atencion'),('RAZONAMIENTO','Razonamiento','razonamiento'),
      ('LENGUAJE','Lenguaje','lenguaje'),('CALCULO','Cálculo','calculo'),('VELOCIDAD','Velocidad','velocidad')]
# ---- metas ----
im=sky(); d=ImageDraw.Draw(im); dots(d,4); back(d)
y=ctext(d,120*DP,'¿Qué quieres entrenar?',F(28),CREAM)+6*DP
y=ctext(d,y,'Elige hasta 3. Tu camino de cada día les dará prioridad.',F(16,False),DIM,W-60*DP)+22*DP
chosen={'MEMORIA','LENGUAJE'}
for key,label,dom in DOMS:
    on=key in chosen; x0,x1,h=24*DP,W-24*DP,64*DP
    d.rounded_rectangle([x0,y+4*DP,x1,y+h+4*DP],radius=20*DP,fill=INK)
    d.rounded_rectangle([x0,y,x1,y+h],radius=20*DP,fill=(255,244,214) if on else (36,44,96),outline=INK,width=3*DP)
    cy=y+h/2; d.ellipse([x0+22*DP-9*DP,cy-9*DP,x0+22*DP+9*DP,cy+9*DP],fill=ic.DOM[dom],outline=INK,width=2*DP)
    d.text((x0+44*DP,cy),label,font=F(19),fill=INK if on else CREAM,anchor='lm')
    bx=x1-36*DP; d.rounded_rectangle([bx-13*DP,cy-13*DP,bx+13*DP,cy+13*DP],radius=8*DP,fill=LIME if on else (60,68,110),outline=INK,width=int(2.5*DP))
    if on: d.text((bx,cy),'✓',font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',15*DP) if os.path.exists('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf') else F(15),fill=INK,anchor='mm')
    y+=h+12*DP
button(d,H-110*DP,'Siguiente',SUN); p1=im
# ---- presentación ----
im=sky(); d=ImageDraw.Draw(im); dots(d,8); back(d)
y=ctext(d,150*DP,'Encontremos tu',F(30),CREAM); y=ctext(d,y,'punto de partida',F(30),CREAM)+8*DP
y=ctext(d,y,'3 juegos cortos, unos 5 minutos. No es un examen: con esto cada juego empieza a tu medida.',F(16,False),DIM,W-64*DP)+50*DP
steps=[('secuencia','Memoria','Secuencia Lumínica','memoria'),('stroop','Atención','Tinta o Palabra','atencion'),('comparacion','Velocidad','Comparación','velocidad')]
for i,(gid,dn,gn,dom) in enumerate(steps):
    cx=W/2+(i-1)*125*DP; planet(im,cx,y+40*DP,38*DP,gid,dom); d=ImageDraw.Draw(im)
    d.text((cx,y+100*DP),f'{i+1}. {dn}',font=F(15),fill=CREAM,anchor='mm'); d.text((cx,y+120*DP),gn,font=F(12,False),fill=DIM,anchor='mm')
button(d,H-150*DP,'Empezar',SUN); d.text((W/2,H-62*DP),'Hacerlo después',font=F(16,False),fill=(234,240,255),anchor='mm'); p2=im
# ---- mapa ----
im=sky(); d=ImageDraw.Draw(im)
y=ctext(d,70*DP,'Tu mapa',F(34),CREAM)+4*DP
y=ctext(d,y,'Tu punto de partida. Cada juego empieza ahora a tu medida y se ajusta mientras juegas.',F(15,False),DIM,W-64*DP)+14*DP
vals={'MEMORIA':.62,'ATENCION':.48,'VELOCIDAD':.55}; mean=sum(vals.values())/3
cx,cy,R=W/2,y+135*DP,105*DP
ang=[-math.pi/2+i*math.pi/3 for i in range(6)]
for f in (1/3,2/3,1):
    d.polygon([(cx+math.cos(a)*R*f,cy+math.sin(a)*R*f) for a in ang],outline=(70,80,130),width=int(1.5*DP))
for a in ang: d.line([cx,cy,cx+math.cos(a)*R,cy+math.sin(a)*R],fill=(55,64,110),width=DP)
pts=[(cx+math.cos(a)*R*(vals.get(k,mean)),cy+math.sin(a)*R*(vals.get(k,mean))) for a,(k,_,_) in zip(ang,DOMS)]
o=Image.new('RGBA',im.size,(0,0,0,0)); ImageDraw.Draw(o).polygon(pts,fill=(255,201,60,70)); im.alpha_composite(o); d=ImageDraw.Draw(im)
d.line(pts+[pts[0]],fill=SUN,width=int(2.5*DP))
for (px,py),(k,label,dom),a in zip(pts,DOMS,ang):
    r=9*DP
    if k in vals: d.ellipse([px-r,py-r+3*DP,px+r,py+r+3*DP],fill=INK); d.ellipse([px-r,py-r,px+r,py+r],fill=ic.DOM[dom],outline=INK,width=int(2.5*DP))
    else:
        for s in range(0,360,40): d.arc([px-r,py-r,px+r,py+r],s,s+22,fill=ic.DOM[dom],width=int(2.5*DP))
    lx,ly=cx+math.cos(a)*(R+30*DP),cy+math.sin(a)*(R+22*DP); d.text((lx,ly),label,font=F(13),fill=CREAM if k in vals else DIM,anchor='mm')
y=cy+R+50*DP
for k,label,dom,word,p in [('MEMORIA','Memoria','memoria','Alto',71),('ATENCION','Atención','atencion','Medio',51),('VELOCIDAD','Velocidad','velocidad','Medio',61)]:
    d.ellipse([28*DP,y+4*DP,44*DP,y+20*DP],fill=ic.DOM[dom],outline=INK,width=2*DP)
    d.text((56*DP,y),label,font=F(19),fill=CREAM); d.text((56*DP,y+26*DP),word,font=F(14,False),fill=DIM)
    d.text((W-28*DP,y+6*DP),f'P{p}',font=F(20),fill=SUN,anchor='ra'); y+=58*DP
y=ctext(d,y+4*DP,'Razonamiento, lenguaje y cálculo se estiman hasta que los juegues. La comparación es una estimación provisional.',F(12,False),DIM,W-64*DP)
button(d,H-110*DP,'Empezar mi camino',SUN); p3=im
out=Image.new('RGB',(W*3+80,H),(0,0,0))
for i,p in enumerate((p1,p2,p3)): out.paste(p.convert('RGB'),(i*(W+40),0))
path=ROOT+'/docs/previews/punto-partida.png'; out.resize((out.width//3,out.height//3),Image.LANCZOS).save(path); print(path)
