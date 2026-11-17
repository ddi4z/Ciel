# QuizCiel


## Documentación

| Entregable | Archivo |
|---|---|
| Casos de uso | [docs/casos-de-uso.md](docs/casos-de-uso.md) |
| Modelo entidad-relación y diccionario de datos | [docs/modelo-er.md](docs/modelo-er.md) |
| Arquitectura y patrones | [docs/arquitectura.md](docs/arquitectura.md) |
| Decisiones de diseño | [docs/decisiones.md](docs/decisiones.md) |
| Script de base de datos | [database/01_schema.sql](database/01_schema.sql), [database/02_seed.sql](database/02_seed.sql) |
| Código fuente | [src/QuizCiel.Web](src/QuizCiel.Web), [tests/QuizCiel.Tests](tests/QuizCiel.Tests) |

## Instalación

### Docker 

Requisitos: Docker Desktop (Compose v2) y los puertos 8080 y 1433 libres. La imagen de SQL Server es amd64; en Mac con Apple Silicon se ejecuta con emulación.

1. Crea el archivo de variables a partir del ejemplo:
   ```bash
   cp .env.example .env
   ```
2. Levanta todo:
   ```bash
   docker compose up --build -d
   ```
   Esto inicia SQL Server, ejecuta `database/01_schema.sql` y `database/02_seed.sql` y arranca la web.
3. Abre http://localhost:8080.

Para detener todo: `docker compose down`. Para borrar también la base de datos : `docker compose down -v`.


### Cómo usarlo

- **Administrador**: entra a http://localhost:8080/account/login con `ADMIN_USERNAME` y `ADMIN_PASSWORD` del archivo `.env`. Llegas al panel `/admin`, donde ves el estado del juego, las categorías y preguntas, los premios y tiempos de cada ronda, las partidas y los premios a pagar.
- **Jugador**: en la página de inicio pulsa "Registrarme", escribe una cédula, tu nombre y una contraseña, y luego "Empezar mi partida". Cada cédula juega una sola vez; para volver a jugar, registra otra.

El seed deja el juego listo: 5 categorías de programación con 5 preguntas cada una y premios de $100.000 a $50.000.000.

### Sin Docker

Requisitos: SDK de .NET 10 y un SQL Server accesible (puede ser el servicio `db` del compose, que publica el puerto 1433).

1. Crea la base ejecutando `database/01_schema.sql` y luego `database/02_seed.sql` (por ejemplo con `sqlcmd` o SQL Server Management Studio). La aplicación no crea el esquema por su cuenta.
2. Define la cadena de conexión y el administrador:
   ```bash
   dotnet user-secrets --project src/QuizCiel.Web set "ConnectionStrings:QuizDb" "Server=localhost,1433;Database=QuizCiel;User Id=sa;Password=<tu-clave>;TrustServerCertificate=True"
   ```
   ```bash
   dotnet user-secrets --project src/QuizCiel.Web set "AdminCredentials:Username" "admin"
   ```
   ```bash
   dotnet user-secrets --project src/QuizCiel.Web set "AdminCredentials:Password" "<clave-del-admin>"
   ```
3. Ejecuta la aplicación:
   ```bash
   dotnet run --project src/QuizCiel.Web
   ```

### Pruebas

Requisitos: SDK de .NET 10. Desde la raíz del repositorio:

```bash
dotnet test
```

Las pruebas cubren las reglas del dominio (rondas, premios, retiro, tiempo, opciones) y los servicios sobre SQLite en memoria (inicio de partida, dueño de la partida, concurrencia, registro). No necesitan SQL Server ni Docker.



## Configuración

| Clave | Variable de entorno | Descripción |
|---|---|---|
| `ConnectionStrings:QuizDb` | `ConnectionStrings__QuizDb` | Cadena de conexión a SQL Server |
| `AdminCredentials:Username` | `AdminCredentials__Username` | Usuario de la cuenta de administrador que se crea al primer arranque |
| `AdminCredentials:Password` | `AdminCredentials__Password` | Contraseña inicial de esa cuenta (mínimo 8 caracteres, con mayúscula, minúscula y número). Si falta, no se crea administrador |
| `Serilog:*` | `Serilog__...` | Niveles y destinos de log (consola y archivo diario en `logs/`) |

El repositorio no contiene credenciales. Con Docker se toman de `.env`; con `dotnet run`, defínelas con `dotnet user-secrets --project src/QuizCiel.Web set "<clave>" "<valor>"`.

Los premios, los tiempos y el contenido se configuran desde el panel `/admin`.

## Reglas de negocio principales

- Cada jugador se identifica con una cédula única (6 a 10 dígitos) y participa una sola vez, sin importar el resultado.
- Un jugador solo puede ver y jugar su propia partida.
- Tras 5 intentos fallidos de ingreso, la cuenta se bloquea 5 minutos.
- 5 rondas; la ronda N usa preguntas de la categoría de nivel N.
- Cada pregunta tiene 4 opciones distintas y exactamente 1 correcta.
- Cada nivel necesita al menos 5 preguntas activas para poder jugar.
- Los premios son enteros y estrictamente crecientes por ronda.
- El tiempo por ronda está entre 10 y 120 s, con 2 s de gracia por latencia.
- Solo se puede retirar antes de responder la pregunta actual.
- Una pregunta ya jugada no se puede editar, y una categoría con preguntas no se puede eliminar.
- Una partida terminada no acepta más acciones.
