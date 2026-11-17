# Decisiones de diseño (ADR resumidos)

| # | Decisión | Motivo |
|---|---|---|
| 1 | **Monolito ASP.NET Core MVC** en un solo proyecto | El dominio es pequeño; separar en proyectos o servicios añade ceremonia. Las carpetas mantienen fronteras claras y el dominio no depende de infraestructura. |
| 2 | **Partidas individuales y concurrentes** | Las reglas de retiro y eliminación son de juego individual. Varias personas juegan a la vez, cada una con su partida. No se cuenta con una sala, sino que cada persona juega por aparte. |
| 3 | **Sin ranking** | No se implementó al no ser requisito. |
| 4 | **Jugador identificado por cédula, con cuenta propia** | Dos personas pueden llamarse igual; la cédula identifica a quién se le paga. Cada persona se registra sola (cédula, nombre, contraseña). |
| 5 | **ASP.NET Core Identity para jugadores y administrador** | Contraseñas con hash, bloqueo por intentos fallidos y roles sin código propio de seguridad. |
| 6 | **Una sola participación por persona** | Regla de negocio en `Player.EnsureCanStartGame` más el índice único `Game(PlayerId)`, que también cubre dos clics simultáneos. Aplica sin importar cómo terminó la partida. |
| 7 | **Premios a pagar: solo consulta** | El administrador ve quién ganó, con cédula completa y total. |
| 8 | **Cédula enmascarada fuera de "Premios a pagar"** | Minimiza la exposición de datos personales: solo la pantalla de pagos necesita el número completo. |
| 9 | **Categoría = nivel de complejidad (1–5), nivel N → ronda N** | El enunciado agrupa las preguntas por complejidad y pide 5 rondas. Al ser cada ronda de un nivel distinto, las preguntas nunca se repiten dentro de una partida. |
| 10 | **Fin forzado = tiempo agotado, evaluado en cada acción** | Sin trabajos en segundo plano. El servidor es la fuente de verdad del tiempo; 2 s de gracia por latencia. |
| 11 | **Premio y tiempo se copian en `GameRound`** | Cambiar la configuración no altera partidas en curso ni el historial. |
| 12 | **Configuración de rondas validada como un todo (`GameConfiguration`)** | "Cada ronda paga más que la anterior" involucra varias filas.  |
| 13 | **Preguntas jugadas son inmutables** | El historial debe mostrar exactamente lo que vio el jugador. Para corregirlas se desactivan y se crea otra. |
| 14 | **Scripts SQL como fuente del esquema (sin migraciones)** | El script es un entregable explícito.  |
| 15 | **Premios en pesos colombianos enteros** | `DECIMAL(18,0)`, formato `es-CO` centralizado en `DisplayFormat`. |
