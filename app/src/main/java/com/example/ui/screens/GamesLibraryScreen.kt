package com.example.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Speed
import androidx.compose.material.icons.filled.Timer
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.model.DomainType
import com.example.model.GameDefinition
import com.example.model.GameRankInfo
import com.example.model.GameRegistry
import com.example.model.LevelTier
import com.example.ui.components.DomainChip
import com.example.ui.i18n.LocalAppLanguage
import com.example.ui.i18n.getDomainName
import com.example.ui.i18n.getGameTitle
import com.example.ui.i18n.strings
import com.example.ui.theme.*
import com.example.viewmodel.NeuroVidaViewModel

private const val DAY_MS = 86_400_000L
private val OnNight = Color(0xFFEAF0FF)
private val OnNightDim = Color(0xFFC7D0FF)
private val OnNightSoft = Color(0xFF9AA3D6)

private fun dayIndex(ts: Long): Long = (ts + java.util.TimeZone.getDefault().getOffset(ts)) / DAY_MS

/**
 * Pestaña Juegos (28-sep, Ricardo eligió "Para ti + áreas" entre 6 estilos; maqueta `docs/previews/juegos-final-1.png`):
 * arriba "PARA TI HOY", un solo juego sugerido con su razón (ver [com.example.data.Library]; "otro" pasa al siguiente),
 * y debajo una fila por área que se desliza de lado. Cada planeta lleva un anillo con su nivel, una estrella si es juego
 * estrella, ✓ si se jugó hoy, y debajo tu marca, tu nivel o "Nuevo". Tocar un planeta abre su ventana de inicio.
 */
@Composable
fun GamesLibraryScreen(
  viewModel: NeuroVidaViewModel,
  modifier: Modifier = Modifier
) {
  val gameLevels by viewModel.gameLevels.collectAsState()
  val levels by viewModel.gameLevelsForProgress.collectAsState()
  val history by viewModel.gameHistory.collectAsState()
  val userSettings by viewModel.userSettings.collectAsState()
  val measures by viewModel.starMeasures.collectAsState()

  var gameToIntro by remember { mutableStateOf<GameDefinition?>(null) }
  var pickIndex by remember { mutableStateOf(0) }
  val now = remember(history) { System.currentTimeMillis() }

  val lastPlayed = remember(history) {
    history.groupBy { it.gameId }.mapValues { (_, list) -> list.maxOf { it.timestamp } }
  }
  val picks = remember(history) {
    val plays = history.mapNotNull { r -> GameRegistry.getById(r.gameId)?.let { com.example.data.PlanetPlay(it.domain.name, r.timestamp) } }
    val planet = com.example.data.Planet.build(plays, now, ::dayIndex)
    val games = GameRegistry.allGames
      .filter { it.id !in LibraryBookends }
      .map { com.example.data.LibraryGame(it.id, it.domain.name, com.example.data.StarMeasures.defForGame(it.id) != null) }
    com.example.data.Library.picks(planet, games, lastPlayed, now, ::dayIndex) { key ->
      DomainType.values().firstOrNull { it.name == key }?.displayName ?: key
    }
  }
  val pick = picks.getOrNull(pickIndex % picks.size.coerceAtLeast(1))

  Box(modifier = modifier.fillMaxSize()) {
    val currentLang = LocalAppLanguage.current
    LazyColumn(
      modifier = Modifier.fillMaxSize(),
      contentPadding = PaddingValues(top = 12.dp, bottom = 28.dp)
    ) {
      item {
        Text(
          text = strings.gamesLibraryTitle,
          color = OnNight,
          fontSize = 28.sp,
          fontWeight = FontWeight.Bold,
          modifier = Modifier.padding(horizontal = 20.dp)
        )
      }

      if (pick != null) {
        item {
          val def = GameRegistry.getById(pick.gameId)
          if (def != null) {
            ForYou(
              game = def,
              title = getGameTitle(def.id, currentLang, def.title),
              reason = pick.reason,
              hasMore = picks.size > 1,
              onPlay = { viewModel.launchGame(def.id) },
              onOther = { pickIndex += 1 },
              onOpen = { gameToIntro = def }
            )
          }
        }
      }

      items(DomainType.values().toList()) { domain ->
        val games = GameRegistry.allGames
          .filter { it.domain == domain }
          .sortedBy { com.example.data.StarMeasures.defForGame(it.id) == null } // los juegos estrella primero
        AreaRow(
          domain = domain,
          title = getDomainName(domain, currentLang),
          count = games.size
        ) {
          items(games, key = { it.id }) { game ->
            val played = lastPlayed[game.id]
            val latest = com.example.data.StarMeasures.latest(measures, game.id)
            val sub = when {
              played == null -> "Nuevo" to Clay.Sun
              latest != null -> latest.first.compact(latest.second) to Clay.Sun
              else -> "nivel ${gameLevels[game.id] ?: 1}" to OnNightSoft
            }
            LibraryPlanet(
              game = game,
              title = getGameTitle(game.id, currentLang, game.title),
              level = levels[game.id],
              star = com.example.data.StarMeasures.defForGame(game.id) != null,
              playedToday = played != null && dayIndex(played) == dayIndex(now),
              isNew = played == null,
              sub = sub.first,
              subColor = sub.second,
              onClick = { gameToIntro = game }
            )
          }
        }
      }
    }

    // Game Intro Dialog
    gameToIntro?.let { game ->
      // OJO: antes esto leía gameLevels[game.id] directo, que es el nivel adaptativo
      // guardado sin importar el modo de dificultad elegido en Ajustes — significaba
      // que "Avanzado" (nivel 5 fijo) nunca se aplicaba de verdad al jugar. Ahora usa
      // el nivel efectivo del ViewModel (respeta Principiante/Intermedio/Avanzado/
      // Personalizada) como base, y Suave/Desafío siguen ajustando ±1 sobre esa base.
      val currentLevel = viewModel.getEffectiveLevelForGame(game.id)
      var levelOffset by remember { mutableStateOf(0) } // -1 (Suave), 0 (Equilibrado), +1 (Desafío)
      var isTimedMode by remember { mutableStateOf(userSettings.defaultTimed) }

      val effectiveLevel = (currentLevel + levelOffset).coerceIn(1, 5)
      val effectiveTier = LevelTier.fromLevel(effectiveLevel)

      Dialog(onDismissRequest = { gameToIntro = null }) {
        com.example.ui.theme.ClayLightCard(
          modifier = Modifier
            .fillMaxWidth()
            .padding(16.dp)
        ) {
          Column(
            modifier = Modifier
              .fillMaxWidth()
              .padding(22.dp),
            horizontalAlignment = Alignment.CenterHorizontally
          ) {
            Row(
              modifier = Modifier.fillMaxWidth(),
              horizontalArrangement = Arrangement.SpaceBetween,
              verticalAlignment = Alignment.CenterVertically
            ) {
              DomainChip(domain = game.domain)
              IconButton(onClick = { gameToIntro = null }) {
                Icon(Icons.Default.Close, contentDescription = "Cerrar")
              }
            }

            Spacer(modifier = Modifier.height(12.dp))

            com.example.ui.components.GameIcon(game.id, size = 72.dp)
            Spacer(modifier = Modifier.height(8.dp))

            Text(
              text = game.title,
              style = MaterialTheme.typography.headlineSmall,
              fontWeight = FontWeight.Bold,
              color = MaterialTheme.colorScheme.onSurface
            )

            Text(
              text = game.instruction,
              style = MaterialTheme.typography.bodyMedium,
              color = MaterialTheme.colorScheme.onSurfaceVariant,
              modifier = Modifier.padding(top = 8.dp),
              lineHeight = 20.sp
            )

            Spacer(modifier = Modifier.height(16.dp))
            HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant)
            Spacer(modifier = Modifier.height(16.dp))

            // Difficulty adjustment selector: Suave / Equilibrado / Desafío
            Text(
              text = "Ajuste de Dificultad: Nivel $effectiveLevel (${effectiveTier.tierName})",
              style = MaterialTheme.typography.labelMedium,
              fontWeight = FontWeight.Bold,
              color = MaterialTheme.colorScheme.onSurface
            )

            Spacer(modifier = Modifier.height(8.dp))

            Row(
              modifier = Modifier.fillMaxWidth(),
              horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
              val options = listOf(
                Pair(-1, "Suave"),
                Pair(0, "Equilibrado"),
                Pair(1, "Desafío")
              )
              options.forEach { (offset, label) ->
                val isSelected = levelOffset == offset
                Surface(
                  modifier = Modifier
                    .weight(1f)
                    .clip(RoundedCornerShape(12.dp))
                    .clickable { levelOffset = offset },
                  shape = RoundedCornerShape(12.dp),
                  color = if (isSelected) game.domain.color else MaterialTheme.colorScheme.surfaceVariant
                ) {
                  Text(
                    text = label,
                    modifier = Modifier.padding(vertical = 10.dp),
                    style = MaterialTheme.typography.labelSmall,
                    fontWeight = FontWeight.Bold,
                    color = if (isSelected) Color.White else MaterialTheme.colorScheme.onSurface,
                    textAlign = androidx.compose.ui.text.style.TextAlign.Center
                  )
                }
              }
            }

            Spacer(modifier = Modifier.height(16.dp))

            // Mode Selector: Precisión vs Reto
            Row(
              modifier = Modifier.fillMaxWidth(),
              horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
              Surface(
                modifier = Modifier
                  .weight(1f)
                  .clip(RoundedCornerShape(12.dp))
                  .clickable { isTimedMode = false },
                shape = RoundedCornerShape(12.dp),
                color = if (!isTimedMode) TealPrimary else MaterialTheme.colorScheme.surfaceVariant
              ) {
                Text(
                  text = "Precisión (Sin reloj)",
                  modifier = Modifier.padding(vertical = 10.dp),
                  style = MaterialTheme.typography.labelSmall,
                  fontWeight = FontWeight.Bold,
                  color = if (!isTimedMode) Color.White else MaterialTheme.colorScheme.onSurface,
                  textAlign = androidx.compose.ui.text.style.TextAlign.Center
                )
              }

              Surface(
                modifier = Modifier
                  .weight(1f)
                  .clip(RoundedCornerShape(12.dp))
                  .clickable { isTimedMode = true },
                shape = RoundedCornerShape(12.dp),
                color = if (isTimedMode) DomainVelocidad else MaterialTheme.colorScheme.surfaceVariant
              ) {
                Text(
                  text = "Contra el reloj",
                  modifier = Modifier.padding(vertical = 10.dp),
                  style = MaterialTheme.typography.labelSmall,
                  fontWeight = FontWeight.Bold,
                  color = if (isTimedMode) Color.White else MaterialTheme.colorScheme.onSurface,
                  textAlign = androidx.compose.ui.text.style.TextAlign.Center
                )
              }
            }

            Spacer(modifier = Modifier.height(24.dp))

            Button(
              onClick = {
                val g = game
                gameToIntro = null
                viewModel.launchGame(g.id, customLevel = effectiveLevel, customTimed = isTimedMode)
              },
              modifier = Modifier
                .fillMaxWidth()
                .height(50.dp)
                .testTag("btn_intro_start"),
              shape = RoundedCornerShape(14.dp),
              colors = ButtonDefaults.buttonColors(containerColor = game.domain.color)
            ) {
              Text("Empezar", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
              Spacer(modifier = Modifier.width(8.dp))
              Icon(Icons.Default.PlayArrow, contentDescription = null)
            }
          }
        }
      }
    }
  }
}

private val LibraryBookends = setOf("bitacora")

/** "PARA TI HOY": un juego grande con su razón, "Jugar" (arcilla sol) y "otro" (la siguiente sugerencia). */
@Composable
internal fun ForYou(
  game: GameDefinition,
  title: String,
  reason: String,
  hasMore: Boolean,
  onPlay: () -> Unit,
  onOther: () -> Unit,
  onOpen: () -> Unit
) {
  Column(modifier = Modifier.fillMaxWidth().padding(start = 20.dp, end = 20.dp, top = 10.dp, bottom = 6.dp).testTag("for_you")) {
    Text("PARA TI HOY", color = Clay.Sun, fontSize = 11.sp, fontWeight = FontWeight.Bold, letterSpacing = 1.sp)
    Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = 8.dp)) {
      Box(
        modifier = Modifier
          .size(116.dp)
          .clip(CircleShape)
          .clickable(onClick = onOpen)
          .drawBehind {
            drawCircle(
              Brush.radialGradient(listOf(game.domain.color.copy(alpha = 0.45f), Color.Transparent), center, size.minDimension / 2f),
              radius = size.minDimension / 2f
            )
          },
        contentAlignment = Alignment.Center
      ) {
        com.example.ui.components.MiniPlanet(game.id, 92.dp)
      }
      Spacer(Modifier.width(14.dp))
      Column(Modifier.weight(1f)) {
        Text(title, color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold, lineHeight = 26.sp)
        Text(reason, color = OnNightDim, fontSize = 14.sp, lineHeight = 18.sp, modifier = Modifier.padding(top = 4.dp))
        Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = 10.dp)) {
          Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = Modifier
              .drawBehind {
                drawRoundRect(
                  Clay.Ink, topLeft = Offset(0f, 4.dp.toPx()), size = size,
                  cornerRadius = androidx.compose.ui.geometry.CornerRadius(16.dp.toPx())
                )
              }
              .clip(RoundedCornerShape(16.dp))
              .background(Clay.Sun)
              .border(3.dp, Clay.Ink, RoundedCornerShape(16.dp))
              .clickable(onClick = onPlay)
              .padding(horizontal = 18.dp, vertical = 9.dp)
              .testTag("for_you_play")
          ) {
            Icon(Icons.Default.PlayArrow, contentDescription = null, tint = Clay.Ink, modifier = Modifier.size(22.dp))
            Spacer(Modifier.width(6.dp))
            Text("Jugar", color = Clay.Ink, fontSize = 17.sp, fontWeight = FontWeight.Bold)
          }
          if (hasMore) {
            Text(
              "otro ›",
              color = Clay.Sun,
              fontSize = 14.sp,
              fontWeight = FontWeight.Bold,
              modifier = Modifier
                .padding(start = 8.dp)
                .clip(RoundedCornerShape(10.dp))
                .clickable(onClick = onOther)
                .padding(horizontal = 8.dp, vertical = 8.dp)
                .testTag("for_you_other")
            )
          }
        }
      }
    }
  }
}

/** Una fila por área: el nombre con su punto de color y cuántos juegos tiene, y los planetas que se deslizan de lado. */
@Composable
internal fun AreaRow(
  domain: DomainType,
  title: String,
  count: Int,
  content: androidx.compose.foundation.lazy.LazyListScope.() -> Unit
) {
  Column(modifier = Modifier.fillMaxWidth().padding(top = 14.dp).testTag("area_${domain.name.lowercase()}")) {
    Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(horizontal = 20.dp)) {
      Box(Modifier.size(12.dp).clip(CircleShape).background(domain.color).border(2.dp, Clay.Ink, CircleShape))
      Spacer(Modifier.width(8.dp))
      Text(title, color = com.example.ui.components.domainTextColor(domain.name), fontSize = 19.sp, fontWeight = FontWeight.Bold)
      Spacer(Modifier.width(10.dp))
      Text(if (count == 1) "1 juego" else "$count juegos", color = OnNightSoft, fontSize = 13.sp)
    }
    LazyRow(
      contentPadding = PaddingValues(horizontal = 12.dp),
      horizontalArrangement = Arrangement.spacedBy(4.dp),
      modifier = Modifier.fillMaxWidth().padding(top = 8.dp),
      content = content
    )
  }
}

/**
 * Un juego en su fila: planetita con un anillo de su nivel (0..1, color del área), estrella sol si es juego estrella,
 * ✓ si se jugó hoy; los sin probar, con aro sol. Debajo el nombre (2 líneas) y tu marca, tu nivel o "Nuevo".
 */
@Composable
internal fun LibraryPlanet(
  game: GameDefinition,
  title: String,
  level: Float?,
  star: Boolean,
  playedToday: Boolean,
  isNew: Boolean,
  sub: String,
  subColor: Color,
  onClick: () -> Unit
) {
  val ringColor = com.example.ui.components.domainTextColor(game.domain.name)
  Column(
    horizontalAlignment = Alignment.CenterHorizontally,
    modifier = Modifier
      .width(88.dp)
      .clip(RoundedCornerShape(18.dp))
      .clickable(onClick = onClick)
      .padding(vertical = 4.dp)
      .testTag("game_card_${game.id}")
  ) {
    Box(
      modifier = Modifier
        .size(74.dp)
        .drawBehind {
          val stroke = 3.dp.toPx()
          val inset = stroke / 2f
          val arcSize = androidx.compose.ui.geometry.Size(size.width - stroke, size.height - stroke)
          if (isNew) {
            drawCircle(Clay.Sun, radius = size.minDimension / 2f - inset, style = androidx.compose.ui.graphics.drawscope.Stroke(stroke))
          } else {
            drawCircle(Color.White.copy(alpha = 0.16f), radius = size.minDimension / 2f - inset, style = androidx.compose.ui.graphics.drawscope.Stroke(stroke))
            if (level != null && level > 0f) {
              drawArc(
                ringColor, -90f, 360f * level.coerceIn(0.03f, 1f), false, Offset(inset, inset), arcSize,
                style = androidx.compose.ui.graphics.drawscope.Stroke(stroke, cap = androidx.compose.ui.graphics.StrokeCap.Round)
              )
            }
          }
        },
      contentAlignment = Alignment.Center
    ) {
      com.example.ui.components.MiniPlanet(game.id, 58.dp, done = playedToday)
      if (star) {
        StarBadge(Modifier.align(Alignment.TopStart).padding(top = 2.dp, start = 2.dp).size(18.dp))
      }
    }
    Text(
      text = title,
      color = Color.White,
      fontSize = 12.sp,
      fontWeight = FontWeight.Bold,
      textAlign = androidx.compose.ui.text.style.TextAlign.Center,
      maxLines = 2,
      overflow = TextOverflow.Ellipsis,
      lineHeight = 14.sp,
      modifier = Modifier.padding(top = 4.dp).heightIn(min = 28.dp)
    )
    Text(sub, color = subColor, fontSize = 11.sp, fontWeight = FontWeight.Bold, maxLines = 1, overflow = TextOverflow.Ellipsis)
  }
}

/** Estrella sol de arcilla: marca los juegos estrella (los que dan una medida tuya al final). */
@Composable
private fun StarBadge(modifier: Modifier) {
  androidx.compose.foundation.Canvas(modifier) {
    val c = center
    val outer = size.minDimension / 2f
    val path = androidx.compose.ui.graphics.Path().apply {
      for (i in 0 until 10) {
        val a = -Math.PI / 2 + i * Math.PI / 5
        val r = if (i % 2 == 0) outer else outer * 0.45f
        val x = c.x + (Math.cos(a) * r).toFloat()
        val y = c.y + (Math.sin(a) * r).toFloat()
        if (i == 0) moveTo(x, y) else lineTo(x, y)
      }
      close()
    }
    drawPath(path, Clay.Sun)
    drawPath(path, Clay.Ink, style = androidx.compose.ui.graphics.drawscope.Stroke(1.5.dp.toPx()))
  }
}
