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

    internal Lazy<CqlCode> __Ouchie;
    internal Lazy<CqlCode[]> __SnoMed;
    internal Lazy<decimal?> __Thing;
    internal Lazy<decimal?> __Thing2;
    internal Lazy<decimal?> __Thing3;
    internal Lazy<decimal?> __Thing4;
    internal Lazy<IEnumerable<Condition>> __SimpleTest;
    internal Lazy<IEnumerable<Condition>> __CodeTest;
    internal Lazy<IEnumerable<Condition>> __DateTest2;
    internal Lazy<IEnumerable<Condition>> __DateTest3;
    internal Lazy<IEnumerable<Condition>> __DateTest4;
    internal Lazy<IEnumerable<Condition>> __DefinitionReferenceTest;

    #endregion
    public JeffCou1_1_0_0(CqlContext context)
    {
        this.context = context ?? throw new ArgumentNullException("context");

        FHIRHelpers_4_0_001 = new FHIRHelpers_4_0_001(context);

        __Ouchie = new Lazy<CqlCode>(this.Ouchie_Value);
        __SnoMed = new Lazy<CqlCode[]>(this.SnoMed_Value);
        __Thing = new Lazy<decimal?>(this.Thing_Value);
        __Thing2 = new Lazy<decimal?>(this.Thing2_Value);
        __Thing3 = new Lazy<decimal?>(this.Thing3_Value);
        __Thing4 = new Lazy<decimal?>(this.Thing4_Value);
        __SimpleTest = new Lazy<IEnumerable<Condition>>(this.SimpleTest_Value);
        __CodeTest = new Lazy<IEnumerable<Condition>>(this.CodeTest_Value);
        __DateTest2 = new Lazy<IEnumerable<Condition>>(this.DateTest2_Value);
        __DateTest3 = new Lazy<IEnumerable<Condition>>(this.DateTest3_Value);
        __DateTest4 = new Lazy<IEnumerable<Condition>>(this.DateTest4_Value);
        __DefinitionReferenceTest = new Lazy<IEnumerable<Condition>>(this.DefinitionReferenceTest_Value);
    }
    #region Dependencies

    public FHIRHelpers_4_0_001 FHIRHelpers_4_0_001 { get; }

    #endregion

	private CqlCode Ouchie_Value() => 
		new CqlCode("59621000", "http://brain.org", null, null);

    [CqlDeclaration("Ouchie")]
	public CqlCode Ouchie() => 
		__Ouchie.Value;

	private CqlCode[] SnoMed_Value()
	{
		var a_ = new CqlCode[]
		{
			new CqlCode("59621000", "http://brain.org", null, null),
		};

		return a_;
	}

    [CqlDeclaration("SnoMed")]
	public CqlCode[] SnoMed() => 
		__SnoMed.Value;

	private decimal? Thing_Value()
	{
		var a_ = context.Operators.ConvertIntegerToDecimal((int?)3);
		var b_ = context.Operators.Add(a_, (decimal?)4.0m);
		var c_ = context.Operators.Add((int?)1, (int?)2);
		var d_ = context.Operators.ConvertIntegerToDecimal(c_);
		var e_ = context.Operators.Divide(b_, d_);

		return e_;
	}

    [CqlDeclaration("Thing")]
	public decimal? Thing() => 
		__Thing.Value;

	private decimal? Thing2_Value()
	{
		var a_ = context.Operators.ConvertIntegerToDecimal((int?)1);
		var b_ = this.Thing();
		var c_ = context.Operators.Add(a_, b_);

		return c_;
	}

    [CqlDeclaration("Thing2")]
	public decimal? Thing2() => 
		__Thing2.Value;

	private decimal? Thing3_Value()
	{
		var a_ = this.Thing2();
		var b_ = this.Thing();
		var c_ = context.Operators.ConvertIntegerToDecimal((int?)2);
		var d_ = context.Operators.Multiply(b_, c_);
		var e_ = context.Operators.Add(a_, d_);

		return e_;
	}

    [CqlDeclaration("Thing3")]
	public decimal? Thing3() => 
		__Thing3.Value;

	private decimal? Thing4_Value()
	{
		var a_ = this.Thing3();

		return a_;
	}

    [CqlDeclaration("Thing4")]
	public decimal? Thing4() => 
		__Thing4.Value;

	private IEnumerable<Condition> SimpleTest_Value()
	{
		var a_ = context.Operators.RetrieveByValueSet<Condition>(null, null);

		return a_;
	}

    [CqlDeclaration("SimpleTest")]
	public IEnumerable<Condition> SimpleTest() => 
		__SimpleTest.Value;

	private IEnumerable<Condition> CodeTest_Value()
	{
		var a_ = this.Ouchie();
		var b_ = context.Operators.ToList<CqlCode>(a_);
		var c_ = context.Operators.RetrieveByCodes<Condition>(b_, null);

		return c_;
	}

    [CqlDeclaration("CodeTest")]
	public IEnumerable<Condition> CodeTest() => 
		__CodeTest.Value;

	private IEnumerable<Condition> DateTest2_Value()
	{
		var a_ = context.Operators.RetrieveByValueSet<Condition>(null, null);
		bool? b_(Condition c)
		{
			var d_ = FHIRHelpers_4_0_001.ToDateTime((c?.Onset as FhirDateTime));
			var e_ = context.Operators.DateTime((int?)2020, (int?)1, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
			var f_ = context.Operators.After(d_, e_, null);

			return f_;
		};
		var c_ = context.Operators.WhereOrNull<Condition>(a_, b_);

		return c_;
	}

    [CqlDeclaration("DateTest2")]
	public IEnumerable<Condition> DateTest2() => 
		__DateTest2.Value;

	private IEnumerable<Condition> DateTest3_Value()
	{
		var a_ = context.Operators.RetrieveByValueSet<Condition>(null, null);
		bool? b_(Condition c)
		{
			var d_ = FHIRHelpers_4_0_001.ToDateTime((c?.Onset as FhirDateTime));
			var e_ = context.Operators.DateTime((int?)2020, (int?)1, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
			var f_ = context.Operators.After(d_, e_, null);
			var h_ = context.Operators.DateTime((int?)2022, (int?)2, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
			var i_ = context.Operators.Before(d_, h_, null);
			var j_ = context.Operators.And(f_, i_);

			return j_;
		};
		var c_ = context.Operators.WhereOrNull<Condition>(a_, b_);

		return c_;
	}

    [CqlDeclaration("DateTest3")]
	public IEnumerable<Condition> DateTest3() => 
		__DateTest3.Value;

	private IEnumerable<Condition> DateTest4_Value()
	{
		var a_ = this.Ouchie();
		var b_ = context.Operators.ToList<CqlCode>(a_);
		var c_ = context.Operators.RetrieveByCodes<Condition>(b_, null);
		bool? d_(Condition c)
		{
			var f_ = FHIRHelpers_4_0_001.ToDateTime((c?.Onset as FhirDateTime));
			var g_ = context.Operators.DateTime((int?)2020, (int?)1, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
			var h_ = context.Operators.After(f_, g_, null);
			var j_ = context.Operators.DateTime((int?)2022, (int?)2, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
			var k_ = context.Operators.Before(f_, j_, null);
			var l_ = context.Operators.And(h_, k_);

			return l_;
		};
		var e_ = context.Operators.WhereOrNull<Condition>(c_, d_);

		return e_;
	}

    [CqlDeclaration("DateTest4")]
	public IEnumerable<Condition> DateTest4() => 
		__DateTest4.Value;

	private IEnumerable<Condition> DefinitionReferenceTest_Value()
	{
		var a_ = this.SimpleTest();
		bool? b_(Condition s)
		{
			var d_ = FHIRHelpers_4_0_001.ToDateTime((s?.Onset as FhirDateTime));
			var e_ = context.Operators.DateTime((int?)2020, (int?)1, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
			var f_ = context.Operators.After(d_, e_, null);

			return f_;
		};
		var c_ = context.Operators.WhereOrNull<Condition>(a_, b_);

		return c_;
	}

    [CqlDeclaration("DefinitionReferenceTest")]
	public IEnumerable<Condition> DefinitionReferenceTest() => 
		__DefinitionReferenceTest.Value;

}