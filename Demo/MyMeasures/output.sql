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
-- start FirstCompare
DROP FUNCTION FirstCompare
GO
CREATE FUNCTION FirstCompare
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF (((SELECT TOP 1 1 AS Result
                  FROM   (SELECT NULL AS unused_column) AS UNUSED)) < ((SELECT TOP 1 2 AS Result
                                                                        FROM   (SELECT NULL AS unused_column) AS UNUSED)), 1, 0) AS Result
    FROM   (SELECT NULL AS unused_column) AS UNUSED
GO
-- start SecondCompare
DROP FUNCTION SecondCompare
GO
CREATE FUNCTION SecondCompare
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF (FirstCompare.Result = 1
                AND ((SELECT TOP 1 2 AS Result
                      FROM   (SELECT NULL AS unused_column) AS UNUSED)) < ((SELECT TOP 1 3 AS Result
                                                                            FROM   (SELECT NULL AS unused_column) AS UNUSED)), 1, 0) AS Result
    FROM   (SELECT NULL AS unused_column) AS UNUSED CROSS APPLY FirstCompare() AS FirstCompare
GO
-- start ThirdCompare
DROP FUNCTION ThirdCompare
GO
CREATE FUNCTION ThirdCompare
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF (SecondCompare.Result = 1
                OR (5 >= ((SELECT TOP 1 1 AS Result
                           FROM   (SELECT NULL AS unused_column) AS UNUSED))
                    AND 5 <= ((SELECT TOP 1 10 AS Result
                               FROM   (SELECT NULL AS unused_column) AS UNUSED))), 1, 0) AS Result
    FROM   (SELECT NULL AS unused_column) AS UNUSED CROSS APPLY SecondCompare() AS SecondCompare
GO
-- start FourthCompare
DROP FUNCTION FourthCompare
GO
CREATE FUNCTION FourthCompare
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF (FirstCompare.Result = 1
                AND SecondCompare.Result = 1
                AND ThirdCompare.Result = 1, 1, 0) AS Result
    FROM   FirstCompare() AS FirstCompare CROSS APPLY SecondCompare() AS SecondCompare CROSS APPLY ThirdCompare() AS ThirdCompare
GO
-- start SimpleTrue
DROP FUNCTION SimpleTrue
GO
CREATE FUNCTION SimpleTrue
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 1 AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED)
GO
-- start SimpleFalse
DROP FUNCTION SimpleFalse
GO
CREATE FUNCTION SimpleFalse
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 0 AS Result
     FROM   (SELECT NULL AS unused_column) AS UNUSED)
GO
-- start SimpleAnd
DROP FUNCTION SimpleAnd
GO
CREATE FUNCTION SimpleAnd
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF (1 = 1
                AND 1 = 1, 1, 0) AS Result
    FROM   (SELECT NULL AS unused_column) AS UNUSED
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
-- start IntervalDateDefinition
DROP FUNCTION IntervalDateDefinition
GO
CREATE FUNCTION IntervalDateDefinition
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS low,
                  DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS hi,
                  1 AS lowClosed,
                  0 AS hiClosed
     FROM   (SELECT NULL AS unused_column) AS UNUSED)
GO
-- start IntervalIntegerDefinition
DROP FUNCTION IntervalIntegerDefinition
GO
CREATE FUNCTION IntervalIntegerDefinition
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 1 AS low,
                  10 * 3 AS hi,
                  0 AS lowClosed,
                  1 AS hiClosed
     FROM   (SELECT NULL AS unused_column) AS UNUSED)
GO
-- start IntervalIntegerReferenceTestDoesntWork
DROP FUNCTION IntervalIntegerReferenceTestDoesntWork
GO
CREATE FUNCTION IntervalIntegerReferenceTestDoesntWork
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF ((5 >= ((SELECT TOP 1 IntervalIntegerDefinition.low AS Result
                        FROM   IntervalIntegerDefinition() AS IntervalIntegerDefinition))
                 AND 5 <= ((SELECT TOP 1 IntervalIntegerDefinition.hi AS Result
                            FROM   IntervalIntegerDefinition() AS IntervalIntegerDefinition))), 1, 0) AS Result
    FROM   (SELECT NULL AS unused_column) AS UNUSED CROSS APPLY IntervalIntegerDefinition() AS IntervalIntegerDefinition
GO
-- start IntervalTest
DROP FUNCTION IntervalTest
GO
CREATE FUNCTION IntervalTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT CodeTest.*
     FROM   CodeTest() AS CodeTest
     WHERE  (CodeTest.onsetDateTime >= ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS Result
                                         FROM   (SELECT NULL AS unused_column) AS UNUSED))
             AND CodeTest.onsetDateTime < ((SELECT TOP 1 DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS Result
                                            FROM   (SELECT NULL AS unused_column) AS UNUSED))))
GO
-- start FirstExists
DROP FUNCTION FirstExists
GO
CREATE FUNCTION FirstExists
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF ((SELECT COUNT(1)
                 FROM   ((SELECT IntervalTest.*
                          FROM   IntervalTest() AS IntervalTest)) AS UNUSED) > 0, 1, 0)
    FROM   (SELECT NULL AS unused_column) AS UNUSED
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
