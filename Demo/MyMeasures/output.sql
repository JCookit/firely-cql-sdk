DROP FUNCTION Ouchie
GO
CREATE FUNCTION Ouchie
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   (VALUES ('59621000', 'http://brain.org', NULL, NULL)) AS codes(code, codesystem, display, ver)

GO
DROP FUNCTION SnoMed
GO
CREATE FUNCTION SnoMed
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   (VALUES ('59621000', 'http://brain.org', NULL, NULL)) AS codes(code, codesystem, display, ver)

GO
DROP FUNCTION SimpleTest
GO
CREATE FUNCTION SimpleTest
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   condition AS sourceTable

GO
DROP FUNCTION CodeTest
GO
CREATE FUNCTION CodeTest
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   condition AS sourceTable
           INNER JOIN
           (SELECT TOP 1 *
            FROM   Ouchie()) AS codeTable
           ON sourceTable.code_coding_code = codeTable.code
              AND sourceTable.code_coding_system = codeTable.codesystem

GO
DROP FUNCTION DateTest2
GO
CREATE FUNCTION DateTest2
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   condition AS sourceTable
    WHERE  sourceTable.onsetDateTime > ((SELECT DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS Result
                                         FROM   (SELECT NULL AS unused_column) AS UNUSED))

GO
DROP FUNCTION DateTest3
GO
CREATE FUNCTION DateTest3
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   condition AS sourceTable
    WHERE  sourceTable.onsetDateTime > ((SELECT DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS Result
                                         FROM   (SELECT NULL AS unused_column) AS UNUSED))
           AND sourceTable.onsetDateTime < ((SELECT DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS Result
                                             FROM   (SELECT NULL AS unused_column) AS UNUSED))

GO
DROP FUNCTION DateTest4
GO
CREATE FUNCTION DateTest4
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   condition AS sourceTable
           INNER JOIN
           (SELECT TOP 1 *
            FROM   Ouchie()) AS codeTable
           ON sourceTable.code_coding_code = codeTable.code
              AND sourceTable.code_coding_system = codeTable.codesystem
    WHERE  sourceTable.onsetDateTime > ((SELECT DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS Result
                                         FROM   (SELECT NULL AS unused_column) AS UNUSED))
           AND sourceTable.onsetDateTime < ((SELECT DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS Result
                                             FROM   (SELECT NULL AS unused_column) AS UNUSED))

GO
