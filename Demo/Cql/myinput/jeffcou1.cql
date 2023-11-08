library JeffCou1 version '1.0.0'

using FHIR version '4.0.1'

codesystem "ICD10": 'http://hl7.org/fhir/sid/icd-10'
code "Sucked into jet engine": 'V97.33' from "ICD10"
code "Sucked into jet engine, subsequent encounter": 'V97.33XD' from "ICD10"

define "Jet engine conditions":
	[Condition] c
		where c.onset <= '2015-01-01'


define "Thing":
  (3+4.0)/(1+2)

define "Thing2":
  1





