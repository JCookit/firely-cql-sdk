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

    internal Lazy<Patient> __Patient;
    internal Lazy<int?> __Thing;
    internal Lazy<int?> __OtherThing;
    internal Lazy<IEnumerable<Condition>> __Boogers;
    internal Lazy<IEnumerable<Condition>> __Boogers2;

    #endregion
    public JeffCou1_1_0_0(CqlContext context)
    {
        this.context = context ?? throw new ArgumentNullException("context");


        __Patient = new Lazy<Patient>(this.Patient_Value);
        __Thing = new Lazy<int?>(this.Thing_Value);
        __OtherThing = new Lazy<int?>(this.OtherThing_Value);
        __Boogers = new Lazy<IEnumerable<Condition>>(this.Boogers_Value);
        __Boogers2 = new Lazy<IEnumerable<Condition>>(this.Boogers2_Value);
    }
    #region Dependencies


    #endregion

	private Patient Patient_Value()
	{
		var a_ = context.Operators.RetrieveByValueSet<Patient>(null, null);
		var b_ = context.Operators.SingleOrNull<Patient>(a_);

		return b_;
	}

    [CqlDeclaration("Patient")]
	public Patient Patient() => 
		__Patient.Value;

	private int? Thing_Value()
	{
		var a_ = context.Operators.Add((int?)1, (int?)1);

		return a_;
	}

    [CqlDeclaration("Thing")]
	public int? Thing() => 
		__Thing.Value;

	private int? OtherThing_Value()
	{
		var a_ = this.Thing();
		var b_ = context.Operators.Add((int?)3, a_);

		return b_;
	}

    [CqlDeclaration("OtherThing")]
	public int? OtherThing() => 
		__OtherThing.Value;

	private IEnumerable<Condition> Boogers_Value()
	{
		var a_ = context.Operators.RetrieveByValueSet<Condition>(null, null);

		return a_;
	}

    [CqlDeclaration("Boogers")]
	public IEnumerable<Condition> Boogers() => 
		__Boogers.Value;

	private IEnumerable<Condition> Boogers2_Value()
	{
		var a_ = context.Operators.RetrieveByValueSet<Condition>(null, null);

		return a_;
	}

    [CqlDeclaration("Boogers2")]
	public IEnumerable<Condition> Boogers2() => 
		__Boogers2.Value;

}