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
