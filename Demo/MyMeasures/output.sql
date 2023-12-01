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
DROP FUNCTION NumbersInMyBrain
GO
CREATE FUNCTION NumbersInMyBrain
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   (VALUES ('59621000', 'http://brain.org', NULL, NULL)) AS codes(code, codesystem, display, ver)

GO
DROP FUNCTION Jet_engine_conditions2
GO
CREATE FUNCTION Jet_engine_conditions2
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   condition AS sourceTable

GO
DROP FUNCTION Jet_engine_conditions
GO
CREATE FUNCTION Jet_engine_conditions
( )
RETURNS TABLE 
AS
RETURN 
    SELECT *
    FROM   condition AS sourceTable
           INNER JOIN
           (SELECT TOP 1 *
            FROM   Sucked_into_jet_engine()) AS codeTable
           ON sourceTable.code_coding_code = codeTable.code
              AND sourceTable.code_coding_system = codeTable.codesystem

GO
DROP FUNCTION Ouch
GO
CREATE FUNCTION Ouch
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
