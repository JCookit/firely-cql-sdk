library JeffCou1 version '1.0.0'

using FHIR version '4.0.1'

include FHIRHelpers version '4.0.001' called FHIRHelpers

codesystem "SnoMed" : 'http://brain.org'

code "Ouchie" : '59621000' from "SnoMed"

//define "Thing":
//  (3+4.0)/(1+2)

//define "Thing2":
//  1 + Thing

//define "Thing3":
//  Thing2 + Thing * 2

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

//define "DateIntervalTest":
//	[Condition] c
//		where 
//			(c.onset as FHIR.dateTime) after Interval[@2020-01-01T00:00:00.000Z, @2020-02-01T00:00:00.000Z]
