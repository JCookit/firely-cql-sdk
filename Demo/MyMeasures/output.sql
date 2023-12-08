DROP FUNCTION Ouchie
GO
CREATE FUNCTION Ouchie
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   (VALUES ('59621000', 'http://snomed.info/sct', NULL, NULL)) AS codes(code, codesystem, display, ver)

GO
DROP FUNCTION SnoMed
GO
CREATE FUNCTION SnoMed
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   (VALUES ('59621000', 'http://snomed.info/sct', NULL, NULL)) AS codes(code, codesystem, display, ver)

GO
DROP FUNCTION First
GO
CREATE FUNCTION First
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT 1 AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED)

GO
DROP FUNCTION Second
GO
CREATE FUNCTION Second
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT 1 + 1 AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED)

GO
DROP FUNCTION PEDMASTest
GO
CREATE FUNCTION PEDMASTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT (CAST ((3) AS DECIMAL) + 4.0) / CAST ((1 + 2) AS DECIMAL) AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED)

GO
DROP FUNCTION CompoundMathTest
GO
CREATE FUNCTION CompoundMathTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT CAST ((1) AS DECIMAL) + PEDMASTest.Result AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED CROSS APPLY PEDMASTest() AS PEDMASTest)

GO
DROP FUNCTION MultipleCompoundMathTest
GO
CREATE FUNCTION MultipleCompoundMathTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT CompoundMathTest.Result + PEDMASTest.Result * CAST ((2) AS DECIMAL) AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED CROSS APPLY CompoundMathTest() AS CompoundMathTest CROSS APPLY PEDMASTest() AS PEDMASTest)

GO
DROP FUNCTION SimpleRefTest
GO
CREATE FUNCTION SimpleRefTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT MultipleCompoundMathTest.Result AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED CROSS APPLY MultipleCompoundMathTest() AS MultipleCompoundMathTest)

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
