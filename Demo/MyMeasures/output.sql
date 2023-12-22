-- start Sucked_into_jet_engine
DROP FUNCTION Sucked_into_jet_engine
GO
CREATE FUNCTION Sucked_into_jet_engine
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   (VALUES ('V97.33', 'http://hl7.org/fhir/sid/icd-10', NULL, NULL)) AS codes(code, codesystem, display, ver)
GO
-- start Sucked_into_jet_engine__subsequent_encounter
DROP FUNCTION Sucked_into_jet_engine__subsequent_encounter
GO
CREATE FUNCTION Sucked_into_jet_engine__subsequent_encounter
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   (VALUES ('V97.33XD', 'http://hl7.org/fhir/sid/icd-10', NULL, NULL)) AS codes(code, codesystem, display, ver)
GO
-- start Ouchie
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
-- start ICD10
DROP FUNCTION ICD10
GO
CREATE FUNCTION ICD10
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   (VALUES ('V97.33', 'http://hl7.org/fhir/sid/icd-10', NULL, NULL), ('V97.33XD', 'http://hl7.org/fhir/sid/icd-10', NULL, NULL)) AS codes(code, codesystem, display, ver)
GO
-- start SnoMed
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
-- start First
DROP FUNCTION First
GO
CREATE FUNCTION First
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 1 AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED)
GO
-- start Second
DROP FUNCTION Second
GO
CREATE FUNCTION Second
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 1 + 1 AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED)
GO
-- start PEDMASTest
DROP FUNCTION PEDMASTest
GO
CREATE FUNCTION PEDMASTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 (CAST ((3) AS DECIMAL) + 4.0) / CAST ((1 + 2) AS DECIMAL) AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED)
GO
-- start CompoundMathTest
DROP FUNCTION CompoundMathTest
GO
CREATE FUNCTION CompoundMathTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 CAST ((1) AS DECIMAL) + PEDMASTest.Result AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED CROSS APPLY PEDMASTest() AS PEDMASTest)
GO
-- start MultipleCompoundMathTest
DROP FUNCTION MultipleCompoundMathTest
GO
CREATE FUNCTION MultipleCompoundMathTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 CompoundMathTest.Result + PEDMASTest.Result * CAST ((2) AS DECIMAL) AS Result
     FROM   CompoundMathTest() AS CompoundMathTest CROSS APPLY (SELECT NULL AS unused_column) AS UNUSED CROSS APPLY PEDMASTest() AS PEDMASTest)
GO
-- start SimpleRefTest
DROP FUNCTION SimpleRefTest
GO
CREATE FUNCTION SimpleRefTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 MultipleCompoundMathTest.Result AS Result
     FROM   MultipleCompoundMathTest() AS MultipleCompoundMathTest)
GO
-- start SimpleTest
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
-- start CodeTest
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
           (SELECT TOP 1 code,
                         codesystem,
                         display,
                         ver
            FROM   Ouchie()) AS codeTable
           ON sourceTable.code_coding_code = codeTable.code
              AND sourceTable.code_coding_system = codeTable.codesystem
GO
-- start DateTest2
DROP FUNCTION DateTest2
GO
CREATE FUNCTION DateTest2
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   condition AS sourceTable
    WHERE  sourceTable.onsetDateTime > ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS Result
                                         FROM   (SELECT NULL AS unused_column) AS UNUSED))
GO
-- start DateTest3
DROP FUNCTION DateTest3
GO
CREATE FUNCTION DateTest3
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   condition AS sourceTable
    WHERE  sourceTable.onsetDateTime > ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS Result
                                         FROM   (SELECT NULL AS unused_column) AS UNUSED))
           AND sourceTable.onsetDateTime < ((SELECT TOP 1 DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS Result
                                             FROM   (SELECT NULL AS unused_column) AS UNUSED))
GO
-- start DateTest4
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
           (SELECT TOP 1 code,
                         codesystem,
                         display,
                         ver
            FROM   Ouchie()) AS codeTable
           ON sourceTable.code_coding_code = codeTable.code
              AND sourceTable.code_coding_system = codeTable.codesystem
    WHERE  sourceTable.onsetDateTime > ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS Result
                                         FROM   (SELECT NULL AS unused_column) AS UNUSED))
           AND sourceTable.onsetDateTime < ((SELECT TOP 1 DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS Result
                                             FROM   (SELECT NULL AS unused_column) AS UNUSED))
GO
-- start SimpleRetrieveReferenceTest
DROP FUNCTION SimpleRetrieveReferenceTest
GO
CREATE FUNCTION SimpleRetrieveReferenceTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT SimpleTest.*
     FROM   SimpleTest() AS SimpleTest)
GO
-- start RetrieveReferenceWithFilterTest
DROP FUNCTION RetrieveReferenceWithFilterTest
GO
CREATE FUNCTION RetrieveReferenceWithFilterTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT CodeTest.*
     FROM   CodeTest() AS CodeTest
     WHERE  CodeTest.onsetDateTime > ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS Result
                                       FROM   (SELECT NULL AS unused_column) AS UNUSED)))
GO
-- start MultipleNestedTest1
DROP FUNCTION MultipleNestedTest1
GO
CREATE FUNCTION MultipleNestedTest1
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT RetrieveReferenceWithFilterTest.*
     FROM   RetrieveReferenceWithFilterTest() AS RetrieveReferenceWithFilterTest)
GO
-- start MultipleNestedTest2
DROP FUNCTION MultipleNestedTest2
GO
CREATE FUNCTION MultipleNestedTest2
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT MultipleNestedTest1.*
     FROM   MultipleNestedTest1() AS MultipleNestedTest1
     WHERE  MultipleNestedTest1.onsetDateTime < ((SELECT TOP 1 DATETIME2FROMPARTS(2021, 1, 1, 0, 0, 0, 0, 7) AS Result
                                                  FROM   (SELECT NULL AS unused_column) AS UNUSED)))
GO
