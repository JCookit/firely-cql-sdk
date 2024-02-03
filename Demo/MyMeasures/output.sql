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
-- start AllPatients
DROP FUNCTION AllPatients
GO
CREATE FUNCTION AllPatients
( )
RETURNS TABLE 
AS
RETURN 
    SELECT _sourceTable.*
    FROM   patient AS _sourceTable
GO
-- start ExplicitSingletonFrom
DROP FUNCTION ExplicitSingletonFrom
GO
CREATE FUNCTION ExplicitSingletonFrom
( )
RETURNS TABLE 
AS
RETURN 
    SELECT TOP 1 *
    FROM   ((SELECT AllPatients.*
             FROM   AllPatients() AS AllPatients)) AS _UNUSED
GO
-- start PatientDateTest
DROP FUNCTION PatientDateTest
GO
CREATE FUNCTION PatientDateTest
( )
RETURNS TABLE 
AS
RETURN 
    SELECT _sourceTable.*
    FROM   patient AS _sourceTable
    WHERE  _sourceTable.birthDate > ((SELECT TOP 1 DATEFROMPARTS(1970, 1, 1) AS _Result
                                      FROM   (SELECT NULL AS _unused_column) AS _UNUSED))
GO
-- start PatientCountTest
DROP FUNCTION PatientCountTest
GO
CREATE FUNCTION PatientCountTest
( )
RETURNS TABLE 
AS
RETURN 
    SELECT COUNT(1) AS _Result
    FROM   ((SELECT PatientDateTest.*
             FROM   PatientDateTest() AS PatientDateTest)) AS _UNUSED
GO
-- start Patient
DROP FUNCTION Patient
GO
CREATE FUNCTION Patient
( )
RETURNS TABLE 
AS
RETURN 
    SELECT TOP 1 *
    FROM   (SELECT _sourceTable.*,
                   _sourceTable.id AS _Context
            FROM   patient AS _sourceTable) AS _UNUSED
GO
-- start AgeInYearsTest
DROP FUNCTION AgeInYearsTest
GO
CREATE FUNCTION AgeInYearsTest
( )
RETURNS TABLE 
AS
RETURN 
    SELECT Patient.birthDate AS _Result
    FROM   Patient() AS Patient
GO
-- start PatientContextRetrieve
DROP FUNCTION PatientContextRetrieve
GO
CREATE FUNCTION PatientContextRetrieve
( )
RETURNS TABLE 
AS
RETURN 
    SELECT _sourceTable.*,
           JSON_VALUE(_sourceTable.subject_string, '$.id') AS _Context
    FROM   condition AS _sourceTable
    WHERE  _sourceTable.onsetDateTime > ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS _Result
                                          FROM   (SELECT NULL AS _unused_column) AS _UNUSED))
GO
-- start PatientConditionCountTest
DROP FUNCTION PatientConditionCountTest
GO
CREATE FUNCTION PatientConditionCountTest
( )
RETURNS TABLE 
AS
RETURN 
    SELECT COUNT(1) AS _Result
    FROM   ((SELECT PatientContextRetrieve.*
             FROM   PatientContextRetrieve() AS PatientContextRetrieve)) AS _UNUSED
GO
-- start FirstCompare
DROP FUNCTION FirstCompare
GO
CREATE FUNCTION FirstCompare
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF (((SELECT TOP 1 1 AS _Result
                  FROM   (SELECT NULL AS _unused_column) AS _UNUSED)) < ((SELECT TOP 1 2 AS _Result
                                                                          FROM   (SELECT NULL AS _unused_column) AS _UNUSED)), 1, 0) AS _Result
    FROM   (SELECT NULL AS _unused_column) AS _UNUSED
GO
-- start SecondCompare
DROP FUNCTION SecondCompare
GO
CREATE FUNCTION SecondCompare
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF (FirstCompare._Result = 1
                AND ((SELECT TOP 1 2 AS _Result
                      FROM   (SELECT NULL AS _unused_column) AS _UNUSED)) < ((SELECT TOP 1 3 AS _Result
                                                                              FROM   (SELECT NULL AS _unused_column) AS _UNUSED)), 1, 0) AS _Result
    FROM   (SELECT NULL AS _unused_column) AS _UNUSED CROSS APPLY FirstCompare() AS FirstCompare
GO
-- start ThirdCompare
DROP FUNCTION ThirdCompare
GO
CREATE FUNCTION ThirdCompare
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF (SecondCompare._Result = 1
                OR (5 >= ((SELECT TOP 1 1 AS _Result
                           FROM   (SELECT NULL AS _unused_column) AS _UNUSED))
                    AND 5 <= ((SELECT TOP 1 10 AS _Result
                               FROM   (SELECT NULL AS _unused_column) AS _UNUSED))), 1, 0) AS _Result
    FROM   (SELECT NULL AS _unused_column) AS _UNUSED CROSS APPLY SecondCompare() AS SecondCompare
GO
-- start FourthCompare
DROP FUNCTION FourthCompare
GO
CREATE FUNCTION FourthCompare
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF (FirstCompare._Result = 1
                AND SecondCompare._Result = 1
                AND ThirdCompare._Result = 1, 1, 0) AS _Result
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
    (SELECT TOP 1 1 AS _Result
     FROM   (SELECT NULL AS _unused_column) AS _UNUSED)
GO
-- start SimpleFalse
DROP FUNCTION SimpleFalse
GO
CREATE FUNCTION SimpleFalse
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 0 AS _Result
     FROM   (SELECT NULL AS _unused_column) AS _UNUSED)
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
                AND 1 = 1, 1, 0) AS _Result
    FROM   (SELECT NULL AS _unused_column) AS _UNUSED
GO
-- start First
DROP FUNCTION First
GO
CREATE FUNCTION First
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 1 AS _Result
     FROM   (SELECT NULL AS _unused_column) AS _UNUSED)
GO
-- start Second
DROP FUNCTION Second
GO
CREATE FUNCTION Second
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 1 + 1 AS _Result
     FROM   (SELECT NULL AS _unused_column) AS _UNUSED)
GO
-- start PEDMASTest
DROP FUNCTION PEDMASTest
GO
CREATE FUNCTION PEDMASTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 (CAST ((3) AS DECIMAL) + 4.0) / CAST ((1 + 2) AS DECIMAL) AS _Result
     FROM   (SELECT NULL AS _unused_column) AS _UNUSED)
GO
-- start CompoundMathTest
DROP FUNCTION CompoundMathTest
GO
CREATE FUNCTION CompoundMathTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 CAST ((1) AS DECIMAL) + PEDMASTest._Result AS _Result
     FROM   (SELECT NULL AS _unused_column) AS _UNUSED CROSS APPLY PEDMASTest() AS PEDMASTest)
GO
-- start MultipleCompoundMathTest
DROP FUNCTION MultipleCompoundMathTest
GO
CREATE FUNCTION MultipleCompoundMathTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 CompoundMathTest._Result + PEDMASTest._Result * CAST ((2) AS DECIMAL) AS _Result
     FROM   CompoundMathTest() AS CompoundMathTest CROSS APPLY (SELECT NULL AS _unused_column) AS _UNUSED CROSS APPLY PEDMASTest() AS PEDMASTest)
GO
-- start SimpleRefTest
DROP FUNCTION SimpleRefTest
GO
CREATE FUNCTION SimpleRefTest
( )
RETURNS TABLE 
AS
RETURN 
    (SELECT TOP 1 MultipleCompoundMathTest._Result AS _Result
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
    SELECT _sourceTable.*
    FROM   condition AS _sourceTable
GO
-- start CodeTest
DROP FUNCTION CodeTest
GO
CREATE FUNCTION CodeTest
( )
RETURNS TABLE 
AS
RETURN 
    SELECT _sourceTable.*
    FROM   condition AS _sourceTable
           INNER JOIN
           (SELECT TOP 1 code,
                         codesystem,
                         display,
                         ver
            FROM   Ouchie()) AS codeTable
           ON _sourceTable.code_coding_code = codeTable.code
              AND _sourceTable.code_coding_system = codeTable.codesystem
GO
-- start DateTest2
DROP FUNCTION DateTest2
GO
CREATE FUNCTION DateTest2
( )
RETURNS TABLE 
AS
RETURN 
    SELECT _sourceTable.*
    FROM   condition AS _sourceTable
    WHERE  _sourceTable.onsetDateTime > ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS _Result
                                          FROM   (SELECT NULL AS _unused_column) AS _UNUSED))
GO
-- start DateTest3
DROP FUNCTION DateTest3
GO
CREATE FUNCTION DateTest3
( )
RETURNS TABLE 
AS
RETURN 
    SELECT _sourceTable.*
    FROM   condition AS _sourceTable
    WHERE  _sourceTable.onsetDateTime > ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS _Result
                                          FROM   (SELECT NULL AS _unused_column) AS _UNUSED))
           AND _sourceTable.onsetDateTime < ((SELECT TOP 1 DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS _Result
                                              FROM   (SELECT NULL AS _unused_column) AS _UNUSED))
GO
-- start DateTest4
DROP FUNCTION DateTest4
GO
CREATE FUNCTION DateTest4
( )
RETURNS TABLE 
AS
RETURN 
    SELECT _sourceTable.*
    FROM   condition AS _sourceTable
           INNER JOIN
           (SELECT TOP 1 code,
                         codesystem,
                         display,
                         ver
            FROM   Ouchie()) AS codeTable
           ON _sourceTable.code_coding_code = codeTable.code
              AND _sourceTable.code_coding_system = codeTable.codesystem
    WHERE  _sourceTable.onsetDateTime > ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS _Result
                                          FROM   (SELECT NULL AS _unused_column) AS _UNUSED))
           AND _sourceTable.onsetDateTime < ((SELECT TOP 1 DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS _Result
                                              FROM   (SELECT NULL AS _unused_column) AS _UNUSED))
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
     FROM   (SELECT NULL AS _unused_column) AS _UNUSED)
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
     FROM   (SELECT NULL AS _unused_column) AS _UNUSED)
GO
-- start IntervalIntegerReferenceTestDoesntWork
DROP FUNCTION IntervalIntegerReferenceTestDoesntWork
GO
CREATE FUNCTION IntervalIntegerReferenceTestDoesntWork
( )
RETURNS TABLE 
AS
RETURN 
    SELECT IIF ((5 >= ((SELECT TOP 1 IntervalIntegerDefinition.low AS _Result
                        FROM   IntervalIntegerDefinition() AS IntervalIntegerDefinition))
                 AND 5 <= ((SELECT TOP 1 IntervalIntegerDefinition.hi AS _Result
                            FROM   IntervalIntegerDefinition() AS IntervalIntegerDefinition))), 1, 0) AS _Result
    FROM   (SELECT NULL AS _unused_column) AS _UNUSED CROSS APPLY IntervalIntegerDefinition() AS IntervalIntegerDefinition
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
     WHERE  (CodeTest.onsetDateTime >= ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS _Result
                                         FROM   (SELECT NULL AS _unused_column) AS _UNUSED))
             AND CodeTest.onsetDateTime < ((SELECT TOP 1 DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS _Result
                                            FROM   (SELECT NULL AS _unused_column) AS _UNUSED))))
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
                          FROM   IntervalTest() AS IntervalTest)) AS _UNUSED) > 0, 1, 0) AS _Result
    FROM   (SELECT NULL AS _unused_column) AS _UNUSED
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
     WHERE  CodeTest.onsetDateTime > ((SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS _Result
                                       FROM   (SELECT NULL AS _unused_column) AS _UNUSED)))
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
     WHERE  MultipleNestedTest1.onsetDateTime < ((SELECT TOP 1 DATETIME2FROMPARTS(2021, 1, 1, 0, 0, 0, 0, 7) AS _Result
                                                  FROM   (SELECT NULL AS _unused_column) AS _UNUSED)))
GO
