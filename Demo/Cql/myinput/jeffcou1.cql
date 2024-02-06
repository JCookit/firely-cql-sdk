library JeffCou1 version '1.0.0'

using FHIR version '4.0.1'

include FHIRHelpers version '4.0.001' called FHIRHelpers

//parameter "Measurement Period" Interval<DateTime>

valueset "Encounter Inpatient": 'http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113883.3.666.5.307'
valueset "Hypoglycemics Severe Hypoglycemia": 'http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113762.1.4.1196.393'
valueset "Observation Services": 'http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113762.1.4.1111.143'
valueset "Emergency Department Visit": 'http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113883.3.117.1.7.1.292'

// dev days example below
codesystem "ICD10": 'http://hl7.org/fhir/sid/icd-10'
code "Sucked into jet engine": 'V97.33' from "ICD10"
code "Sucked into jet engine, subsequent encounter": 'V97.33XD' from "ICD10"
//

codesystem "SnoMed" : 'http://snomed.info/sct'
code "Ouchie" : '59621000' from "SnoMed"

parameter "Measurement Period" Interval<DateTime>
  default Interval[@2019-01-01T00:00:00.0, @2020-01-01T00:00:00.0)

context Unfiltered

define "AllPatients":
  [Patient]

define "ExplicitSingletonFrom":
  singleton from "AllPatients"

define "PatientDateTest":
	[Patient] p
		where 
			(p.birthDate) after @1970-01-01

define "PatientCountTest":
	Count("PatientDateTest")

define "PatientCountTest2":
	Count("PatientDateTest" p where p.birthDate before @1971-01-01)


context Patient

define "AgeInYearsTest":
  Patient.birthDate

define "PatientContextRetrieve":
  [Condition] c
  		where 
			(c.onset) after @2020-01-01T00:00:00.000Z 

define "PatientContextRetrieveReference":
  "PatientContextRetrieve"

define "PatientContextRetrieveCount":
	Count("PatientContextRetrieve")

context Unfiltered

define "PatientConditionCountTest":
	Count("PatientContextRetrieve")

/* this requires joining patient table with the define result
define "Test":
   Count(from [Patient] p return "PatientContextRetrieve")
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
	[Condition: "Ouchie"] c

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
	[Condition: "Ouchie"] c
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


// test of cross-context querying

/*
context Patient

define "In Initial Population":
  AgeInYearsAt(@2013-01-01) >= 16

context Unfiltered

define "Initial Population Count":
  Count("In Initial Population" IP where IP is true)
*/


// test from DevDays

/*

define "Measurement Period":
   Interval[@2020-01-01T00:00:00.000Z, @2022-02-01T00:00:00.000Z)

context Patient


define "Jet engine conditions":
	[Condition] c
		where 
			exists(c.code.coding coding where coding ~ "Sucked into jet engine")
			and (c.onset as FHIR.dateTime) in "Measurement Period"

define "Subsequent encounters":
	[Condition] c
		where 
			exists(c.code.coding coding where coding ~ "Sucked into jet engine, subsequent encounter")
			and (c.onset as FHIR.dateTime) in "Measurement Period"


define "Initial population":
	exists("Jet engine conditions")

define "Numerator":
	exists("Subsequent encounters")

context Population

define "Initial population2":
	exists("Jet engine conditions")	
define "Numerator2":
	exists("Subsequent encounters")
	
	*/

// example taken from HospitalHarmSevereHypoglycemiaFHIR

/*
context Patient

define "Hypoglycemic Medication Administration":
  ["MedicationAdministration": "Hypoglycemics Severe Hypoglycemia"] HypoMedication
    where HypoMedication.status = 'completed'
      and HypoMedication.status != 'not-done'
	  
define "Inpatient Encounter During Measurement Period":
  [Encounter: "Encounter Inpatient"] EncounterInpatient
                		where EncounterInpatient.status = 'finished'
                		       			and EncounterInpatient.period ends during "Measurement Period"


define "Qualifying Encounter":
  "Inpatient Encounter During Measurement Period" InpatientEncounter
  where AgeInYearsAt (start of "HospitalizationWithObservation"(InpatientEncounter))>=18

define "Qualifying Encounter with Hypoglycemic Medication Administration":
  "Qualifying Encounter" QualifyingEncounter
                           with "Hypoglycemic Medication Administration" HypoglycemicMedication
                             such that "Normalize Interval"(HypoglycemicMedication.effective) starts during HospitalizationWithObservation ( QualifyingEncounter)

	
define function "HospitalizationWithObservation"(TheEncounter FHIR.Encounter ):
  TheEncounter Visit
  		let ObsVisit: Last([Encounter: "Observation Services"] LastObs
  				where LastObs.period ends 1 hour or less on or before start of Visit.period
  				sort by end of period
  			),
  			VisitStart: Coalesce(start of ObsVisit.period, start of Visit.period),
  			EDVisit: Last([Encounter: "Emergency Department Visit"] LastED
  				where LastED.period ends 1 hour or less on or before VisitStart
  				sort by end of period
  			)
  		return Interval[Coalesce(start of EDVisit.period, VisitStart), end of Visit.period]

define function "Normalize Interval"(choice Choice<FHIR.dateTime, FHIR.Period, FHIR.Timing, FHIR.instant, FHIR.string, FHIR.Age, FHIR.Range> ):
  case
  	  when choice is FHIR.dateTime then
  	Interval[FHIRHelpers.ToDateTime(choice as FHIR.dateTime), FHIRHelpers.ToDateTime(choice as FHIR.dateTime)]
  		when choice is FHIR.Period then
  		FHIRHelpers.ToInterval(choice as FHIR.Period)
  		when choice is FHIR.instant then
  			Interval[FHIRHelpers.ToDateTime(choice as FHIR.instant), FHIRHelpers.ToDateTime(choice as FHIR.instant)]
  		when choice is FHIR.Age then
  		  Interval[FHIRHelpers.ToDate(Patient.birthDate) + FHIRHelpers.ToQuantity(choice as FHIR.Age),
  			  FHIRHelpers.ToDate(Patient.birthDate) + FHIRHelpers.ToQuantity(choice as FHIR.Age) + 1 year)
  		when choice is FHIR.Range then
  		  Interval[FHIRHelpers.ToDate(Patient.birthDate) + FHIRHelpers.ToQuantity((choice as FHIR.Range).low),
  			  FHIRHelpers.ToDate(Patient.birthDate) + FHIRHelpers.ToQuantity((choice as FHIR.Range).high) + 1 year)
  		when choice is FHIR.Timing then
  		  Message(null as Interval<DateTime>, true, '1', 'Error', 'Cannot compute a single interval from a Timing type')
    when choice is FHIR.string then
      Message(null as Interval<DateTime>, true, '1', 'Error', 'Cannot compute an interval from a String value')
  		else
  			null as Interval<DateTime>
  	end
*/