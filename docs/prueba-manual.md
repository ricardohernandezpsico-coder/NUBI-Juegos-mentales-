# Prueba manual antes de marcar una versión

Lista corta para recorrer la app completa en el teléfono (unos 30 minutos). Antes de empezar:
`git pull && bash tools/verificar-todo.sh --instalar` sin errores, y la cuenta regresiva de cualquier juego muestra
la marca de estilo actual (ver `CountdownScreen.StyleStamp` en `CLAUDE.md`).

Marca cada punto con ✓ o anota lo que viste. Si algo falla, basta con decir el número y qué pasó.

## A. Instalación limpia

1. Ajustes → "Borrar datos" (o desinstalar e instalar). La app abre el onboarding, sin nombre ni partidas previas.
2. Onboarding completo: nombre, edad, educación, hasta 3 metas (la 4ª no se deja marcar), días por semana,
   recordatorio (acepta el permiso de notificaciones).

## B. Punto de partida

3. "Empezar" → se juegan 3 juegos seguidos. Cada uno dice "Punto de partida · n de 3" en la carga y en la cuenta
   regresiva.
4. Entre juego y juego se ve el avance, con el que sigue y qué mide.
5. Al terminar: "Tu mapa" con 3 dominios medidos (puntos llenos) y 3 estimados (puntos huecos), sin percentiles. "Empezar mi camino" lleva a Hoy.
6. La sesión de hoy incluye juegos de tus metas. En Juegos, los niveles ya no están todos en 1.

## C. Camino diario y partidas

7. Hoy → botón de abajo (el juego que toca) → jugar la sesión completa (3 juegos). Después de cada uno: "¡Listo!"
   en Unity y enseguida la pantalla de resultado de la app (puntaje, estrellas, trofeos). "Siguiente juego (2 de 3)"
   sigue el camino. Un juego abierto desde la pestaña Juegos muestra "Continuar", no "Siguiente juego".
8. La segunda partida abre rápido (Unity ya está vivo). La primera tras abrir la app muestra la pantalla de carga
   del juego (planeta con luna), sin pantalla negra.
9. Al completar la sesión, el día de hoy queda marcado en el camino y la racha sube.

## D. Los 8 juegos (desde Juegos o desde los botones "[Debug]" de Ajustes)

10. Secuencia Lumínica · 11. Constelaciones · 12. Tinta o Palabra ·
    13. En la punta de la lengua · 14. Carga exacta · (Cambio de Chip se retiró el 3-oct y Comparación, Detective de Series, Ruta del Tesoro y Tráfico Estelar el 4-oct.)
    En cada uno: se entiende qué hacer, el arte se ve bien, suena al acertar y al fallar, y termina con "¡Listo!".

## E. Pausa y salidas

19. A mitad de partida: Atrás → menú de pausa (el reloj se detiene). Continuar sigue donde estaba.
20. Pausa → Salir → en Hoy aparece "Tienes una partida en pausa" con el juego; al tocarlo vuelve a la pausa y se
    puede continuar.
21. Pausa → Reiniciar: la misma partida desde cero.
22. A mitad de partida, deslizar desde el borde inferior para que aparezcan los botones del sistema (el juego los
    esconde), ir a Inicio y volver con el ícono: el juego sigue ahí (en pausa).
23. Atrás en Hoy: la app pasa a segundo plano (no se ve Unity detrás).
24. Girar el teléfono en un juego y en la app: la pantalla NO gira (queda siempre vertical, es a propósito) y nada
    se desarma.

## F. Recompensas

25. Ajustes → "[Debug] Ver celebración de ascenso de liga" y "[Debug] Ver celebración de logro": se ven completas;
    "Compartir" abre la hoja de compartir con la imagen.
26. Perfil: logros (bloqueados con avance), punto de partida ("Repetir la evaluación").
27. Liga: escudo, trofeos, "Compartir mi liga".

## G. Ajustes

28. Apagar sonido y vibración → un juego queda en silencio y sin vibrar. Volver a encenderlos.
29. Notificación de prueba: muestra un texto acorde al día (si ya completaste la sesión, no aparece: es lo esperado).
30. "[Debug] Ver el onboarding otra vez" → "Hacerlo después" en el punto de partida: la app vuelve a Hoy, que
    invita a encontrar el punto de partida.

## H. Cuando Android cierra la app mientras se juega

En teléfonos con poca memoria Android puede cerrar la app mientras un juego está abierto. Para probarlo a propósito:
Ajustes del teléfono → Opciones de desarrollador → activar **"No conservar actividades"** (desactivarlo al terminar: con
ella activa Unity arranca de cero en cada juego y la pausa no se conserva).

31. Con la opción activa, jugar la sesión diaria: después de cada juego aparece la pantalla de resultado y
    "Siguiente juego" sigue el camino.
32. Con la opción activa, hacer la evaluación ("Encuéntralo en 5 minutos" en Hoy): se juegan los 3 juegos y al final
    aparece "Tu mapa".
