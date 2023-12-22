library JeffCou1 version '1.0.0'

using FHIR version '4.0.1'

include FHIRHelpers version '4.0.001' called FHIRHelpers

//parameter "Measurement Period" Interval<DateTime>


// dev days example below
codesystem "ICD10": 'http://hl7.org/fhir/sid/icd-10'
code "Sucked into jet engine": 'V97.33' from "ICD10"
code "Sucked into jet engine, subsequent encounter": 'V97.33XD' from "ICD10"
//

codesystem "SnoMed" : 'http://snomed.info/sct'
code "Ouchie" : '59621000' from "SnoMed"

define "FirstCompare":
  1 < 2

define "SecondCompare":
  FirstCompare and 2 < 3

define "ThirdCompare":
  SecondCompare or 5 in Interval[1,10]

define "FourthCompare":
  FirstCompare and SecondCompare and ThirdCompare

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

define "IntervalTest":
	CodeTest c
		where 
			(c.onset) in Interval[@2020-01-01T00:00:00.000Z, @2022-02-01T00:00:00.000Z)

define "SimpleRetrieveReferenceTest":
	SimpleTest s

define "RetrieveReferenceWithFilterTest":
	CodeTest c where (c.onset) after @2020-01-01T00:00:00.000Z 

define "MultipleNestedTest1":
	RetrieveReferenceWithFilterTest

define "MultipleNestedTest2":
	MultipleNestedTest1 m where m.onset before @2021-01-01T00:00:00.000Z





// test from DevDays
/*
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
