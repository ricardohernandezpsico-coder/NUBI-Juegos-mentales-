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
5. Al terminar: "Tu mapa" con 3 dominios medidos (puntos llenos) y 3 estimados (puntos huecos), percentiles y el
   aviso de estimación provisional. "Empezar mi camino" lleva a Hoy.
6. La sesión de hoy incluye juegos de tus metas. En Juegos, los niveles ya no están todos en 1.

## C. Camino diario y partidas

7. Hoy → jugar la sesión completa (3 juegos). Después de cada uno: "¡Listo!" en Unity y enseguida la pantalla de
   resultado de la app (puntaje, estrellas, trofeos). "Siguiente juego" sigue el camino.
8. La segunda partida abre rápido (Unity ya está vivo). La primera tras abrir la app muestra la pantalla de carga
   del juego (planeta con luna), sin pantalla negra.
9. Al completar la sesión, el día de hoy queda marcado en el camino y la racha sube.

## D. Los 9 juegos (desde Juegos o desde los botones "[Debug]" de Ajustes)

10. Secuencia Lumínica · 11. Parejas Ocultas · 12. Ruta del Tesoro · 13. Tinta o Palabra · 14. Cambio de Chip ·
    15. Detective de Series · 16. Anagramas · 17. Cálculo Sereno · 18. Comparación.
    En cada uno: se entiende qué hacer, el arte se ve bien, suena al acertar y al fallar, y termina con "¡Listo!".

## E. Pausa y salidas

19. A mitad de partida: Atrás → menú de pausa (el reloj se detiene). Continuar sigue donde estaba.
20. Pausa → Salir → volver a abrir el mismo juego desde Hoy: aparece la pausa y se puede continuar.
21. Pausa → Reiniciar: la misma partida desde cero.
22. Botón Inicio a mitad de partida y volver con el ícono: el juego sigue ahí (en pausa).
23. Atrás en Hoy: la app pasa a segundo plano (no se ve Unity detrás).
24. Girar el teléfono en un juego y en la app: todo queda vertical, nada se desarma.

## F. Recompensas

25. Ajustes → "[Debug] Ver celebración de ascenso de liga" y "[Debug] Ver celebración de logro": se ven completas;
    "Compartir" abre la hoja de compartir con la imagen.
26. Perfil: logros (bloqueados con avance), punto de partida ("Repetir la evaluación").
27. Liga: escudo, trofeos, "Compartir mi liga".

## G. Ajustes

28. Apagar sonido y vibración → un juego queda en silencio y sin vibrar. Volver a encenderlos.
29. Notificación de prueba: muestra un texto acorde al día (si ya completaste la sesión, no aparece: es lo esperado).
30. "[Debug] Ver el onboarding otra vez" → "Hacerlo después" en el punto de partida: la app sigue normal.
