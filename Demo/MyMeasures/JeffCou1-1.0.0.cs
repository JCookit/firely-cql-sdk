using System;
using System.Linq;
using System.Collections.Generic;
using Hl7.Cql.Runtime;
using Hl7.Cql.Primitives;
using Hl7.Cql.Abstractions;
using Hl7.Cql.ValueSets;
using Hl7.Cql.Iso8601;
using Hl7.Fhir.Model;
using Range = Hl7.Fhir.Model.Range;
using Task = Hl7.Fhir.Model.Task;
[System.CodeDom.Compiler.GeneratedCode(".NET Code Generation", "1.0.0.0")]
[CqlLibrary("JeffCou1", "1.0.0")]
public class JeffCou1_1_0_0
{


    internal CqlContext context;

    #region Cached values

    internal Lazy<CqlCode> __Sucked_into_jet_engine;
    internal Lazy<CqlCode> __Sucked_into_jet_engine__subsequent_encounter;
    internal Lazy<CqlCode[]> __ICD10;

    #endregion
    public JeffCou1_1_0_0(CqlContext context)
    {
        this.context = context ?? throw new ArgumentNullException("context");


        __Sucked_into_jet_engine = new Lazy<CqlCode>(this.Sucked_into_jet_engine_Value);
        __Sucked_into_jet_engine__subsequent_encounter = new Lazy<CqlCode>(this.Sucked_into_jet_engine__subsequent_encounter_Value);
        __ICD10 = new Lazy<CqlCode[]>(this.ICD10_Value);
    }
    #region Dependencies


    #endregion

	private CqlCode Sucked_into_jet_engine_Value() => 
		new CqlCode("V97.33", "http://hl7.org/fhir/sid/icd-10", null, null);

    [CqlDeclaration("Sucked into jet engine")]
	public CqlCode Sucked_into_jet_engine() => 
		__Sucked_into_jet_engine.Value;

	private CqlCode Sucked_into_jet_engine__subsequent_encounter_Value() => 
		new CqlCode("V97.33XD", "http://hl7.org/fhir/sid/icd-10", null, null);

    [CqlDeclaration("Sucked into jet engine, subsequent encounter")]
	public CqlCode Sucked_into_jet_engine__subsequent_encounter() => 
		__Sucked_into_jet_engine__subsequent_encounter.Value;

	private CqlCode[] ICD10_Value()
	{
		var a_ = new CqlCode[]
		{
			new CqlCode("V97.33", "http://hl7.org/fhir/sid/icd-10", null, null),
			new CqlCode("V97.33XD", "http://hl7.org/fhir/sid/icd-10", null, null),
		};

		return a_;
	}

    [CqlDeclaration("ICD10")]
	public CqlCode[] ICD10() => 
		__ICD10.Value;

}