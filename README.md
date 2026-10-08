<div align="center">

<img src="app/src/main/res/mipmap-xxxhdpi/ic_launcher_round.webp" width="96" alt="Nubi, la nube donde nacen las estrellas" />

# Nubi

**Juegos cortos para mantener la mente activa: 19 juegos en 4 áreas.**

</div>

Nubi es una app Android para jugar unos minutos al día. Cada día propone un camino de 3 juegos (unos 5 minutos) entre 19 juegos en 4 áreas: Memoria, Atención, Razonamiento y Lenguaje. La dificultad se ajusta a cómo juegas, y ves tu avance en cada área, tu liga y tus logros.
Todo se guarda en tu teléfono: sin cuenta, sin internet y sin anuncios.

**Nubi es un juego, no una herramienta de salud**: no diagnostica ni trata nada y no promete resultados. La medida que muestran algunos juegos al final es de esa partida, no un diagnóstico.

Tres pestañas: **Hoy** (Nubi al centro, tus 4 áreas y el camino del día), **Juegos** (todos los juegos por área, con su ficha) y **Avance** (tu liga, tus áreas, tus partidas y tus logros).

Cómo está hecho: la app es Kotlin con Jetpack Compose (Material 3, Room y WorkManager) y los juegos están hechos con Unity 6 e incrustados en la app. En desarrollo, todavía sin publicar.

Cómo compilar y probar: hace falta el Android SDK, JDK 21 y Unity 6000.0.84f1 con soporte Android; `bash tools/verificar-todo.sh` corre todo (Unity, Kotlin e instalación opcional). El detalle técnico, las reglas del proyecto y el mapa de documentos están en [`CLAUDE.md`](CLAUDE.md) y en [`docs/`](docs/).
