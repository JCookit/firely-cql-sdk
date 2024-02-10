library JeffCou1 version '1.0.0'

using FHIR version '4.0.1'

include FHIRHelpers version '4.0.001' called FHIRHelpers

codesystem "SnoMed" : 'http://snomed.info/sct'

code "Flexible Sigmoidoscopy": '73761001' from "SnoMed"
code "Hypertension" : '59621000' from "SnoMed"

parameter "Measurement Period" Interval<DateTime>
  default Interval[@2019-01-01T00:00:00.0, @2020-01-01T00:00:00.0)

context Patient

define "InInitialPopulation":
  AgeInYearsAt((end of "Measurement Period") - 5 years) >= 50

define "FlexibleSigmoidoscopyPerformed":
	[Procedure: "Flexible Sigmoidoscopy"] FlexibleSigmoidoscopy
		where FlexibleSigmoidoscopy.status = 'completed'
			and FlexibleSigmoidoscopy.performed in Interval[(end of "Measurement Period") - 5 years, end of "Measurement Period"]

define "FlexibleSigmoidoscopyPerformed2":
	[Procedure: "Flexible Sigmoidoscopy"] FlexibleSigmoidoscopy
		with AgeInYearsAt(@2020-01-01) >= 50
		where FlexibleSigmoidoscopy.status = 'completed'
			and FlexibleSigmoidoscopy.performed in Interval[(end of "Measurement Period") - 5 years, end of "Measurement Period"]

context Unfiltered

define "Numerator":
  Count("FlexibleSigmoidoscopyPerformed")

define "Denominator":
  Count("InInitialPopulation" i where i = true)

define "Percentage":
  (Numerator / Denominator) * 100


//
// Additional tests
//

context Unfiltered

define "AllPatients":
  [Patient]

define "AllPatientsCount":
  Count("AllPatients")

define "AllPatientCountBoolean":
  "AllPatientsCount" > 5

define "AllPatientCountBoolean2":
  Count("AllPatients") > 5 

define "ExplicitSingletonFrom":
  singleton from "AllPatients"

define "PatientBirthDateTest":
	[Patient] p
		where 
			(p.birthDate) after @1970-01-01

define "PatientCountBirthDateTest":
	Count("PatientBirthDateTest")

define "PatientCountBirthDateTestWithFilter":
	Count("PatientBirthDateTest" p where p.birthDate before @1971-01-01)

define "StartOfTest":
  start of "Measurement Period"

define "QuantityTest":
  5 years

define "DateMathTest":
  @2020-01-01 - 5 years

define "ReferenceDateMathTest":
  StartOfTest - QuantityTest


// Patient context defines

context Patient

define "AgeInYearsTest":
  Patient.birthDate

define "PatientContextRetrieveFilteredConditions":
  [Condition] c
  		where 
			(c.onset) after @2020-01-01T00:00:00.000Z 

define "PatientContextConditionsCount":
  Count("PatientContextRetrieveFilteredConditions")

/* aggregate operations in a filtered context don't work yet 
define "PatientContextConditionsCountBoolean":
  Count("PatientContextRetrieveFilteredConditions") > 5

define "PatientContextStupidAdd":
  Count("PatientContextRetrieveFilteredConditions") + 5
  */

define "PatientContextRetrieveReference":
  "PatientContextRetrieveFilteredConditions"

// Unfiltered

context Unfiltered

define "CountPatientsFlexibleSigmoidoscopyPerformed":
  Count("FlexibleSigmoidoscopyPerformed")

define "CrossContextCountPatientsWithConditions":
	Count("PatientContextRetrieveFilteredConditions")

/*
define "ThisWontWorkProbablyEver":
    Count("PatientContextRetrieveFilteredConditions" pcr where Count(pcr) > 2)
 */


/* this requires joining patient table with the define result
define "Test":
   Count(from [Patient] p return "PatientContextRetrieveFilteredConditions")
*/

define "FirstCompare":
  1 < 2
  
define "SecondCompare":
  FirstCompare and 2 < 3

define "ThirdCompare":
  SecondCompare or 5 in Interval[1,10]

define "FourthCompare":
  FirstCompare and SecondCompare and ThirdCompare
  
define "SimpleTrue":
  true

define "SimpleFalse":
  false
  
define "SimpleAnd":
  true and true
  
define "First":
  1

define "Second":
  1+1

define "PEDMASTest":
  (3+4.0)/(1+2)

define "CompoundMathTest":
  1 + PEDMASTest

define "MultipleCompoundMathTest":
  CompoundMathTest + PEDMASTest * 2

define "SimpleRefTest":
  MultipleCompoundMathTest

define "SimpleTest":
	[Condition] c

define "CodeTest":
	[Condition: "Hypertension"] c

//define "DateTest":
//	[Condition] c
//		where 
//			(c.onset as FHIR.dateTime) after @2020-01-01T00:00:00.000Z

define "DateTest2":
	[Condition] c
		where 
			(c.onset) after @2020-01-01T00:00:00.000Z 

define "DateTest3":
	[Condition] c
		where 
			(c.onset) after @2020-01-01T00:00:00.000Z and (c.onset) before @2022-02-01T00:00:00.000Z

define "DateTest4":
	[Condition: "Hypertension"] c
		where 
			(c.onset) after @2020-01-01T00:00:00.000Z and (c.onset) before @2022-02-01T00:00:00.000Z

define "IntervalDateDefinition":
	Interval[@2020-01-01T00:00:00.000Z, @2022-02-01T00:00:00.000Z)

define "IntervalIntegerDefinition":
	Interval(1, 10*3]

define "IntervalIntegerReferenceTestDoesntWork":
    5 in IntervalIntegerDefinition

define "IntervalTest":
	CodeTest c
		where 
			(c.onset) in Interval[@2020-01-01T00:00:00.000Z, @2022-02-01T00:00:00.000Z)

define "FirstExists":
	exists("IntervalTest")

define "SimpleRetrieveReferenceTest":
	SimpleTest s

define "RetrieveReferenceWithFilterTest":
	CodeTest c where (c.onset) after @2020-01-01T00:00:00.000Z 

define "MultipleNestedTest1":
	RetrieveReferenceWithFilterTest

define "MultipleNestedTest2":
	MultipleNestedTest1 m where m.onset before @2021-01-01T00:00:00.000Z


