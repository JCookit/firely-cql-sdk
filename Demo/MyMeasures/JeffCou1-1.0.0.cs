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
    internal Lazy<CqlCode> __Ouchie;
    internal Lazy<CqlCode[]> __ICD10;
    internal Lazy<CqlCode[]> __NumbersInMyBrain;
    internal Lazy<IEnumerable<Condition>> __Jet_engine_conditions2;
    internal Lazy<IEnumerable<Condition>> __Jet_engine_conditions;
    internal Lazy<IEnumerable<Condition>> __Ouch;

    #endregion
    public JeffCou1_1_0_0(CqlContext context)
    {
        this.context = context ?? throw new ArgumentNullException("context");


        __Sucked_into_jet_engine = new Lazy<CqlCode>(this.Sucked_into_jet_engine_Value);
        __Sucked_into_jet_engine__subsequent_encounter = new Lazy<CqlCode>(this.Sucked_into_jet_engine__subsequent_encounter_Value);
        __Ouchie = new Lazy<CqlCode>(this.Ouchie_Value);
        __ICD10 = new Lazy<CqlCode[]>(this.ICD10_Value);
        __NumbersInMyBrain = new Lazy<CqlCode[]>(this.NumbersInMyBrain_Value);
        __Jet_engine_conditions2 = new Lazy<IEnumerable<Condition>>(this.Jet_engine_conditions2_Value);
        __Jet_engine_conditions = new Lazy<IEnumerable<Condition>>(this.Jet_engine_conditions_Value);
        __Ouch = new Lazy<IEnumerable<Condition>>(this.Ouch_Value);
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

	private CqlCode Ouchie_Value() => 
		new CqlCode("59621000", "http://brain.org", null, null);

    [CqlDeclaration("Ouchie")]
	public CqlCode Ouchie() => 
		__Ouchie.Value;

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

	private CqlCode[] NumbersInMyBrain_Value()
	{
		var a_ = new CqlCode[]
		{
			new CqlCode("59621000", "http://brain.org", null, null),
		};

		return a_;
	}

    [CqlDeclaration("NumbersInMyBrain")]
	public CqlCode[] NumbersInMyBrain() => 
		__NumbersInMyBrain.Value;

	private IEnumerable<Condition> Jet_engine_conditions2_Value()
	{
		var a_ = context.Operators.RetrieveByValueSet<Condition>(null, null);

		return a_;
	}

    [CqlDeclaration("Jet engine conditions2")]
	public IEnumerable<Condition> Jet_engine_conditions2() => 
		__Jet_engine_conditions2.Value;

	private IEnumerable<Condition> Jet_engine_conditions_Value()
	{
		var a_ = this.Sucked_into_jet_engine();
		var b_ = context.Operators.ToList<CqlCode>(a_);
		var c_ = context.Operators.RetrieveByCodes<Condition>(b_, null);

		return c_;
	}

    [CqlDeclaration("Jet engine conditions")]
	public IEnumerable<Condition> Jet_engine_conditions() => 
		__Jet_engine_conditions.Value;

	private IEnumerable<Condition> Ouch_Value()
	{
		var a_ = this.Ouchie();
		var b_ = context.Operators.ToList<CqlCode>(a_);
		var c_ = context.Operators.RetrieveByCodes<Condition>(b_, null);

		return c_;
	}

    [CqlDeclaration("Ouch")]
	public IEnumerable<Condition> Ouch() => 
		__Ouch.Value;

}