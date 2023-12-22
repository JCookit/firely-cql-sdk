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
    internal Lazy<CqlCode[]> __SnoMed;
    internal Lazy<int?> __First;
    internal Lazy<int?> __Second;
    internal Lazy<decimal?> __PEDMASTest;
    internal Lazy<decimal?> __CompoundMathTest;
    internal Lazy<decimal?> __MultipleCompoundMathTest;
    internal Lazy<decimal?> __SimpleRefTest;
    internal Lazy<IEnumerable<Condition>> __SimpleTest;
    internal Lazy<IEnumerable<Condition>> __CodeTest;
    internal Lazy<IEnumerable<Condition>> __DateTest2;
    internal Lazy<IEnumerable<Condition>> __DateTest3;
    internal Lazy<IEnumerable<Condition>> __DateTest4;
    internal Lazy<IEnumerable<Condition>> __IntervalTest;
    internal Lazy<IEnumerable<Condition>> __SimpleRetrieveReferenceTest;
    internal Lazy<IEnumerable<Condition>> __RetrieveReferenceWithFilterTest;
    internal Lazy<IEnumerable<Condition>> __MultipleNestedTest1;
    internal Lazy<IEnumerable<Condition>> __MultipleNestedTest2;

    #endregion
    public JeffCou1_1_0_0(CqlContext context)
    {
        this.context = context ?? throw new ArgumentNullException("context");

        FHIRHelpers_4_0_001 = new FHIRHelpers_4_0_001(context);

        __Sucked_into_jet_engine = new Lazy<CqlCode>(this.Sucked_into_jet_engine_Value);
        __Sucked_into_jet_engine__subsequent_encounter = new Lazy<CqlCode>(this.Sucked_into_jet_engine__subsequent_encounter_Value);
        __Ouchie = new Lazy<CqlCode>(this.Ouchie_Value);
        __ICD10 = new Lazy<CqlCode[]>(this.ICD10_Value);
        __SnoMed = new Lazy<CqlCode[]>(this.SnoMed_Value);
        __First = new Lazy<int?>(this.First_Value);
        __Second = new Lazy<int?>(this.Second_Value);
        __PEDMASTest = new Lazy<decimal?>(this.PEDMASTest_Value);
        __CompoundMathTest = new Lazy<decimal?>(this.CompoundMathTest_Value);
        __MultipleCompoundMathTest = new Lazy<decimal?>(this.MultipleCompoundMathTest_Value);
        __SimpleRefTest = new Lazy<decimal?>(this.SimpleRefTest_Value);
        __SimpleTest = new Lazy<IEnumerable<Condition>>(this.SimpleTest_Value);
        __CodeTest = new Lazy<IEnumerable<Condition>>(this.CodeTest_Value);
        __DateTest2 = new Lazy<IEnumerable<Condition>>(this.DateTest2_Value);
        __DateTest3 = new Lazy<IEnumerable<Condition>>(this.DateTest3_Value);
        __DateTest4 = new Lazy<IEnumerable<Condition>>(this.DateTest4_Value);
        __IntervalTest = new Lazy<IEnumerable<Condition>>(this.IntervalTest_Value);
        __SimpleRetrieveReferenceTest = new Lazy<IEnumerable<Condition>>(this.SimpleRetrieveReferenceTest_Value);
        __RetrieveReferenceWithFilterTest = new Lazy<IEnumerable<Condition>>(this.RetrieveReferenceWithFilterTest_Value);
        __MultipleNestedTest1 = new Lazy<IEnumerable<Condition>>(this.MultipleNestedTest1_Value);
        __MultipleNestedTest2 = new Lazy<IEnumerable<Condition>>(this.MultipleNestedTest2_Value);
    }
    #region Dependencies

    public FHIRHelpers_4_0_001 FHIRHelpers_4_0_001 { get; }

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
		new CqlCode("59621000", "http://snomed.info/sct", null, null);

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

	private CqlCode[] SnoMed_Value()
	{
		var a_ = new CqlCode[]
		{
			new CqlCode("59621000", "http://snomed.info/sct", null, null),
		};

		return a_;
	}

    [CqlDeclaration("SnoMed")]
	public CqlCode[] SnoMed() => 
		__SnoMed.Value;

	private int? First_Value() => 
		(int?)1;

    [CqlDeclaration("First")]
	public int? First() => 
		__First.Value;

	private int? Second_Value()
	{
		var a_ = context.Operators.Add((int?)1, (int?)1);

		return a_;
	}

    [CqlDeclaration("Second")]
	public int? Second() => 
		__Second.Value;

	private decimal? PEDMASTest_Value()
	{
		var a_ = context.Operators.ConvertIntegerToDecimal((int?)3);
		var b_ = context.Operators.Add(a_, (decimal?)4.0m);
		var c_ = context.Operators.Add((int?)1, (int?)2);
		var d_ = context.Operators.ConvertIntegerToDecimal(c_);
		var e_ = context.Operators.Divide(b_, d_);

		return e_;
	}

    [CqlDeclaration("PEDMASTest")]
	public decimal? PEDMASTest() => 
		__PEDMASTest.Value;

	private decimal? CompoundMathTest_Value()
	{
		var a_ = context.Operators.ConvertIntegerToDecimal((int?)1);
		var b_ = this.PEDMASTest();
		var c_ = context.Operators.Add(a_, b_);

		return c_;
	}

    [CqlDeclaration("CompoundMathTest")]
	public decimal? CompoundMathTest() => 
		__CompoundMathTest.Value;

	private decimal? MultipleCompoundMathTest_Value()
	{
		var a_ = this.CompoundMathTest();
		var b_ = this.PEDMASTest();
		var c_ = context.Operators.ConvertIntegerToDecimal((int?)2);
		var d_ = context.Operators.Multiply(b_, c_);
		var e_ = context.Operators.Add(a_, d_);

		return e_;
	}

    [CqlDeclaration("MultipleCompoundMathTest")]
	public decimal? MultipleCompoundMathTest() => 
		__MultipleCompoundMathTest.Value;

	private decimal? SimpleRefTest_Value()
	{
		var a_ = this.MultipleCompoundMathTest();

		return a_;
	}

    [CqlDeclaration("SimpleRefTest")]
	public decimal? SimpleRefTest() => 
		__SimpleRefTest.Value;

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

	private IEnumerable<Condition> IntervalTest_Value()
	{
		var a_ = context.Operators.RetrieveByValueSet<Condition>(null, null);
		bool? b_(Condition c)
		{
			var d_ = FHIRHelpers_4_0_001.ToDateTime((c?.Onset as FhirDateTime));
			var e_ = context.Operators.DateTime((int?)2020, (int?)1, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
			var f_ = context.Operators.DateTime((int?)2022, (int?)2, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
			var g_ = context.Operators.Interval(e_, f_, true, false);
			var h_ = context.Operators.ElementInInterval<CqlDateTime>(d_, g_, null);

			return h_;
		};
		var c_ = context.Operators.WhereOrNull<Condition>(a_, b_);

		return c_;
	}

    [CqlDeclaration("IntervalTest")]
	public IEnumerable<Condition> IntervalTest() => 
		__IntervalTest.Value;

	private IEnumerable<Condition> SimpleRetrieveReferenceTest_Value()
	{
		var a_ = this.SimpleTest();

		return a_;
	}

    [CqlDeclaration("SimpleRetrieveReferenceTest")]
	public IEnumerable<Condition> SimpleRetrieveReferenceTest() => 
		__SimpleRetrieveReferenceTest.Value;

	private IEnumerable<Condition> RetrieveReferenceWithFilterTest_Value()
	{
		var a_ = this.CodeTest();
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

    [CqlDeclaration("RetrieveReferenceWithFilterTest")]
	public IEnumerable<Condition> RetrieveReferenceWithFilterTest() => 
		__RetrieveReferenceWithFilterTest.Value;

	private IEnumerable<Condition> MultipleNestedTest1_Value()
	{
		var a_ = this.RetrieveReferenceWithFilterTest();

		return a_;
	}

    [CqlDeclaration("MultipleNestedTest1")]
	public IEnumerable<Condition> MultipleNestedTest1() => 
		__MultipleNestedTest1.Value;

	private IEnumerable<Condition> MultipleNestedTest2_Value()
	{
		var a_ = this.MultipleNestedTest1();
		bool? b_(Condition m)
		{
			var d_ = FHIRHelpers_4_0_001.ToDateTime((m?.Onset as FhirDateTime));
			var e_ = context.Operators.DateTime((int?)2021, (int?)1, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
			var f_ = context.Operators.Before(d_, e_, null);

			return f_;
		};
		var c_ = context.Operators.WhereOrNull<Condition>(a_, b_);

		return c_;
	}

    [CqlDeclaration("MultipleNestedTest2")]
	public IEnumerable<Condition> MultipleNestedTest2() => 
		__MultipleNestedTest2.Value;

}