--
-- start Flexible_Sigmoidoscopy
--
DROP FUNCTION Flexible_Sigmoidoscopy
GO

CREATE FUNCTION Flexible_Sigmoidoscopy ()
RETURNS TABLE
AS
RETURN

SELECT *
FROM (
	VALUES (
		'73761001'
		,'http://snomed.info/sct'
		,NULL
		,NULL
		)
	) AS codes(code, codesystem, display, ver)
GO

--
-- start Hypertension
--
DROP FUNCTION Hypertension
GO

CREATE FUNCTION Hypertension ()
RETURNS TABLE
AS
RETURN

SELECT *
FROM (
	VALUES (
		'59621000'
		,'http://snomed.info/sct'
		,NULL
		,NULL
		)
	) AS codes(code, codesystem, display, ver)
GO

--
-- start SnoMed
--
DROP FUNCTION SnoMed
GO

CREATE FUNCTION SnoMed ()
RETURNS TABLE
AS
RETURN

SELECT *
FROM (
	VALUES (
		'73761001'
		,'http://snomed.info/sct'
		,NULL
		,NULL
		)
		,(
		'59621000'
		,'http://snomed.info/sct'
		,NULL
		,NULL
		)
	) AS codes(code, codesystem, display, ver)
GO

--
-- start Measurement_Period
--
DROP FUNCTION Measurement_Period
GO

CREATE FUNCTION Measurement_Period ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 DATETIME2FROMPARTS(2019, 1, 1, 0, 0, 0, 0, 7) AS [low]
			,DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS [hi]
			,1 AS [lowClosed]
			,0 AS [hiClosed]
		)
GO

--
-- start Patient
--
DROP FUNCTION Patient
GO

CREATE FUNCTION Patient ()
RETURNS TABLE
AS
RETURN

SELECT *
FROM (
	SELECT [_sourceTable].*
		,[_sourceTable].[id] AS [_Context]
	FROM [patient] AS [_sourceTable]
	) AS [_UNUSED]
GO

--
-- start InInitialPopulation
--
DROP FUNCTION InInitialPopulation
GO

CREATE FUNCTION InInitialPopulation ()
RETURNS TABLE
AS
RETURN

SELECT IIF(DATEDIFF(YEAR, [Patient].[birthDate], CASE (
					SELECT [op2].[units]
					FROM (
						(
							SELECT 5 AS [value]
								,'years' AS [units]
							)
						) AS [op2]
					)
				WHEN 'years'
					THEN DATEADD(year, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
									FROM (
										SELECT [Measurement_Period].[low] AS [low]
											,[Measurement_Period].[hi] AS [hi]
											,[Measurement_Period].[lowClosed] AS [lowClosed]
											,[Measurement_Period].[hiClosed] AS [hiClosed]
										FROM [Measurement_Period]() AS [Measurement_Period]
										) AS [_sourceTable]
									) AS [op1]
								))
				WHEN 'months'
					THEN DATEADD(month, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
									FROM (
										SELECT [Measurement_Period].[low] AS [low]
											,[Measurement_Period].[hi] AS [hi]
											,[Measurement_Period].[lowClosed] AS [lowClosed]
											,[Measurement_Period].[hiClosed] AS [hiClosed]
										FROM [Measurement_Period]() AS [Measurement_Period]
										) AS [_sourceTable]
									) AS [op1]
								))
				WHEN 'weeks'
					THEN DATEADD(week, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
									FROM (
										SELECT [Measurement_Period].[low] AS [low]
											,[Measurement_Period].[hi] AS [hi]
											,[Measurement_Period].[lowClosed] AS [lowClosed]
											,[Measurement_Period].[hiClosed] AS [hiClosed]
										FROM [Measurement_Period]() AS [Measurement_Period]
										) AS [_sourceTable]
									) AS [op1]
								))
				WHEN 'days'
					THEN DATEADD(day, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
									FROM (
										SELECT [Measurement_Period].[low] AS [low]
											,[Measurement_Period].[hi] AS [hi]
											,[Measurement_Period].[lowClosed] AS [lowClosed]
											,[Measurement_Period].[hiClosed] AS [hiClosed]
										FROM [Measurement_Period]() AS [Measurement_Period]
										) AS [_sourceTable]
									) AS [op1]
								))
				END) - CASE 
			WHEN (
					MONTH([Patient].[birthDate]) > MONTH(CASE (
								SELECT [op2].[units]
								FROM (
									(
										SELECT 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								)
							WHEN 'years'
								THEN DATEADD(year, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							WHEN 'months'
								THEN DATEADD(month, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							WHEN 'weeks'
								THEN DATEADD(week, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							WHEN 'days'
								THEN DATEADD(day, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							END)
					)
				OR (
					MONTH([Patient].[birthDate]) = MONTH(CASE (
								SELECT [op2].[units]
								FROM (
									(
										SELECT 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								)
							WHEN 'years'
								THEN DATEADD(year, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							WHEN 'months'
								THEN DATEADD(month, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							WHEN 'weeks'
								THEN DATEADD(week, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							WHEN 'days'
								THEN DATEADD(day, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							END)
					AND DAY([Patient].[birthDate]) > DAY(CASE (
								SELECT [op2].[units]
								FROM (
									(
										SELECT 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								)
							WHEN 'years'
								THEN DATEADD(year, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							WHEN 'months'
								THEN DATEADD(month, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							WHEN 'weeks'
								THEN DATEADD(week, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							WHEN 'days'
								THEN DATEADD(day, (
											SELECT - [op2].[value]
											FROM (
												(
													SELECT 5 AS [value]
														,'years' AS [units]
													)
												) AS [op2]
											), (
											SELECT [op1].[_Result]
											FROM (
												SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
												FROM (
													SELECT [Measurement_Period].[low] AS [low]
														,[Measurement_Period].[hi] AS [hi]
														,[Measurement_Period].[lowClosed] AS [lowClosed]
														,[Measurement_Period].[hiClosed] AS [hiClosed]
													FROM [Measurement_Period]() AS [Measurement_Period]
													) AS [_sourceTable]
												) AS [op1]
											))
							END)
					)
				THEN 1
			ELSE 0
			END >= 50, 1, 0) AS [_Result]
	,[_Context]
FROM [Patient]() AS [Patient]
GO

--
-- start FlexibleSigmoidoscopyConditions
--
DROP FUNCTION FlexibleSigmoidoscopyConditions
GO

CREATE FUNCTION FlexibleSigmoidoscopyConditions ()
RETURNS TABLE
AS
RETURN

SELECT [_sourceTable].*
	,JSON_VALUE([_sourceTable].[subject_string], '$.id') AS [_Context]
FROM [procedure] AS [_sourceTable]
INNER JOIN (
	SELECT TOP 1 code
		,codesystem
		,display
		,ver
	FROM [Flexible_Sigmoidoscopy]()
	) AS codeTable ON [_sourceTable].[code_coding_code] = [codeTable].[code]
	AND [_sourceTable].[code_coding_system] = [codeTable].[codesystem]
GO

--
-- start FlexibleSigmoidoscopyPerformed
--
DROP FUNCTION FlexibleSigmoidoscopyPerformed
GO

CREATE FUNCTION FlexibleSigmoidoscopyPerformed ()
RETURNS TABLE
AS
RETURN (
		SELECT [FlexibleSigmoidoscopyConditions].*
		FROM [FlexibleSigmoidoscopyConditions]() AS [FlexibleSigmoidoscopyConditions]
		WHERE [FlexibleSigmoidoscopyConditions].[status] = 'completed'
			AND (
				[FlexibleSigmoidoscopyConditions].[performedPeriod_end] >= (
					(
						SELECT CASE (
									SELECT [op2].[units]
									FROM (
										(
											SELECT 5 AS [value]
												,'years' AS [units]
											)
										) AS [op2]
									)
								WHEN 'years'
									THEN DATEADD(year, (
												SELECT - [op2].[value]
												FROM (
													(
														SELECT 5 AS [value]
															,'years' AS [units]
														)
													) AS [op2]
												), (
												SELECT [op1].[_Result]
												FROM (
													SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
													FROM (
														SELECT [Measurement_Period].[low] AS [low]
															,[Measurement_Period].[hi] AS [hi]
															,[Measurement_Period].[lowClosed] AS [lowClosed]
															,[Measurement_Period].[hiClosed] AS [hiClosed]
														FROM [Measurement_Period]() AS [Measurement_Period]
														) AS [_sourceTable]
													) AS [op1]
												))
								WHEN 'months'
									THEN DATEADD(month, (
												SELECT - [op2].[value]
												FROM (
													(
														SELECT 5 AS [value]
															,'years' AS [units]
														)
													) AS [op2]
												), (
												SELECT [op1].[_Result]
												FROM (
													SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
													FROM (
														SELECT [Measurement_Period].[low] AS [low]
															,[Measurement_Period].[hi] AS [hi]
															,[Measurement_Period].[lowClosed] AS [lowClosed]
															,[Measurement_Period].[hiClosed] AS [hiClosed]
														FROM [Measurement_Period]() AS [Measurement_Period]
														) AS [_sourceTable]
													) AS [op1]
												))
								WHEN 'weeks'
									THEN DATEADD(week, (
												SELECT - [op2].[value]
												FROM (
													(
														SELECT 5 AS [value]
															,'years' AS [units]
														)
													) AS [op2]
												), (
												SELECT [op1].[_Result]
												FROM (
													SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
													FROM (
														SELECT [Measurement_Period].[low] AS [low]
															,[Measurement_Period].[hi] AS [hi]
															,[Measurement_Period].[lowClosed] AS [lowClosed]
															,[Measurement_Period].[hiClosed] AS [hiClosed]
														FROM [Measurement_Period]() AS [Measurement_Period]
														) AS [_sourceTable]
													) AS [op1]
												))
								WHEN 'days'
									THEN DATEADD(day, (
												SELECT - [op2].[value]
												FROM (
													(
														SELECT 5 AS [value]
															,'years' AS [units]
														)
													) AS [op2]
												), (
												SELECT [op1].[_Result]
												FROM (
													SELECT TOP 1 [_sourceTable].[hi] AS [_Result]
													FROM (
														SELECT [Measurement_Period].[low] AS [low]
															,[Measurement_Period].[hi] AS [hi]
															,[Measurement_Period].[lowClosed] AS [lowClosed]
															,[Measurement_Period].[hiClosed] AS [hiClosed]
														FROM [Measurement_Period]() AS [Measurement_Period]
														) AS [_sourceTable]
													) AS [op1]
												))
								END AS [_Result]
						FROM (
							SELECT [Measurement_Period].[low] AS [low]
								,[Measurement_Period].[hi] AS [hi]
								,[Measurement_Period].[lowClosed] AS [lowClosed]
								,[Measurement_Period].[hiClosed] AS [hiClosed]
							FROM [Measurement_Period]() AS [Measurement_Period]
							) AS [_sourceTable]
						)
					)
				AND [FlexibleSigmoidoscopyConditions].[performedPeriod_end] <= (
					(
						SELECT [_sourceTable].[hi] AS [_Result]
						FROM (
							SELECT [Measurement_Period].[low] AS [low]
								,[Measurement_Period].[hi] AS [hi]
								,[Measurement_Period].[lowClosed] AS [lowClosed]
								,[Measurement_Period].[hiClosed] AS [hiClosed]
							FROM [Measurement_Period]() AS [Measurement_Period]
							) AS [_sourceTable]
						)
					)
				)
		)
GO

--
-- start Numerator
--
DROP FUNCTION Numerator
GO

CREATE FUNCTION Numerator ()
RETURNS TABLE
AS
RETURN

SELECT COUNT(DISTINCT [_sourceTable].[_Context]) AS [_Result]
FROM (
	(
		SELECT [FlexibleSigmoidoscopyPerformed].*
		FROM [FlexibleSigmoidoscopyPerformed]() AS [FlexibleSigmoidoscopyPerformed]
		)
	) AS [_sourceTable]
GO

--
-- start Denominator
--
DROP FUNCTION Denominator
GO

CREATE FUNCTION Denominator ()
RETURNS TABLE
AS
RETURN

SELECT COUNT(DISTINCT [_sourceTable].[_Context]) AS [_Result]
FROM (
	(
		SELECT [InInitialPopulation].*
		FROM [InInitialPopulation]() AS [InInitialPopulation]
		WHERE [_Result] = (
				(
					SELECT TOP 1 1 AS [_Result]
					)
				)
		)
	) AS [_sourceTable]
GO

--
-- start Percentage
--
DROP FUNCTION Percentage
GO

CREATE FUNCTION Percentage ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 (
				(
					(
						SELECT TOP 1 (
								(
									(
										SELECT TOP 1 CAST(([Numerator].[_Result]) AS DECIMAL) AS [_Result]
										FROM [Numerator]() AS [Numerator]
										)
									)
								) / (
								(
									(
										SELECT TOP 1 CAST(([Denominator].[_Result]) AS DECIMAL) AS [_Result]
										FROM [Denominator]() AS [Denominator]
										)
									)
								) AS [_Result]
						)
					)
				) * (
				(
					(
						SELECT TOP 1 CAST((100) AS DECIMAL) AS [_Result]
						)
					)
				) AS [_Result]
		)
GO

--
-- start AllPatients
--
DROP FUNCTION AllPatients
GO

CREATE FUNCTION AllPatients ()
RETURNS TABLE
AS
RETURN

SELECT [_sourceTable].*
FROM [patient] AS [_sourceTable]
GO

--
-- start AllPatientsCount
--
DROP FUNCTION AllPatientsCount
GO

CREATE FUNCTION AllPatientsCount ()
RETURNS TABLE
AS
RETURN

SELECT COUNT(1) AS [_Result]
FROM (
	(
		SELECT [AllPatients].*
		FROM [AllPatients]() AS [AllPatients]
		)
	) AS [_sourceTable]
GO

--
-- start AllPatientCountBoolean
--
DROP FUNCTION AllPatientCountBoolean
GO

CREATE FUNCTION AllPatientCountBoolean ()
RETURNS TABLE
AS
RETURN

SELECT IIF((
			(
				SELECT TOP 1 [AllPatientsCount].[_Result] AS [_Result]
				FROM [AllPatientsCount]() AS [AllPatientsCount]
				)
			) > (
			(
				SELECT TOP 1 5 AS [_Result]
				)
			), 1, 0) AS [_Result]
FROM [AllPatientsCount]() AS [AllPatientsCount]
GO

--
-- start AllPatientCountBoolean2
--
DROP FUNCTION AllPatientCountBoolean2
GO

CREATE FUNCTION AllPatientCountBoolean2 ()
RETURNS TABLE
AS
RETURN

SELECT IIF((
			(
				SELECT TOP 1 COUNT(1) AS [_Result]
				FROM (
					(
						SELECT [AllPatients].*
						FROM [AllPatients]() AS [AllPatients]
						)
					) AS [_sourceTable]
				)
			) > (
			(
				SELECT TOP 1 5 AS [_Result]
				)
			), 1, 0) AS [_Result]
FROM (
	(
		SELECT [AllPatients].*
		FROM [AllPatients]() AS [AllPatients]
		)
	) AS [_sourceTable]
GO

--
-- start ExplicitSingletonFrom
--
DROP FUNCTION ExplicitSingletonFrom
GO

CREATE FUNCTION ExplicitSingletonFrom ()
RETURNS TABLE
AS
RETURN

SELECT TOP 1 *
FROM (
	(
		SELECT [AllPatients].*
		FROM [AllPatients]() AS [AllPatients]
		)
	) AS [_UNUSED]
GO

--
-- start PatientBirthDateTest
--
DROP FUNCTION PatientBirthDateTest
GO

CREATE FUNCTION PatientBirthDateTest ()
RETURNS TABLE
AS
RETURN

SELECT [_sourceTable].*
FROM [patient] AS [_sourceTable]
WHERE [_sourceTable].[birthDate] > (
		(
			SELECT TOP 1 DATEFROMPARTS(1970, 1, 1) AS [_Result]
			)
		)
GO

--
-- start PatientCountBirthDateTest
--
DROP FUNCTION PatientCountBirthDateTest
GO

CREATE FUNCTION PatientCountBirthDateTest ()
RETURNS TABLE
AS
RETURN

SELECT COUNT(1) AS [_Result]
FROM (
	(
		SELECT [PatientBirthDateTest].*
		FROM [PatientBirthDateTest]() AS [PatientBirthDateTest]
		)
	) AS [_sourceTable]
GO

--
-- start PatientCountBirthDateTestWithFilter
--
DROP FUNCTION PatientCountBirthDateTestWithFilter
GO

CREATE FUNCTION PatientCountBirthDateTestWithFilter ()
RETURNS TABLE
AS
RETURN

SELECT COUNT(1) AS [_Result]
FROM (
	(
		SELECT [PatientBirthDateTest].*
		FROM [PatientBirthDateTest]() AS [PatientBirthDateTest]
		WHERE [PatientBirthDateTest].[birthDate] < (
				(
					SELECT TOP 1 DATEFROMPARTS(1971, 1, 1) AS [_Result]
					)
				)
		)
	) AS [_sourceTable]
GO

--
-- start StartOfTest
--
DROP FUNCTION StartOfTest
GO

CREATE FUNCTION StartOfTest ()
RETURNS TABLE
AS
RETURN

SELECT TOP 1 [_sourceTable].[low] AS [_Result]
FROM (
	SELECT TOP 1 [Measurement_Period].[low] AS [low]
		,[Measurement_Period].[hi] AS [hi]
		,[Measurement_Period].[lowClosed] AS [lowClosed]
		,[Measurement_Period].[hiClosed] AS [hiClosed]
	FROM [Measurement_Period]() AS [Measurement_Period]
	) AS [_sourceTable]
GO

--
-- start QuantityTest
--
DROP FUNCTION QuantityTest
GO

CREATE FUNCTION QuantityTest ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 5 AS [value]
			,'years' AS [units]
		)
GO

--
-- start DateMathTest
--
DROP FUNCTION DateMathTest
GO

CREATE FUNCTION DateMathTest ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 CASE (
					SELECT [op2].[units]
					FROM (
						(
							SELECT TOP 1 5 AS [value]
								,'years' AS [units]
							)
						) AS [op2]
					)
				WHEN 'years'
					THEN DATEADD(year, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT TOP 1 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									(
										SELECT TOP 1 DATEFROMPARTS(2020, 1, 1) AS [_Result]
										)
									) AS [op1]
								))
				WHEN 'months'
					THEN DATEADD(month, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT TOP 1 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									(
										SELECT TOP 1 DATEFROMPARTS(2020, 1, 1) AS [_Result]
										)
									) AS [op1]
								))
				WHEN 'weeks'
					THEN DATEADD(week, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT TOP 1 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									(
										SELECT TOP 1 DATEFROMPARTS(2020, 1, 1) AS [_Result]
										)
									) AS [op1]
								))
				WHEN 'days'
					THEN DATEADD(day, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT TOP 1 5 AS [value]
											,'years' AS [units]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									(
										SELECT TOP 1 DATEFROMPARTS(2020, 1, 1) AS [_Result]
										)
									) AS [op1]
								))
				END AS [_Result]
		)
GO

--
-- start ReferenceDateMathTest
--
DROP FUNCTION ReferenceDateMathTest
GO

CREATE FUNCTION ReferenceDateMathTest ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 CASE (
					SELECT [op2].[units]
					FROM (
						(
							SELECT TOP 1 [QuantityTest].[value] AS [value]
								,[QuantityTest].[units] AS [units]
							FROM [QuantityTest]() AS [QuantityTest]
							)
						) AS [op2]
					)
				WHEN 'years'
					THEN DATEADD(year, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT TOP 1 [QuantityTest].[value] AS [value]
											,[QuantityTest].[units] AS [units]
										FROM [QuantityTest]() AS [QuantityTest]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									(
										SELECT TOP 1 [StartOfTest].[_Result] AS [_Result]
										FROM [StartOfTest]() AS [StartOfTest]
										)
									) AS [op1]
								))
				WHEN 'months'
					THEN DATEADD(month, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT TOP 1 [QuantityTest].[value] AS [value]
											,[QuantityTest].[units] AS [units]
										FROM [QuantityTest]() AS [QuantityTest]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									(
										SELECT TOP 1 [StartOfTest].[_Result] AS [_Result]
										FROM [StartOfTest]() AS [StartOfTest]
										)
									) AS [op1]
								))
				WHEN 'weeks'
					THEN DATEADD(week, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT TOP 1 [QuantityTest].[value] AS [value]
											,[QuantityTest].[units] AS [units]
										FROM [QuantityTest]() AS [QuantityTest]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									(
										SELECT TOP 1 [StartOfTest].[_Result] AS [_Result]
										FROM [StartOfTest]() AS [StartOfTest]
										)
									) AS [op1]
								))
				WHEN 'days'
					THEN DATEADD(day, (
								SELECT - [op2].[value]
								FROM (
									(
										SELECT TOP 1 [QuantityTest].[value] AS [value]
											,[QuantityTest].[units] AS [units]
										FROM [QuantityTest]() AS [QuantityTest]
										)
									) AS [op2]
								), (
								SELECT [op1].[_Result]
								FROM (
									(
										SELECT TOP 1 [StartOfTest].[_Result] AS [_Result]
										FROM [StartOfTest]() AS [StartOfTest]
										)
									) AS [op1]
								))
				END AS [_Result]
		)
GO

--
-- start AgeInYearsTest
--
DROP FUNCTION AgeInYearsTest
GO

CREATE FUNCTION AgeInYearsTest ()
RETURNS TABLE
AS
RETURN

SELECT [Patient].[birthDate] AS [_Result]
FROM [Patient]() AS [Patient]
GO

--
-- start PatientContextRetrieveFilteredConditions
--
DROP FUNCTION PatientContextRetrieveFilteredConditions
GO

CREATE FUNCTION PatientContextRetrieveFilteredConditions ()
RETURNS TABLE
AS
RETURN

SELECT [_sourceTable].*
	,JSON_VALUE([_sourceTable].[subject_string], '$.id') AS [_Context]
FROM [condition] AS [_sourceTable]
WHERE [_sourceTable].[onsetDateTime] > DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7)
GO

--
-- start PatientContextConditionsCount
--
DROP FUNCTION PatientContextConditionsCount
GO

CREATE FUNCTION PatientContextConditionsCount ()
RETURNS TABLE
AS
RETURN

SELECT COUNT(1) AS [_Result]
	,[_sourceTable].[_Context]
FROM (
	(
		SELECT [PatientContextRetrieveFilteredConditions].*
		FROM [PatientContextRetrieveFilteredConditions]() AS [PatientContextRetrieveFilteredConditions]
		)
	) AS [_sourceTable]
GROUP BY [_sourceTable].[_Context]
GO

--
-- start PatientContextRetrieveReference
--
DROP FUNCTION PatientContextRetrieveReference
GO

CREATE FUNCTION PatientContextRetrieveReference ()
RETURNS TABLE
AS
RETURN (
		SELECT [PatientContextRetrieveFilteredConditions].*
		FROM [PatientContextRetrieveFilteredConditions]() AS [PatientContextRetrieveFilteredConditions]
		)
GO

--
-- start CountPatientsFlexibleSigmoidoscopyPerformed
--
DROP FUNCTION CountPatientsFlexibleSigmoidoscopyPerformed
GO

CREATE FUNCTION CountPatientsFlexibleSigmoidoscopyPerformed ()
RETURNS TABLE
AS
RETURN

SELECT COUNT(DISTINCT [_sourceTable].[_Context]) AS [_Result]
FROM (
	(
		SELECT [FlexibleSigmoidoscopyPerformed].*
		FROM [FlexibleSigmoidoscopyPerformed]() AS [FlexibleSigmoidoscopyPerformed]
		)
	) AS [_sourceTable]
GO

--
-- start CrossContextCountPatientsWithConditions
--
DROP FUNCTION CrossContextCountPatientsWithConditions
GO

CREATE FUNCTION CrossContextCountPatientsWithConditions ()
RETURNS TABLE
AS
RETURN

SELECT COUNT(DISTINCT [_sourceTable].[_Context]) AS [_Result]
FROM (
	(
		SELECT [PatientContextRetrieveFilteredConditions].*
		FROM [PatientContextRetrieveFilteredConditions]() AS [PatientContextRetrieveFilteredConditions]
		)
	) AS [_sourceTable]
GO

--
-- start FirstCompare
--
DROP FUNCTION FirstCompare
GO

CREATE FUNCTION FirstCompare ()
RETURNS TABLE
AS
RETURN

SELECT IIF((
			(
				SELECT TOP 1 1 AS [_Result]
				)
			) < (
			(
				SELECT TOP 1 2 AS [_Result]
				)
			), 1, 0) AS [_Result]
GO

--
-- start SecondCompare
--
DROP FUNCTION SecondCompare
GO

CREATE FUNCTION SecondCompare ()
RETURNS TABLE
AS
RETURN

SELECT IIF([FirstCompare].[_Result] = 1
		AND (
			(
				SELECT TOP 1 2 AS [_Result]
				)
			) < (
			(
				SELECT TOP 1 3 AS [_Result]
				)
			), 1, 0) AS [_Result]
FROM [FirstCompare]() AS [FirstCompare]
GO

--
-- start ThirdCompare
--
DROP FUNCTION ThirdCompare
GO

CREATE FUNCTION ThirdCompare ()
RETURNS TABLE
AS
RETURN

SELECT IIF([SecondCompare].[_Result] = 1
		OR (
			5 >= (
				(
					SELECT TOP 1 1 AS [_Result]
					)
				)
			AND 5 <= (
				(
					SELECT TOP 1 10 AS [_Result]
					)
				)
			), 1, 0) AS [_Result]
FROM [SecondCompare]() AS [SecondCompare]
GO

--
-- start FourthCompare
--
DROP FUNCTION FourthCompare
GO

CREATE FUNCTION FourthCompare ()
RETURNS TABLE
AS
RETURN

SELECT IIF([FirstCompare].[_Result] = 1
		AND [SecondCompare].[_Result] = 1
		AND [ThirdCompare].[_Result] = 1, 1, 0) AS [_Result]
FROM [FirstCompare]() AS [FirstCompare]
CROSS APPLY [SecondCompare]() AS [SecondCompare]
CROSS APPLY [ThirdCompare]() AS [ThirdCompare]
GO

--
-- start SimpleTrue
--
DROP FUNCTION SimpleTrue
GO

CREATE FUNCTION SimpleTrue ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 1 AS [_Result]
		)
GO

--
-- start SimpleFalse
--
DROP FUNCTION SimpleFalse
GO

CREATE FUNCTION SimpleFalse ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 0 AS [_Result]
		)
GO

--
-- start SimpleAnd
--
DROP FUNCTION SimpleAnd
GO

CREATE FUNCTION SimpleAnd ()
RETURNS TABLE
AS
RETURN

SELECT IIF(1 = 1
		AND 1 = 1, 1, 0) AS [_Result]
GO

--
-- start First
--
DROP FUNCTION First
GO

CREATE FUNCTION First ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 1 AS [_Result]
		)
GO

--
-- start Second
--
DROP FUNCTION Second
GO

CREATE FUNCTION Second ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 (
				(
					(
						SELECT TOP 1 1 AS [_Result]
						)
					)
				) + (
				(
					(
						SELECT TOP 1 1 AS [_Result]
						)
					)
				) AS [_Result]
		)
GO

--
-- start PEDMASTest
--
DROP FUNCTION PEDMASTest
GO

CREATE FUNCTION PEDMASTest ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 (
				(
					(
						SELECT TOP 1 (
								(
									(
										SELECT TOP 1 CAST((3) AS DECIMAL) AS [_Result]
										)
									)
								) + (
								(
									(
										SELECT TOP 1 4.0 AS [_Result]
										)
									)
								) AS [_Result]
						)
					)
				) / (
				(
					(
						SELECT TOP 1 CAST((
									(
										(
											(
												SELECT TOP 1 1 AS [_Result]
												)
											)
										) + (
										(
											(
												SELECT TOP 1 2 AS [_Result]
												)
											)
										)
									) AS DECIMAL) AS [_Result]
						)
					)
				) AS [_Result]
		)
GO

--
-- start CompoundMathTest
--
DROP FUNCTION CompoundMathTest
GO

CREATE FUNCTION CompoundMathTest ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 (
				(
					(
						SELECT TOP 1 CAST((1) AS DECIMAL) AS [_Result]
						)
					)
				) + (
				(
					(
						SELECT TOP 1 [PEDMASTest].[_Result] AS [_Result]
						FROM [PEDMASTest]() AS [PEDMASTest]
						)
					)
				) AS [_Result]
		)
GO

--
-- start MultipleCompoundMathTest
--
DROP FUNCTION MultipleCompoundMathTest
GO

CREATE FUNCTION MultipleCompoundMathTest ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 (
				(
					(
						SELECT TOP 1 [CompoundMathTest].[_Result] AS [_Result]
						FROM [CompoundMathTest]() AS [CompoundMathTest]
						)
					)
				) + (
				(
					(
						SELECT TOP 1 (
								(
									(
										SELECT TOP 1 [PEDMASTest].[_Result] AS [_Result]
										FROM [PEDMASTest]() AS [PEDMASTest]
										)
									)
								) * (
								(
									(
										SELECT TOP 1 CAST((2) AS DECIMAL) AS [_Result]
										)
									)
								) AS [_Result]
						)
					)
				) AS [_Result]
		)
GO

--
-- start SimpleRefTest
--
DROP FUNCTION SimpleRefTest
GO

CREATE FUNCTION SimpleRefTest ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 [MultipleCompoundMathTest].[_Result] AS [_Result]
		FROM [MultipleCompoundMathTest]() AS [MultipleCompoundMathTest]
		)
GO

--
-- start SimpleTest
--
DROP FUNCTION SimpleTest
GO

CREATE FUNCTION SimpleTest ()
RETURNS TABLE
AS
RETURN

SELECT [_sourceTable].*
FROM [condition] AS [_sourceTable]
GO

--
-- start CodeTest
--
DROP FUNCTION CodeTest
GO

CREATE FUNCTION CodeTest ()
RETURNS TABLE
AS
RETURN

SELECT [_sourceTable].*
FROM [condition] AS [_sourceTable]
INNER JOIN (
	SELECT TOP 1 code
		,codesystem
		,display
		,ver
	FROM [Hypertension]()
	) AS codeTable ON [_sourceTable].[code_coding_code] = [codeTable].[code]
	AND [_sourceTable].[code_coding_system] = [codeTable].[codesystem]
GO

--
-- start DateTest2
--
DROP FUNCTION DateTest2
GO

CREATE FUNCTION DateTest2 ()
RETURNS TABLE
AS
RETURN

SELECT [_sourceTable].*
FROM [condition] AS [_sourceTable]
WHERE [_sourceTable].[onsetDateTime] > (
		(
			SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS [_Result]
			)
		)
GO

--
-- start DateTest3
--
DROP FUNCTION DateTest3
GO

CREATE FUNCTION DateTest3 ()
RETURNS TABLE
AS
RETURN

SELECT [_sourceTable].*
FROM [condition] AS [_sourceTable]
WHERE [_sourceTable].[onsetDateTime] > (
		(
			SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS [_Result]
			)
		)
	AND [_sourceTable].[onsetDateTime] < (
		(
			SELECT TOP 1 DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS [_Result]
			)
		)
GO

--
-- start DateTest4
--
DROP FUNCTION DateTest4
GO

CREATE FUNCTION DateTest4 ()
RETURNS TABLE
AS
RETURN

SELECT [_sourceTable].*
FROM [condition] AS [_sourceTable]
INNER JOIN (
	SELECT TOP 1 code
		,codesystem
		,display
		,ver
	FROM [Hypertension]()
	) AS codeTable ON [_sourceTable].[code_coding_code] = [codeTable].[code]
	AND [_sourceTable].[code_coding_system] = [codeTable].[codesystem]
WHERE [_sourceTable].[onsetDateTime] > (
		(
			SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS [_Result]
			)
		)
	AND [_sourceTable].[onsetDateTime] < (
		(
			SELECT TOP 1 DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS [_Result]
			)
		)
GO

--
-- start IntervalDateDefinition
--
DROP FUNCTION IntervalDateDefinition
GO

CREATE FUNCTION IntervalDateDefinition ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS [low]
			,DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS [hi]
			,1 AS [lowClosed]
			,0 AS [hiClosed]
		)
GO

--
-- start IntervalIntegerDefinition
--
DROP FUNCTION IntervalIntegerDefinition
GO

CREATE FUNCTION IntervalIntegerDefinition ()
RETURNS TABLE
AS
RETURN (
		SELECT TOP 1 1 AS [low]
			,(
				(
					(
						SELECT TOP 1 10 AS [_Result]
						)
					)
				) * (
				(
					(
						SELECT TOP 1 3 AS [_Result]
						)
					)
				) AS [hi]
			,0 AS [lowClosed]
			,1 AS [hiClosed]
		)
GO

--
-- start IntervalIntegerReferenceTestDoesntWork
--
DROP FUNCTION IntervalIntegerReferenceTestDoesntWork
GO

CREATE FUNCTION IntervalIntegerReferenceTestDoesntWork ()
RETURNS TABLE
AS
RETURN

SELECT IIF((
			(
				(
					(
						(
							SELECT TOP 1 [IntervalIntegerDefinition].[lowClosed] AS [_Result]
							FROM [IntervalIntegerDefinition]() AS [IntervalIntegerDefinition]
							)
						) = 0
					AND 5 > (
						(
							SELECT TOP 1 [IntervalIntegerDefinition].[low] AS [_Result]
							FROM [IntervalIntegerDefinition]() AS [IntervalIntegerDefinition]
							)
						)
					)
				OR (
					(
						(
							SELECT TOP 1 [IntervalIntegerDefinition].[lowClosed] AS [_Result]
							FROM [IntervalIntegerDefinition]() AS [IntervalIntegerDefinition]
							)
						) = 1
					AND 5 >= (
						(
							SELECT TOP 1 [IntervalIntegerDefinition].[low] AS [_Result]
							FROM [IntervalIntegerDefinition]() AS [IntervalIntegerDefinition]
							)
						)
					)
				)
			AND (
				(
					(
						(
							SELECT TOP 1 [IntervalIntegerDefinition].[hiClosed] AS [_Result]
							FROM [IntervalIntegerDefinition]() AS [IntervalIntegerDefinition]
							)
						) = 0
					AND 5 < (
						(
							SELECT TOP 1 [IntervalIntegerDefinition].[hi] AS [_Result]
							FROM [IntervalIntegerDefinition]() AS [IntervalIntegerDefinition]
							)
						)
					)
				OR (
					(
						(
							SELECT TOP 1 [IntervalIntegerDefinition].[hiClosed] AS [_Result]
							FROM [IntervalIntegerDefinition]() AS [IntervalIntegerDefinition]
							)
						) = 1
					AND 5 <= (
						(
							SELECT TOP 1 [IntervalIntegerDefinition].[hi] AS [_Result]
							FROM [IntervalIntegerDefinition]() AS [IntervalIntegerDefinition]
							)
						)
					)
				)
			), 1, 0) AS [_Result]
FROM [IntervalIntegerDefinition]() AS [IntervalIntegerDefinition]
GO

--
-- start IntervalTest
--
DROP FUNCTION IntervalTest
GO

CREATE FUNCTION IntervalTest ()
RETURNS TABLE
AS
RETURN (
		SELECT [CodeTest].*
		FROM [CodeTest]() AS [CodeTest]
		WHERE (
				[CodeTest].[onsetDateTime] >= (
					(
						SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS [_Result]
						)
					)
				AND [CodeTest].[onsetDateTime] < (
					(
						SELECT TOP 1 DATETIME2FROMPARTS(2022, 2, 1, 0, 0, 0, 0, 7) AS [_Result]
						)
					)
				)
		)
GO

--
-- start FirstExists
--
DROP FUNCTION FirstExists
GO

CREATE FUNCTION FirstExists ()
RETURNS TABLE
AS
RETURN

SELECT IIF((
			SELECT COUNT(1)
			FROM (
				(
					SELECT [IntervalTest].*
					FROM [IntervalTest]() AS [IntervalTest]
					)
				) AS [_UNUSED]
			) > 0, 1, 0) AS [_Result]
FROM (
	SELECT NULL AS [_unused_column]
	) AS [_UNUSED]
GO

--
-- start SimpleRetrieveReferenceTest
--
DROP FUNCTION SimpleRetrieveReferenceTest
GO

CREATE FUNCTION SimpleRetrieveReferenceTest ()
RETURNS TABLE
AS
RETURN (
		SELECT [SimpleTest].*
		FROM [SimpleTest]() AS [SimpleTest]
		)
GO

--
-- start RetrieveReferenceWithFilterTest
--
DROP FUNCTION RetrieveReferenceWithFilterTest
GO

CREATE FUNCTION RetrieveReferenceWithFilterTest ()
RETURNS TABLE
AS
RETURN (
		SELECT [CodeTest].*
		FROM [CodeTest]() AS [CodeTest]
		WHERE [CodeTest].[onsetDateTime] > (
				(
					SELECT TOP 1 DATETIME2FROMPARTS(2020, 1, 1, 0, 0, 0, 0, 7) AS [_Result]
					)
				)
		)
GO

--
-- start MultipleNestedTest1
--
DROP FUNCTION MultipleNestedTest1
GO

CREATE FUNCTION MultipleNestedTest1 ()
RETURNS TABLE
AS
RETURN (
		SELECT [RetrieveReferenceWithFilterTest].*
		FROM [RetrieveReferenceWithFilterTest]() AS [RetrieveReferenceWithFilterTest]
		)
GO

--
-- start MultipleNestedTest2
--
DROP FUNCTION MultipleNestedTest2
GO

CREATE FUNCTION MultipleNestedTest2 ()
RETURNS TABLE
AS
RETURN (
		SELECT [MultipleNestedTest1].*
		FROM [MultipleNestedTest1]() AS [MultipleNestedTest1]
		WHERE [MultipleNestedTest1].[onsetDateTime] < (
				(
					SELECT TOP 1 DATETIME2FROMPARTS(2021, 1, 1, 0, 0, 0, 0, 7) AS [_Result]
					)
				)
		)
GO


