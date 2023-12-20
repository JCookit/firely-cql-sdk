library JeffCou1 version '1.0.0'

using FHIR version '4.0.1'

include FHIRHelpers version '4.0.001' called FHIRHelpers

codesystem "SnoMed" : 'http://snomed.info/sct'

code "Ouchie" : '59621000' from "SnoMed"

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

define "SimpleReferenceTest":
	SimpleTest s

define "DefinitionReferenceTest":
	SimpleTest s where (s.onset) after @2020-01-01T00:00:00.000Z 

//define "DateIntervalTest":
//	[Condition] c
//		where 
//			(c.onset as FHIR.dateTime) after Interval[@2020-01-01T00:00:00.000Z, @2020-02-01T00:00:00.000Z]
