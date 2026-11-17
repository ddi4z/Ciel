SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE QuizCiel;
GO

IF EXISTS (SELECT 1 FROM dbo.Category)
BEGIN
    PRINT N'Seed omitido.';
    SET NOEXEC ON;
END;
GO


CREATE PROCEDURE #AddQuestion
    @Level           INT,
    @Text            NVARCHAR(300),
    @Option1         NVARCHAR(150),
    @Option2         NVARCHAR(150),
    @Option3         NVARCHAR(150),
    @Option4         NVARCHAR(150),
    @CorrectPosition INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CategoryId INT = (SELECT Id FROM dbo.Category WHERE Level = @Level);

    INSERT INTO dbo.Question (CategoryId, Text, IsActive) VALUES (@CategoryId, @Text, 1);
    DECLARE @QuestionId INT = CAST(SCOPE_IDENTITY() AS INT);

    INSERT INTO dbo.AnswerOption (QuestionId, Position, Text, IsCorrect)
    VALUES (@QuestionId, 1, @Option1, IIF(@CorrectPosition = 1, 1, 0)),
           (@QuestionId, 2, @Option2, IIF(@CorrectPosition = 2, 1, 0)),
           (@QuestionId, 3, @Option3, IIF(@CorrectPosition = 3, 1, 0)),
           (@QuestionId, 4, @Option4, IIF(@CorrectPosition = 4, 1, 0));
END;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT INTO dbo.Category (Name, Level, Description)
VALUES (N'Conceptos básicos',       1, N'Ideas generales de programación.'),
       (N'Variables y tipos',       2, N'Tipos de datos y asignación de valores.'),
       (N'Estructuras de control',  3, N'Condicionales, ciclos.'),
       (N'Bases de datos',          4, N'Consultas y conceptos básicos.'),
       (N'C# y objetos',            5, N'Clases, objetos y sintaxis de C#.');

-- Level 1: Basic concepts
EXEC #AddQuestion 1, N'¿Qué entiendes por algoritmo?', N'Una serie de pasos para resolver un problema', N'Un tipo de computador', N'Un virus informático', N'Un cable de red', 1;
EXEC #AddQuestion 1, N'En programación, ¿qué es un "bug"?', N'Un comentario dentro del código', N'Un error en el programa', N'Un tipo de variable', N'Un lenguaje de programación', 2;
EXEC #AddQuestion 1, N'¿Qué extensión suelen tener los archivos de código C#?', N'.java', N'.py', N'.cs', N'.html', 3;
EXEC #AddQuestion 1, N'¿Para qué se utiliza Git?', N'Para diseñar imágenes', N'Para enviar correos', N'Para crear bases de datos', N'Para llevar el control de versiones del código', 4;
EXEC #AddQuestion 1, N'¿Qué lenguaje se utiliza para definir la estructura de una página web?', N'HTML', N'SQL', N'C#', N'Excel', 1;

-- Level 2: Variables and types
EXEC #AddQuestion 2, N'¿Qué tipo de dato usarías para guardar el texto "Hola"?', N'int', N'string', N'bool', N'double', 2;
EXEC #AddQuestion 2, N'¿Qué valores puede tomar una variable de tipo bool?', N'Cualquier número', N'Cualquier texto', N'true o false', N'Solo el valor 0', 3;
EXEC #AddQuestion 2, N'¿Qué tipo de dato usarías para guardar la edad de una persona?', N'int', N'string', N'bool', N'char', 1;
EXEC #AddQuestion 2, N'Si int x = 5 y después haces x = x + 3, ¿cuánto vale x?', N'5', N'3', N'53', N'8', 4;
EXEC #AddQuestion 2, N'¿Qué es una variable?', N'Un error del compilador', N'Un espacio con nombre que guarda un valor', N'Un tipo de ciclo', N'Un archivo del proyecto', 2;

-- Level 3: Control structures
EXEC #AddQuestion 3, N'¿Qué instrucción permite tomar una decisión a partir de una condición?', N'for', N'while', N'if', N'return', 3;
EXEC #AddQuestion 3, N'¿Cuántas veces se ejecuta este ciclo: for (int i = 0; i < 3; i++)?', N'3', N'2', N'4', N'Infinitas veces', 1;
EXEC #AddQuestion 3, N'¿Qué operador se usa en C# para comprobar si dos valores son iguales?', N'=', N'!=', N'=>', N'==', 4;
EXEC #AddQuestion 3, N'¿Cuál es el resultado de calcular 10 % 3?', N'3', N'1', N'0', N'30', 2;
EXEC #AddQuestion 3, N'¿Qué operador lógico representa "Y" en C#?', N'||', N'!', N'&&', N'??', 3;

-- Level 4: Databases
EXEC #AddQuestion 4, N'¿Qué comando SQL se usa para consultar los datos de una tabla?', N'SELECT', N'INSERT', N'DELETE', N'UPDATE', 1;
EXEC #AddQuestion 4, N'¿Qué comando SQL se usa para agregar un registro?', N'SELECT', N'INSERT', N'DROP', N'ALTER', 2;
EXEC #AddQuestion 4, N'¿Qué cláusula SQL sirve para filtrar los registros de una consulta?', N'ORDER BY', N'FROM', N'WHERE', N'GROUP BY', 3;
EXEC #AddQuestion 4, N'¿Qué es una llave primaria (PK)?', N'La contraseña de la base de datos', N'Una tabla temporal', N'Un tipo de índice para textos', N'Un campo que identifica de manera única cada registro', 4;
EXEC #AddQuestion 4, N'¿Qué cláusula SQL se usa para ordenar los resultados?', N'ORDER BY', N'WHERE', N'SORT', N'HAVING', 1;

-- Level 5: C# and objects
EXEC #AddQuestion 5, N'¿Qué palabra clave se usa para crear un objeto en C#?', N'create', N'new', N'object', N'make', 2;
EXEC #AddQuestion 5, N'¿Qué es una clase?', N'Un error de compilación', N'Un tipo de ciclo', N'Una plantilla para crear objetos', N'Una tabla de la base de datos', 3;
EXEC #AddQuestion 5, N'¿Qué modificador hace que un miembro solo pueda usarse dentro de su propia clase?', N'public', N'internal', N'protected', N'private', 4;
EXEC #AddQuestion 5, N'¿Qué significan las siglas POO?', N'Programación Orientada a Objetos', N'Proceso de Operaciones Ordenadas', N'Programa de Objetos Opcionales', N'Protocolo de Orden Operativo', 1;
EXEC #AddQuestion 5, N'¿Cómo se llama el método donde comienza la ejecución de un programa de consola en C#?', N'Start', N'Main', N'Run', N'Init', 2;

-- Prizes in Colombian pesos, increasing with each round.
INSERT INTO dbo.RoundConfig (RoundNumber, Prize, TimeLimitSeconds)
VALUES (1,    100000, 30),
       (2,    500000, 30),
       (3,   2000000, 40),
       (4,  10000000, 40),
       (5,  50000000, 45);

COMMIT TRANSACTION;
PRINT N'Seed completo.';
GO

SET NOEXEC OFF;
GO
