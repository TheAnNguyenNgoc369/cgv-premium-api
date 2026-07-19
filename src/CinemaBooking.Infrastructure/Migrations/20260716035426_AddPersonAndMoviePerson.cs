using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonAndMoviePerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ----------------------------------------------------------------
            // 1) Create Person table (idempotent).
            // ----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables
                               WHERE name = 'Person'
                                 AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    CREATE TABLE [Person] (
                        [PersonId]  INT           IDENTITY(1,1) NOT NULL,
                        [Name]      NVARCHAR(200) NOT NULL,
                        [CreatedAt] DATETIME2     NOT NULL CONSTRAINT [DF_Person_CreatedAt] DEFAULT (GETDATE()),
                        [UpdatedAt] DATETIME2     NOT NULL CONSTRAINT [DF_Person_UpdatedAt] DEFAULT (GETDATE()),
                        CONSTRAINT [PK_Person] PRIMARY KEY CLUSTERED ([PersonId] ASC)
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'UQ_Person_Name'
                                 AND object_id = OBJECT_ID('dbo.Person'))
                BEGIN
                    CREATE UNIQUE INDEX [UQ_Person_Name] ON [Person] ([Name]);
                END
            ");

            // ----------------------------------------------------------------
            // 2) Create MoviePerson table (idempotent).
            //    Composite PK (MovieId, PersonId, Role); Role check constraint.
            // ----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables
                               WHERE name = 'MoviePerson'
                                 AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    CREATE TABLE [MoviePerson] (
                        [MovieId]      INT          NOT NULL,
                        [PersonId]     INT          NOT NULL,
                        [Role]         NVARCHAR(50) NOT NULL,
                        [DisplayOrder] INT          NOT NULL CONSTRAINT [DF_MoviePerson_DisplayOrder] DEFAULT (0),
                        CONSTRAINT [PK_MoviePerson] PRIMARY KEY CLUSTERED ([MovieId] ASC, [PersonId] ASC, [Role] ASC),
                        CONSTRAINT [FK_MoviePerson_Movie]
                            FOREIGN KEY ([MovieId])  REFERENCES [Movie]([MovieID]),
                        CONSTRAINT [FK_MoviePerson_Person]
                            FOREIGN KEY ([PersonId]) REFERENCES [Person]([PersonId]),
                        CONSTRAINT [CK_MoviePerson_Role]
                            CHECK ([Role] IN ('Director', 'Actor'))
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_MoviePerson_PersonId'
                                 AND object_id = OBJECT_ID('dbo.MoviePerson'))
                BEGIN
                    CREATE INDEX [IX_MoviePerson_PersonId]
                        ON [MoviePerson] ([PersonId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_MoviePerson_MovieId_Role_DisplayOrder'
                                 AND object_id = OBJECT_ID('dbo.MoviePerson'))
                BEGIN
                    CREATE INDEX [IX_MoviePerson_MovieId_Role_DisplayOrder]
                        ON [MoviePerson] ([MovieId], [Role], [DisplayOrder]);
                END
            ");

            // ----------------------------------------------------------------
            // 3) Migrate data from Movie.Director / Movie.Cast into
            //    Person + MoviePerson. Only runs if legacy columns still exist,
            //    so re-running after data migration is a no-op.
            // ----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE name = 'Director'
                             AND object_id = OBJECT_ID('dbo.Movie'))
                   OR EXISTS (SELECT 1 FROM sys.columns
                              WHERE name = 'Cast'
                                AND object_id = OBJECT_ID('dbo.Movie'))
                BEGIN
                    -- Build a working set of (MovieId, Role, DisplayOrder, Name)
                    -- from Director and comma-separated Cast, trimming whitespace,
                    -- dropping empty entries, and preserving order.
                    DECLARE @sql NVARCHAR(MAX);

                    SET @sql = N'
                    ;WITH DirectorSrc AS (
                        SELECT
                            m.MovieID  AS MovieId,
                            ''Director'' AS Role,
                            0           AS DisplayOrder,
                            LTRIM(RTRIM(m.Director)) AS Name
                        FROM dbo.Movie m
                        WHERE m.Director IS NOT NULL
                          AND LTRIM(RTRIM(m.Director)) <> ''''
                    ),
                    CastSplit AS (
                        SELECT
                            m.MovieID AS MovieId,
                            LTRIM(RTRIM(value)) AS Name,
                            -- ordinal is not exposed in older compat levels;
                            -- fall back to a deterministic row_number over the
                            -- position of each fragment in the original string.
                            ROW_NUMBER() OVER (
                                PARTITION BY m.MovieID
                                ORDER BY CHARINDEX(''|'' + LTRIM(RTRIM(value)) + ''|'',
                                                   ''|'' + REPLACE(m.Cast, '','', ''|'') + ''|'')
                            ) AS DisplayOrder
                        FROM dbo.Movie m
                        CROSS APPLY STRING_SPLIT(m.Cast, '','')
                        WHERE m.Cast IS NOT NULL
                          AND LTRIM(RTRIM(m.Cast)) <> ''''
                          -- Skip placeholder ""Đang cập nhật"" (case-insensitive)
                          -- entirely; malformed rows (e.g. MovieId=31) are fixed
                          -- manually in SQL, not by migration heuristics.
                          AND LOWER(LTRIM(RTRIM(m.Cast))) <> LOWER(N''Đang cập nhật'')
                    ),
                    ActorSrc AS (
                        SELECT
                            MovieId,
                            ''Actor'' AS Role,
                            DisplayOrder,
                            Name
                        FROM CastSplit
                        WHERE Name <> ''''
                    ),
                    AllSrc AS (
                        SELECT * FROM DirectorSrc
                        UNION ALL
                        SELECT * FROM ActorSrc
                    )
                    SELECT MovieId, Role, DisplayOrder, Name
                    INTO #Src
                    FROM AllSrc;

                    -- 3a) Insert any Person names not already present. Reuse
                    -- existing rows (case-insensitive) when the name already exists.
                    INSERT INTO dbo.Person (Name, CreatedAt, UpdatedAt)
                    SELECT DISTINCT s.Name, GETDATE(), GETDATE()
                    FROM #Src s
                    WHERE NOT EXISTS (
                        SELECT 1 FROM dbo.Person p WHERE p.Name = s.Name
                    );

                    -- 3b) Insert MoviePerson rows, mapping each name to its
                    -- Person row. Dedupe on (MovieId, PersonId, Role) to satisfy
                    -- the composite PK if the same actor appears twice in a cast
                    -- string; keep the earliest DisplayOrder.
                    ;WITH Mapped AS (
                        SELECT
                            s.MovieId,
                            p.PersonId,
                            s.Role,
                            MIN(s.DisplayOrder) AS DisplayOrder
                        FROM #Src s
                        INNER JOIN dbo.Person p ON p.Name = s.Name
                        GROUP BY s.MovieId, p.PersonId, s.Role
                    )
                    INSERT INTO dbo.MoviePerson (MovieId, PersonId, Role, DisplayOrder)
                    SELECT m.MovieId, m.PersonId, m.Role, m.DisplayOrder
                    FROM Mapped m
                    WHERE NOT EXISTS (
                        SELECT 1 FROM dbo.MoviePerson mp
                        WHERE mp.MovieId  = m.MovieId
                          AND mp.PersonId = m.PersonId
                          AND mp.Role     = m.Role
                    );

                    DROP TABLE #Src;
                    ';

                    EXEC sp_executesql @sql;
                END
            ");

            // ----------------------------------------------------------------
            // 4) Only AFTER the data migration succeeds, drop the legacy
            //    Movie.Director and Movie.Cast columns. Idempotent.
            // ----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE name = 'Director'
                             AND object_id = OBJECT_ID('dbo.Movie'))
                BEGIN
                    DECLARE @default_name NVARCHAR(200);
                    SELECT @default_name = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON dc.parent_column_id = c.column_id
                                            AND dc.parent_object_id = c.object_id
                    WHERE c.name = 'Director'
                      AND c.object_id = OBJECT_ID('dbo.Movie');
                    IF @default_name IS NOT NULL
                        EXEC('ALTER TABLE [Movie] DROP CONSTRAINT [' + @default_name + ']');

                    ALTER TABLE [Movie] DROP COLUMN [Director];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE name = 'Cast'
                             AND object_id = OBJECT_ID('dbo.Movie'))
                BEGIN
                    DECLARE @default_name NVARCHAR(200);
                    SELECT @default_name = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON dc.parent_column_id = c.column_id
                                            AND dc.parent_object_id = c.object_id
                    WHERE c.name = 'Cast'
                      AND c.object_id = OBJECT_ID('dbo.Movie');
                    IF @default_name IS NOT NULL
                        EXEC('ALTER TABLE [Movie] DROP CONSTRAINT [' + @default_name + ']');

                    ALTER TABLE [Movie] DROP COLUMN [Cast];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore legacy Director/Cast columns, backfill from the normalized
            // tables, then drop the new tables. Best-effort — the original
            // Cast ordering is preserved via DisplayOrder.

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns
                               WHERE name = 'Director'
                                 AND object_id = OBJECT_ID('dbo.Movie'))
                BEGIN
                    ALTER TABLE [Movie] ADD [Director] NVARCHAR(100) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns
                               WHERE name = 'Cast'
                                 AND object_id = OBJECT_ID('dbo.Movie'))
                BEGIN
                    ALTER TABLE [Movie] ADD [Cast] NVARCHAR(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables
                           WHERE name = 'MoviePerson'
                             AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    DECLARE @sql NVARCHAR(MAX);
                    SET @sql = N'
                    UPDATE m
                    SET Director = d.Name
                    FROM dbo.Movie m
                    CROSS APPLY (
                        SELECT TOP 1 p.Name
                        FROM dbo.MoviePerson mp
                        INNER JOIN dbo.Person p ON p.PersonId = mp.PersonId
                        WHERE mp.MovieId = m.MovieID
                          AND mp.Role = ''Director''
                        ORDER BY mp.DisplayOrder
                    ) d;

                    ;WITH Actors AS (
                        SELECT
                            mp.MovieId,
                            STUFF((
                                SELECT '', '' + p2.Name
                                FROM dbo.MoviePerson mp2
                                INNER JOIN dbo.Person p2 ON p2.PersonId = mp2.PersonId
                                WHERE mp2.MovieId = mp.MovieId
                                  AND mp2.Role = ''Actor''
                                ORDER BY mp2.DisplayOrder
                                FOR XML PATH(''''), TYPE
                            ).value(''.'', ''NVARCHAR(MAX)''), 1, 2, '''') AS CastList
                        FROM dbo.MoviePerson mp
                        WHERE mp.Role = ''Actor''
                        GROUP BY mp.MovieId
                    )
                    UPDATE m
                    SET Cast = a.CastList
                    FROM dbo.Movie m
                    INNER JOIN Actors a ON a.MovieId = m.MovieID;
                    ';
                    EXEC sp_executesql @sql;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables
                           WHERE name = 'MoviePerson'
                             AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    DROP TABLE [MoviePerson];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables
                           WHERE name = 'Person'
                             AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    DROP TABLE [Person];
                END
            ");
        }
    }
}
