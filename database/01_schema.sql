SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'QuizCiel') IS NULL
    CREATE DATABASE QuizCiel;
GO

USE QuizCiel;
GO

/* ---------- categories, questions and options ---------- */

IF OBJECT_ID(N'dbo.Category', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Category
    (
        Id          INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Category PRIMARY KEY,
        Name        NVARCHAR(80)       NOT NULL,
        Level       INT                NOT NULL,
        Description NVARCHAR(250)      NULL,
        CONSTRAINT CK_Category_Level CHECK (Level BETWEEN 1 AND 5)
    );
    -- Business rule: one category per complexity level (level N feeds round N).
    CREATE UNIQUE INDEX UX_Category_Level ON dbo.Category (Level);
END;
GO

IF OBJECT_ID(N'dbo.Question', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Question
    (
        Id         INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Question PRIMARY KEY,
        CategoryId INT                NOT NULL CONSTRAINT FK_Question_Category REFERENCES dbo.Category (Id),
        Text       NVARCHAR(300)      NOT NULL,
        IsActive   BIT                NOT NULL CONSTRAINT DF_Question_IsActive DEFAULT (1)
    );
    CREATE INDEX IX_Question_CategoryId_IsActive ON dbo.Question (CategoryId, IsActive);
END;
GO

IF OBJECT_ID(N'dbo.AnswerOption', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AnswerOption
    (
        Id         INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AnswerOption PRIMARY KEY,
        QuestionId INT                NOT NULL CONSTRAINT FK_AnswerOption_Question REFERENCES dbo.Question (Id) ON DELETE CASCADE,
        Position   INT                NOT NULL,
        Text       NVARCHAR(150)      NOT NULL,
        IsCorrect  BIT                NOT NULL,
        CONSTRAINT CK_AnswerOption_Position CHECK (Position BETWEEN 1 AND 4)
    );
    -- Business rule: four options per question, one per position.
    CREATE UNIQUE INDEX UX_AnswerOption_QuestionId_Position ON dbo.AnswerOption (QuestionId, Position);
    -- Business rule: a single valid answer per question.
    CREATE UNIQUE INDEX UX_AnswerOption_OneCorrectPerQuestion ON dbo.AnswerOption (QuestionId) WHERE IsCorrect = 1;
END;
GO

IF OBJECT_ID(N'dbo.RoundConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoundConfig
    (
        RoundNumber      INT            NOT NULL CONSTRAINT PK_RoundConfig PRIMARY KEY,
        Prize            DECIMAL(18, 0) NOT NULL,
        TimeLimitSeconds INT            NOT NULL,
        CONSTRAINT CK_RoundConfig_RoundNumber CHECK (RoundNumber BETWEEN 1 AND 5),
        CONSTRAINT CK_RoundConfig_Prize CHECK (Prize > 0),
        CONSTRAINT CK_RoundConfig_TimeLimit CHECK (TimeLimitSeconds BETWEEN 10 AND 120)
    );
END;
GO

IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles
    (
        Id               NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
        Name             NVARCHAR(256) NULL,
        NormalizedName   NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL
    );
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers
    (
        Id                   NVARCHAR(450)  NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
        UserName             NVARCHAR(256)  NULL,
        NormalizedUserName   NVARCHAR(256)  NULL,
        Email                NVARCHAR(256)  NULL,
        NormalizedEmail      NVARCHAR(256)  NULL,
        EmailConfirmed       BIT            NOT NULL,
        PasswordHash         NVARCHAR(MAX)  NULL,
        SecurityStamp        NVARCHAR(MAX)  NULL,
        ConcurrencyStamp     NVARCHAR(MAX)  NULL,
        PhoneNumber          NVARCHAR(MAX)  NULL,
        PhoneNumberConfirmed BIT            NOT NULL,
        TwoFactorEnabled     BIT            NOT NULL,
        LockoutEnd           DATETIMEOFFSET NULL,
        LockoutEnabled       BIT            NOT NULL,
        AccessFailedCount    INT            NOT NULL
    );
    CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
    -- Players use their cédula as username, so this index also guarantees one account per cédula.
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims
    (
        Id         INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
        RoleId     NVARCHAR(450)      NOT NULL CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE,
        ClaimType  NVARCHAR(MAX)      NULL,
        ClaimValue NVARCHAR(MAX)      NULL
    );
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims (RoleId);
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims
    (
        Id         INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
        UserId     NVARCHAR(450)      NOT NULL CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
        ClaimType  NVARCHAR(MAX)      NULL,
        ClaimValue NVARCHAR(MAX)      NULL
    );
    CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims (UserId);
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins
    (
        LoginProvider       NVARCHAR(450) NOT NULL,
        ProviderKey         NVARCHAR(450) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId              NVARCHAR(450) NOT NULL CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey)
    );
    CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins (UserId);
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles
    (
        UserId NVARCHAR(450) NOT NULL CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
        RoleId NVARCHAR(450) NOT NULL CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId)
    );
    CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles (RoleId);
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens
    (
        UserId        NVARCHAR(450) NOT NULL CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
        LoginProvider NVARCHAR(450) NOT NULL,
        Name          NVARCHAR(450) NOT NULL,
        Value         NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name)
    );
END;
GO

/* ---------- Gameplay: players, games and rounds ---------- */

IF OBJECT_ID(N'dbo.Player', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Player
    (
        Id             INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Player PRIMARY KEY,
        UserId         NVARCHAR(450)      NOT NULL CONSTRAINT FK_Player_AspNetUsers_UserId REFERENCES dbo.AspNetUsers (Id),
        DocumentNumber VARCHAR(10)        NOT NULL,
        FullName       NVARCHAR(80)       NOT NULL,
        CreatedAt      DATETIME2          NOT NULL,
        CONSTRAINT CK_Player_DocumentNumber CHECK (LEN(DocumentNumber) BETWEEN 6 AND 10 AND DocumentNumber NOT LIKE '%[^0-9]%')
    );
    -- Business rule: one player per cédula (who gets paid) and one player per login account.
    CREATE UNIQUE INDEX UX_Player_DocumentNumber ON dbo.Player (DocumentNumber);
    CREATE UNIQUE INDEX UX_Player_UserId ON dbo.Player (UserId);
END;
GO

IF OBJECT_ID(N'dbo.Game', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Game
    (
        Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Game PRIMARY KEY,
        PlayerId           INT              NOT NULL CONSTRAINT FK_Game_Player REFERENCES dbo.Player (Id),
        Status             INT              NOT NULL,
        CurrentRoundNumber INT              NOT NULL,
        AccumulatedPrize   DECIMAL(18, 0)   NOT NULL,
        FinalPrize         DECIMAL(18, 0)   NULL,
        StartedAt          DATETIME2        NOT NULL,
        EndedAt            DATETIME2        NULL,
        ConcurrencyStamp   UNIQUEIDENTIFIER NOT NULL,
        -- 0 InProgress, 1 Won, 2 Withdrawn, 3 Lost, 4 ForcedEnd
        CONSTRAINT CK_Game_Status CHECK (Status BETWEEN 0 AND 4),
        CONSTRAINT CK_Game_CurrentRound CHECK (CurrentRoundNumber BETWEEN 1 AND 5),
        CONSTRAINT CK_Game_AccumulatedPrize CHECK (AccumulatedPrize >= 0),
        CONSTRAINT CK_Game_FinishedHasPrizeAndEnd CHECK (
            (Status = 0 AND FinalPrize IS NULL AND EndedAt IS NULL)
            OR (Status <> 0 AND FinalPrize IS NOT NULL AND EndedAt IS NOT NULL))
    );
    -- Business rule: each player takes part only once (also protects against double "start" clicks).
    CREATE UNIQUE INDEX UX_Game_PlayerId ON dbo.Game (PlayerId);
    CREATE INDEX IX_Game_StartedAt ON dbo.Game (StartedAt);
END;
GO

IF OBJECT_ID(N'dbo.GameRound', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GameRound
    (
        Id               INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_GameRound PRIMARY KEY,
        GameId           UNIQUEIDENTIFIER   NOT NULL CONSTRAINT FK_GameRound_Game REFERENCES dbo.Game (Id) ON DELETE CASCADE,
        RoundNumber      INT                NOT NULL,
        QuestionId       INT                NOT NULL CONSTRAINT FK_GameRound_Question REFERENCES dbo.Question (Id),
        SelectedOptionId INT                NULL CONSTRAINT FK_GameRound_AnswerOption REFERENCES dbo.AnswerOption (Id),
        IsCorrect        BIT                NULL,
        Prize            DECIMAL(18, 0)     NOT NULL,
        TimeLimitSeconds INT                NOT NULL,
        AskedAt          DATETIME2          NOT NULL,
        AnsweredAt       DATETIME2          NULL,
        CONSTRAINT CK_GameRound_RoundNumber CHECK (RoundNumber BETWEEN 1 AND 5)
    );
    -- Business rule: one question per round of a game.
    CREATE UNIQUE INDEX UX_GameRound_GameId_RoundNumber ON dbo.GameRound (GameId, RoundNumber);
    CREATE INDEX IX_GameRound_QuestionId ON dbo.GameRound (QuestionId);
    CREATE INDEX IX_GameRound_SelectedOptionId ON dbo.GameRound (SelectedOptionId);
END;
GO
