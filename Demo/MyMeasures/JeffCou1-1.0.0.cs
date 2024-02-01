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

    internal Lazy<CqlValueSet> __Encounter_Inpatient;
    internal Lazy<CqlValueSet> __Hypoglycemics_Severe_Hypoglycemia;
    internal Lazy<CqlValueSet> __Observation_Services;
    internal Lazy<CqlValueSet> __Emergency_Department_Visit;
    internal Lazy<CqlCode> __Sucked_into_jet_engine;
    internal Lazy<CqlCode> __Sucked_into_jet_engine__subsequent_encounter;
    internal Lazy<CqlCode> __Ouchie;
    internal Lazy<CqlCode[]> __ICD10;
    internal Lazy<CqlCode[]> __SnoMed;
    internal Lazy<CqlInterval<CqlDateTime>> __Measurement_Period;
    internal Lazy<bool?> __FirstCompare;
    internal Lazy<bool?> __SecondCompare;
    internal Lazy<bool?> __ThirdCompare;
    internal Lazy<bool?> __FourthCompare;
    internal Lazy<bool?> __SimpleTrue;
    internal Lazy<bool?> __SimpleFalse;
    internal Lazy<bool?> __SimpleAnd;
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
    internal Lazy<CqlInterval<CqlDateTime>> __IntervalDateDefinition;
    internal Lazy<CqlInterval<int?>> __IntervalIntegerDefinition;
    internal Lazy<bool?> __IntervalIntegerReferenceTestDoesntWork;
    internal Lazy<IEnumerable<Condition>> __IntervalTest;
    internal Lazy<bool?> __FirstExists;
    internal Lazy<IEnumerable<Condition>> __SimpleRetrieveReferenceTest;
    internal Lazy<IEnumerable<Condition>> __RetrieveReferenceWithFilterTest;
    internal Lazy<IEnumerable<Condition>> __MultipleNestedTest1;
    internal Lazy<IEnumerable<Condition>> __MultipleNestedTest2;
    internal Lazy<Patient> __Patient;
    internal Lazy<IEnumerable<MedicationAdministration>> __Hypoglycemic_Medication_Administration;
    internal Lazy<IEnumerable<Encounter>> __Inpatient_Encounter_During_Measurement_Period;
    internal Lazy<IEnumerable<Encounter>> __Qualifying_Encounter;
    internal Lazy<IEnumerable<Encounter>> __Qualifying_Encounter_with_Hypoglycemic_Medication_Administration;

    #endregion
    public JeffCou1_1_0_0(CqlContext context)
    {
        this.context = context ?? throw new ArgumentNullException("context");

        FHIRHelpers_4_0_001 = new FHIRHelpers_4_0_001(context);

        __Encounter_Inpatient = new Lazy<CqlValueSet>(this.Encounter_Inpatient_Value);
        __Hypoglycemics_Severe_Hypoglycemia = new Lazy<CqlValueSet>(this.Hypoglycemics_Severe_Hypoglycemia_Value);
        __Observation_Services = new Lazy<CqlValueSet>(this.Observation_Services_Value);
        __Emergency_Department_Visit = new Lazy<CqlValueSet>(this.Emergency_Department_Visit_Value);
        __Sucked_into_jet_engine = new Lazy<CqlCode>(this.Sucked_into_jet_engine_Value);
        __Sucked_into_jet_engine__subsequent_encounter = new Lazy<CqlCode>(this.Sucked_into_jet_engine__subsequent_encounter_Value);
        __Ouchie = new Lazy<CqlCode>(this.Ouchie_Value);
        __ICD10 = new Lazy<CqlCode[]>(this.ICD10_Value);
        __SnoMed = new Lazy<CqlCode[]>(this.SnoMed_Value);
        __Measurement_Period = new Lazy<CqlInterval<CqlDateTime>>(this.Measurement_Period_Value);
        __FirstCompare = new Lazy<bool?>(this.FirstCompare_Value);
        __SecondCompare = new Lazy<bool?>(this.SecondCompare_Value);
        __ThirdCompare = new Lazy<bool?>(this.ThirdCompare_Value);
        __FourthCompare = new Lazy<bool?>(this.FourthCompare_Value);
        __SimpleTrue = new Lazy<bool?>(this.SimpleTrue_Value);
        __SimpleFalse = new Lazy<bool?>(this.SimpleFalse_Value);
        __SimpleAnd = new Lazy<bool?>(this.SimpleAnd_Value);
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
        __IntervalDateDefinition = new Lazy<CqlInterval<CqlDateTime>>(this.IntervalDateDefinition_Value);
        __IntervalIntegerDefinition = new Lazy<CqlInterval<int?>>(this.IntervalIntegerDefinition_Value);
        __IntervalIntegerReferenceTestDoesntWork = new Lazy<bool?>(this.IntervalIntegerReferenceTestDoesntWork_Value);
        __IntervalTest = new Lazy<IEnumerable<Condition>>(this.IntervalTest_Value);
        __FirstExists = new Lazy<bool?>(this.FirstExists_Value);
        __SimpleRetrieveReferenceTest = new Lazy<IEnumerable<Condition>>(this.SimpleRetrieveReferenceTest_Value);
        __RetrieveReferenceWithFilterTest = new Lazy<IEnumerable<Condition>>(this.RetrieveReferenceWithFilterTest_Value);
        __MultipleNestedTest1 = new Lazy<IEnumerable<Condition>>(this.MultipleNestedTest1_Value);
        __MultipleNestedTest2 = new Lazy<IEnumerable<Condition>>(this.MultipleNestedTest2_Value);
        __Patient = new Lazy<Patient>(this.Patient_Value);
        __Hypoglycemic_Medication_Administration = new Lazy<IEnumerable<MedicationAdministration>>(this.Hypoglycemic_Medication_Administration_Value);
        __Inpatient_Encounter_During_Measurement_Period = new Lazy<IEnumerable<Encounter>>(this.Inpatient_Encounter_During_Measurement_Period_Value);
        __Qualifying_Encounter = new Lazy<IEnumerable<Encounter>>(this.Qualifying_Encounter_Value);
        __Qualifying_Encounter_with_Hypoglycemic_Medication_Administration = new Lazy<IEnumerable<Encounter>>(this.Qualifying_Encounter_with_Hypoglycemic_Medication_Administration_Value);
    }
    #region Dependencies

    public FHIRHelpers_4_0_001 FHIRHelpers_4_0_001 { get; }

    #endregion

	private CqlValueSet Encounter_Inpatient_Value() => 
		new CqlValueSet("http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113883.3.666.5.307", null);

    [CqlDeclaration("Encounter Inpatient")]
    [CqlValueSet("http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113883.3.666.5.307")]
	public CqlValueSet Encounter_Inpatient() => 
		__Encounter_Inpatient.Value;

	private CqlValueSet Hypoglycemics_Severe_Hypoglycemia_Value() => 
		new CqlValueSet("http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113762.1.4.1196.393", null);

    [CqlDeclaration("Hypoglycemics Severe Hypoglycemia")]
    [CqlValueSet("http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113762.1.4.1196.393")]
	public CqlValueSet Hypoglycemics_Severe_Hypoglycemia() => 
		__Hypoglycemics_Severe_Hypoglycemia.Value;

	private CqlValueSet Observation_Services_Value() => 
		new CqlValueSet("http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113762.1.4.1111.143", null);

    [CqlDeclaration("Observation Services")]
    [CqlValueSet("http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113762.1.4.1111.143")]
	public CqlValueSet Observation_Services() => 
		__Observation_Services.Value;

	private CqlValueSet Emergency_Department_Visit_Value() => 
		new CqlValueSet("http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113883.3.117.1.7.1.292", null);

    [CqlDeclaration("Emergency Department Visit")]
    [CqlValueSet("http://cts.nlm.nih.gov/fhir/ValueSet/2.16.840.1.113883.3.117.1.7.1.292")]
	public CqlValueSet Emergency_Department_Visit() => 
		__Emergency_Department_Visit.Value;

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

	private CqlInterval<CqlDateTime> Measurement_Period_Value()
	{
		var a_ = context.Operators.ConvertIntegerToDecimal(default);
		var b_ = context.Operators.DateTime((int?)2019, (int?)1, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, a_);
		var d_ = context.Operators.DateTime((int?)2020, (int?)1, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, a_);
		var e_ = context.Operators.Interval(b_, d_, true, false);
		var f_ = context.ResolveParameter("JeffCou1-1.0.0", "Measurement Period", e_);

		return (CqlInterval<CqlDateTime>)f_;
	}

    [CqlDeclaration("Measurement Period")]
	public CqlInterval<CqlDateTime> Measurement_Period() => 
		__Measurement_Period.Value;

	private bool? FirstCompare_Value()
	{
		var a_ = context.Operators.Less((int?)1, (int?)2);

		return a_;
	}

    [CqlDeclaration("FirstCompare")]
	public bool? FirstCompare() => 
		__FirstCompare.Value;

	private bool? SecondCompare_Value()
	{
		var a_ = this.FirstCompare();
		var b_ = context.Operators.Less((int?)2, (int?)3);
		var c_ = context.Operators.And(a_, b_);

		return c_;
	}

    [CqlDeclaration("SecondCompare")]
	public bool? SecondCompare() => 
		__SecondCompare.Value;

	private bool? ThirdCompare_Value()
	{
		var a_ = this.SecondCompare();
		var b_ = context.Operators.Interval((int?)1, (int?)10, true, true);
		var c_ = context.Operators.ElementInInterval<int?>((int?)5, b_, null);
		var d_ = context.Operators.Or(a_, c_);

		return d_;
	}

    [CqlDeclaration("ThirdCompare")]
	public bool? ThirdCompare() => 
		__ThirdCompare.Value;

	private bool? FourthCompare_Value()
	{
		var a_ = this.FirstCompare();
		var b_ = this.SecondCompare();
		var c_ = context.Operators.And(a_, b_);
		var d_ = this.ThirdCompare();
		var e_ = context.Operators.And(c_, d_);

		return e_;
	}

    [CqlDeclaration("FourthCompare")]
	public bool? FourthCompare() => 
		__FourthCompare.Value;

	private bool? SimpleTrue_Value() => 
		(bool?)true;

    [CqlDeclaration("SimpleTrue")]
	public bool? SimpleTrue() => 
		__SimpleTrue.Value;

	private bool? SimpleFalse_Value() => 
		(bool?)false;

    [CqlDeclaration("SimpleFalse")]
	public bool? SimpleFalse() => 
		__SimpleFalse.Value;

	private bool? SimpleAnd_Value()
	{
		var a_ = context.Operators.And((bool?)true, (bool?)true);

		return a_;
	}

    [CqlDeclaration("SimpleAnd")]
	public bool? SimpleAnd() => 
		__SimpleAnd.Value;

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

	private CqlInterval<CqlDateTime> IntervalDateDefinition_Value()
	{
		var a_ = context.Operators.DateTime((int?)2020, (int?)1, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
		var b_ = context.Operators.DateTime((int?)2022, (int?)2, (int?)1, (int?)0, (int?)0, (int?)0, (int?)0, (decimal?)0.0m);
		var c_ = context.Operators.Interval(a_, b_, true, false);

		return c_;
	}

    [CqlDeclaration("IntervalDateDefinition")]
	public CqlInterval<CqlDateTime> IntervalDateDefinition() => 
		__IntervalDateDefinition.Value;

	private CqlInterval<int?> IntervalIntegerDefinition_Value()
	{
		var a_ = context.Operators.Multiply((int?)10, (int?)3);
		var b_ = context.Operators.Interval((int?)1, a_, false, true);

		return b_;
	}

    [CqlDeclaration("IntervalIntegerDefinition")]
	public CqlInterval<int?> IntervalIntegerDefinition() => 
		__IntervalIntegerDefinition.Value;

	private bool? IntervalIntegerReferenceTestDoesntWork_Value()
	{
		var a_ = this.IntervalIntegerDefinition();
		var b_ = context.Operators.ElementInInterval<int?>((int?)5, a_, null);

		return b_;
	}

    [CqlDeclaration("IntervalIntegerReferenceTestDoesntWork")]
	public bool? IntervalIntegerReferenceTestDoesntWork() => 
		__IntervalIntegerReferenceTestDoesntWork.Value;

	private IEnumerable<Condition> IntervalTest_Value()
	{
		var a_ = this.CodeTest();
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

	private bool? FirstExists_Value()
	{
		var a_ = this.IntervalTest();
		var b_ = context.Operators.ExistsInList<Condition>(a_);

		return b_;
	}

    [CqlDeclaration("FirstExists")]
	public bool? FirstExists() => 
		__FirstExists.Value;

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

	private Patient Patient_Value()
	{
		var a_ = context.Operators.RetrieveByValueSet<Patient>(null, null);
		var b_ = context.Operators.SingleOrNull<Patient>(a_);

		return b_;
	}

    [CqlDeclaration("Patient")]
	public Patient Patient() => 
		__Patient.Value;

	private IEnumerable<MedicationAdministration> Hypoglycemic_Medication_Administration_Value()
	{
		var a_ = this.Hypoglycemics_Severe_Hypoglycemia();
		var b_ = context.Operators.RetrieveByValueSet<MedicationAdministration>(a_, null);
		var d_ = context.Operators.RetrieveByValueSet<MedicationAdministration>(a_, null);
		var e_ = context.Operators.ListUnion<MedicationAdministration>(b_, d_);
		bool? f_(MedicationAdministration HypoMedication)
		{
			var h_ = context.Operators.Convert<string>(HypoMedication?.StatusElement);
			var i_ = context.Operators.Equal(h_, "completed");
			var k_ = context.Operators.Equal(h_, "not-done");
			var l_ = context.Operators.Not(k_);
			var m_ = context.Operators.And(i_, l_);

			return m_;
		};
		var g_ = context.Operators.WhereOrNull<MedicationAdministration>(e_, f_);

		return g_;
	}

    [CqlDeclaration("Hypoglycemic Medication Administration")]
	public IEnumerable<MedicationAdministration> Hypoglycemic_Medication_Administration() => 
		__Hypoglycemic_Medication_Administration.Value;

	private IEnumerable<Encounter> Inpatient_Encounter_During_Measurement_Period_Value()
	{
		var a_ = this.Encounter_Inpatient();
		var b_ = context.Operators.RetrieveByValueSet<Encounter>(a_, null);
		bool? c_(Encounter EncounterInpatient)
		{
			var e_ = context.Operators.Convert<string>(EncounterInpatient?.StatusElement);
			var f_ = context.Operators.Equal(e_, "finished");
			var g_ = FHIRHelpers_4_0_001.ToInterval(EncounterInpatient?.Period);
			var h_ = context.Operators.End(g_);
			var i_ = this.Measurement_Period();
			var j_ = context.Operators.ElementInInterval<CqlDateTime>(h_, i_, null);
			var k_ = context.Operators.And(f_, j_);

			return k_;
		};
		var d_ = context.Operators.WhereOrNull<Encounter>(b_, c_);

		return d_;
	}

    [CqlDeclaration("Inpatient Encounter During Measurement Period")]
	public IEnumerable<Encounter> Inpatient_Encounter_During_Measurement_Period() => 
		__Inpatient_Encounter_During_Measurement_Period.Value;

    [CqlDeclaration("HospitalizationWithObservation")]
	public CqlInterval<CqlDateTime> HospitalizationWithObservation(Encounter TheEncounter)
	{
		var a_ = new Encounter[]
		{
			TheEncounter,
		};
		CqlInterval<CqlDateTime> b_(Encounter Visit)
		{
			var e_ = this.Emergency_Department_Visit();
			var f_ = context.Operators.RetrieveByValueSet<Encounter>(e_, null);
			bool? g_(Encounter LastED)
			{
				var ab_ = FHIRHelpers_4_0_001.ToInterval(LastED?.Period);
				var ac_ = context.Operators.End(ab_);
				var ad_ = this.Observation_Services();
				var ae_ = context.Operators.RetrieveByValueSet<Encounter>(ad_, null);
				bool? af_(Encounter LastObs)
				{
					var bq_ = FHIRHelpers_4_0_001.ToInterval(LastObs?.Period);
					var br_ = context.Operators.End(bq_);
					var bs_ = FHIRHelpers_4_0_001.ToInterval(Visit?.Period);
					var bt_ = context.Operators.Start(bs_);
					var bu_ = context.Operators.Quantity(1m, "hour");
					var bv_ = context.Operators.Subtract(bt_, bu_);
					var bx_ = context.Operators.Start(bs_);
					var by_ = context.Operators.Interval(bv_, bx_, true, true);
					var bz_ = context.Operators.ElementInInterval<CqlDateTime>(br_, by_, null);
					var cb_ = context.Operators.Start(bs_);
					var cc_ = context.Operators.Not((bool?)(cb_ is null));
					var cd_ = context.Operators.And(bz_, cc_);

					return cd_;
				};
				var ag_ = context.Operators.WhereOrNull<Encounter>(ae_, af_);
				object ah_(Encounter @this)
				{
					var ce_ = FHIRHelpers_4_0_001.ToInterval(@this?.Period);
					var cf_ = context.Operators.End(ce_);

					return cf_;
				};
				var ai_ = context.Operators.ListSortBy<Encounter>(ag_, ah_, System.ComponentModel.ListSortDirection.Ascending);
				var aj_ = context.Operators.LastOfList<Encounter>(ai_);
				var ak_ = FHIRHelpers_4_0_001.ToInterval(aj_?.Period);
				var al_ = context.Operators.Start(ak_);
				var am_ = FHIRHelpers_4_0_001.ToInterval(Visit?.Period);
				var an_ = context.Operators.Start(am_);
				var ao_ = context.Operators.Quantity(1m, "hour");
				var ap_ = context.Operators.Subtract((al_ ?? an_), ao_);
				var ar_ = context.Operators.RetrieveByValueSet<Encounter>(ad_, null);
				bool? as_(Encounter LastObs)
				{
					var cg_ = FHIRHelpers_4_0_001.ToInterval(LastObs?.Period);
					var ch_ = context.Operators.End(cg_);
					var ci_ = FHIRHelpers_4_0_001.ToInterval(Visit?.Period);
					var cj_ = context.Operators.Start(ci_);
					var ck_ = context.Operators.Quantity(1m, "hour");
					var cl_ = context.Operators.Subtract(cj_, ck_);
					var cn_ = context.Operators.Start(ci_);
					var co_ = context.Operators.Interval(cl_, cn_, true, true);
					var cp_ = context.Operators.ElementInInterval<CqlDateTime>(ch_, co_, null);
					var cr_ = context.Operators.Start(ci_);
					var cs_ = context.Operators.Not((bool?)(cr_ is null));
					var ct_ = context.Operators.And(cp_, cs_);

					return ct_;
				};
				var at_ = context.Operators.WhereOrNull<Encounter>(ar_, as_);
				object au_(Encounter @this)
				{
					var cu_ = FHIRHelpers_4_0_001.ToInterval(@this?.Period);
					var cv_ = context.Operators.End(cu_);

					return cv_;
				};
				var av_ = context.Operators.ListSortBy<Encounter>(at_, au_, System.ComponentModel.ListSortDirection.Ascending);
				var aw_ = context.Operators.LastOfList<Encounter>(av_);
				var ax_ = FHIRHelpers_4_0_001.ToInterval(aw_?.Period);
				var ay_ = context.Operators.Start(ax_);
				var ba_ = context.Operators.Start(am_);
				var bb_ = context.Operators.Interval(ap_, (ay_ ?? ba_), true, true);
				var bc_ = context.Operators.ElementInInterval<CqlDateTime>(ac_, bb_, null);
				var be_ = context.Operators.RetrieveByValueSet<Encounter>(ad_, null);
				bool? bf_(Encounter LastObs)
				{
					var cw_ = FHIRHelpers_4_0_001.ToInterval(LastObs?.Period);
					var cx_ = context.Operators.End(cw_);
					var cy_ = FHIRHelpers_4_0_001.ToInterval(Visit?.Period);
					var cz_ = context.Operators.Start(cy_);
					var da_ = context.Operators.Quantity(1m, "hour");
					var db_ = context.Operators.Subtract(cz_, da_);
					var dd_ = context.Operators.Start(cy_);
					var de_ = context.Operators.Interval(db_, dd_, true, true);
					var df_ = context.Operators.ElementInInterval<CqlDateTime>(cx_, de_, null);
					var dh_ = context.Operators.Start(cy_);
					var di_ = context.Operators.Not((bool?)(dh_ is null));
					var dj_ = context.Operators.And(df_, di_);

					return dj_;
				};
				var bg_ = context.Operators.WhereOrNull<Encounter>(be_, bf_);
				object bh_(Encounter @this)
				{
					var dk_ = FHIRHelpers_4_0_001.ToInterval(@this?.Period);
					var dl_ = context.Operators.End(dk_);

					return dl_;
				};
				var bi_ = context.Operators.ListSortBy<Encounter>(bg_, bh_, System.ComponentModel.ListSortDirection.Ascending);
				var bj_ = context.Operators.LastOfList<Encounter>(bi_);
				var bk_ = FHIRHelpers_4_0_001.ToInterval(bj_?.Period);
				var bl_ = context.Operators.Start(bk_);
				var bn_ = context.Operators.Start(am_);
				var bo_ = context.Operators.Not((bool?)((bl_ ?? bn_) is null));
				var bp_ = context.Operators.And(bc_, bo_);

				return bp_;
			};
			var h_ = context.Operators.WhereOrNull<Encounter>(f_, g_);
			object i_(Encounter @this)
			{
				var dm_ = FHIRHelpers_4_0_001.ToInterval(@this?.Period);
				var dn_ = context.Operators.End(dm_);

				return dn_;
			};
			var j_ = context.Operators.ListSortBy<Encounter>(h_, i_, System.ComponentModel.ListSortDirection.Ascending);
			var k_ = context.Operators.LastOfList<Encounter>(j_);
			var l_ = FHIRHelpers_4_0_001.ToInterval(k_?.Period);
			var m_ = context.Operators.Start(l_);
			var n_ = this.Observation_Services();
			var o_ = context.Operators.RetrieveByValueSet<Encounter>(n_, null);
			bool? p_(Encounter LastObs)
			{
				var do_ = FHIRHelpers_4_0_001.ToInterval(LastObs?.Period);
				var dp_ = context.Operators.End(do_);
				var dq_ = FHIRHelpers_4_0_001.ToInterval(Visit?.Period);
				var dr_ = context.Operators.Start(dq_);
				var ds_ = context.Operators.Quantity(1m, "hour");
				var dt_ = context.Operators.Subtract(dr_, ds_);
				var dv_ = context.Operators.Start(dq_);
				var dw_ = context.Operators.Interval(dt_, dv_, true, true);
				var dx_ = context.Operators.ElementInInterval<CqlDateTime>(dp_, dw_, null);
				var dz_ = context.Operators.Start(dq_);
				var ea_ = context.Operators.Not((bool?)(dz_ is null));
				var eb_ = context.Operators.And(dx_, ea_);

				return eb_;
			};
			var q_ = context.Operators.WhereOrNull<Encounter>(o_, p_);
			object r_(Encounter @this)
			{
				var ec_ = FHIRHelpers_4_0_001.ToInterval(@this?.Period);
				var ed_ = context.Operators.End(ec_);

				return ed_;
			};
			var s_ = context.Operators.ListSortBy<Encounter>(q_, r_, System.ComponentModel.ListSortDirection.Ascending);
			var t_ = context.Operators.LastOfList<Encounter>(s_);
			var u_ = FHIRHelpers_4_0_001.ToInterval(t_?.Period);
			var v_ = context.Operators.Start(u_);
			var w_ = FHIRHelpers_4_0_001.ToInterval(Visit?.Period);
			var x_ = context.Operators.Start(w_);
			var z_ = context.Operators.End(w_);
			var aa_ = context.Operators.Interval((m_ ?? (v_ ?? x_)), z_, true, true);

			return aa_;
		};
		var c_ = context.Operators.SelectOrNull<Encounter, CqlInterval<CqlDateTime>>(a_, b_);
		var d_ = context.Operators.SingleOrNull<CqlInterval<CqlDateTime>>(c_);

		return d_;
	}

	private IEnumerable<Encounter> Qualifying_Encounter_Value()
	{
		var a_ = this.Inpatient_Encounter_During_Measurement_Period();
		bool? b_(Encounter InpatientEncounter)
		{
			var d_ = this.Patient();
			var e_ = context.Operators.ConvertStringToDateTime(d_?.BirthDateElement?.Value);
			var f_ = this.HospitalizationWithObservation(InpatientEncounter);
			var g_ = context.Operators.Start(f_);
			var h_ = context.Operators.CalculateAgeAt(e_, g_, "year");
			var i_ = context.Operators.GreaterOrEqual(h_, (int?)18);

			return i_;
		};
		var c_ = context.Operators.WhereOrNull<Encounter>(a_, b_);

		return c_;
	}

    [CqlDeclaration("Qualifying Encounter")]
	public IEnumerable<Encounter> Qualifying_Encounter() => 
		__Qualifying_Encounter.Value;

    [CqlDeclaration("Normalize Interval")]
	public CqlInterval<CqlDateTime> Normalize_Interval(object choice)
	{
		CqlInterval<CqlDateTime> a_()
		{
			if (choice is FhirDateTime)
			{
				var b_ = FHIRHelpers_4_0_001.ToDateTime((choice as FhirDateTime));
				var d_ = context.Operators.Interval(b_, b_, true, true);

				return d_;
			}
			else if (choice is Period)
			{
				var e_ = FHIRHelpers_4_0_001.ToInterval((choice as Period));

				return e_;
			}
			else if (choice is Instant)
			{
				var f_ = FHIRHelpers_4_0_001.ToDateTime((choice as Instant));
				var h_ = context.Operators.Interval(f_, f_, true, true);

				return h_;
			}
			else if (choice is Age)
			{
				var i_ = this.Patient();
				var j_ = FHIRHelpers_4_0_001.ToDate(i_?.BirthDateElement);
				var k_ = FHIRHelpers_4_0_001.ToQuantity((choice as Age));
				var l_ = context.Operators.Add(j_, k_);
				var n_ = FHIRHelpers_4_0_001.ToDate(i_?.BirthDateElement);
				var p_ = context.Operators.Add(n_, k_);
				var q_ = context.Operators.Quantity(1m, "year");
				var r_ = context.Operators.Add(p_, q_);
				var s_ = context.Operators.Interval(l_, r_, true, false);
				var t_ = context.Operators.ConvertDateToDateTime(s_?.low);
				var v_ = FHIRHelpers_4_0_001.ToDate(i_?.BirthDateElement);
				var x_ = context.Operators.Add(v_, k_);
				var z_ = FHIRHelpers_4_0_001.ToDate(i_?.BirthDateElement);
				var ab_ = context.Operators.Add(z_, k_);
				var ad_ = context.Operators.Add(ab_, q_);
				var ae_ = context.Operators.Interval(x_, ad_, true, false);
				var af_ = context.Operators.ConvertDateToDateTime(ae_?.high);
				var ah_ = FHIRHelpers_4_0_001.ToDate(i_?.BirthDateElement);
				var aj_ = context.Operators.Add(ah_, k_);
				var al_ = FHIRHelpers_4_0_001.ToDate(i_?.BirthDateElement);
				var an_ = context.Operators.Add(al_, k_);
				var ap_ = context.Operators.Add(an_, q_);
				var aq_ = context.Operators.Interval(aj_, ap_, true, false);
				var as_ = FHIRHelpers_4_0_001.ToDate(i_?.BirthDateElement);
				var au_ = context.Operators.Add(as_, k_);
				var aw_ = FHIRHelpers_4_0_001.ToDate(i_?.BirthDateElement);
				var ay_ = context.Operators.Add(aw_, k_);
				var ba_ = context.Operators.Add(ay_, q_);
				var bb_ = context.Operators.Interval(au_, ba_, true, false);
				var bc_ = context.Operators.Interval(t_, af_, aq_?.lowClosed, bb_?.highClosed);

				return bc_;
			}
			else if (choice is Range)
			{
				var bd_ = this.Patient();
				var be_ = FHIRHelpers_4_0_001.ToDate(bd_?.BirthDateElement);
				var bf_ = FHIRHelpers_4_0_001.ToQuantity((choice as Range)?.Low);
				var bg_ = context.Operators.Add(be_, bf_);
				var bi_ = FHIRHelpers_4_0_001.ToDate(bd_?.BirthDateElement);
				var bj_ = FHIRHelpers_4_0_001.ToQuantity((choice as Range)?.High);
				var bk_ = context.Operators.Add(bi_, bj_);
				var bl_ = context.Operators.Quantity(1m, "year");
				var bm_ = context.Operators.Add(bk_, bl_);
				var bn_ = context.Operators.Interval(bg_, bm_, true, false);
				var bo_ = context.Operators.ConvertDateToDateTime(bn_?.low);
				var bq_ = FHIRHelpers_4_0_001.ToDate(bd_?.BirthDateElement);
				var bs_ = context.Operators.Add(bq_, bf_);
				var bu_ = FHIRHelpers_4_0_001.ToDate(bd_?.BirthDateElement);
				var bw_ = context.Operators.Add(bu_, bj_);
				var by_ = context.Operators.Add(bw_, bl_);
				var bz_ = context.Operators.Interval(bs_, by_, true, false);
				var ca_ = context.Operators.ConvertDateToDateTime(bz_?.high);
				var cc_ = FHIRHelpers_4_0_001.ToDate(bd_?.BirthDateElement);
				var ce_ = context.Operators.Add(cc_, bf_);
				var cg_ = FHIRHelpers_4_0_001.ToDate(bd_?.BirthDateElement);
				var ci_ = context.Operators.Add(cg_, bj_);
				var ck_ = context.Operators.Add(ci_, bl_);
				var cl_ = context.Operators.Interval(ce_, ck_, true, false);
				var cn_ = FHIRHelpers_4_0_001.ToDate(bd_?.BirthDateElement);
				var cp_ = context.Operators.Add(cn_, bf_);
				var cr_ = FHIRHelpers_4_0_001.ToDate(bd_?.BirthDateElement);
				var ct_ = context.Operators.Add(cr_, bj_);
				var cv_ = context.Operators.Add(ct_, bl_);
				var cw_ = context.Operators.Interval(cp_, cv_, true, false);
				var cx_ = context.Operators.Interval(bo_, ca_, cl_?.lowClosed, cw_?.highClosed);

				return cx_;
			}
			else if (choice is Timing)
			{
				CqlInterval<CqlDateTime> cy_ = null;
				var cz_ = context.Operators.Message<CqlInterval<CqlDateTime>>((cy_ as CqlInterval<CqlDateTime>), "1", "Error", "Cannot compute a single interval from a Timing type");

				return cz_;
			}
			else if (choice is FhirString)
			{
				CqlInterval<CqlDateTime> da_ = null;
				var db_ = context.Operators.Message<CqlInterval<CqlDateTime>>((da_ as CqlInterval<CqlDateTime>), "1", "Error", "Cannot compute an interval from a String value");

				return db_;
			}
			else
			{
				CqlInterval<CqlDateTime> dc_ = null;

				return (dc_ as CqlInterval<CqlDateTime>);
			};
		};

		return a_();
	}

	private IEnumerable<Encounter> Qualifying_Encounter_with_Hypoglycemic_Medication_Administration_Value()
	{
		var a_ = this.Qualifying_Encounter();
		IEnumerable<Encounter> b_(Encounter QualifyingEncounter)
		{
			var d_ = this.Hypoglycemic_Medication_Administration();
			bool? e_(MedicationAdministration HypoglycemicMedication)
			{
				var i_ = this.Normalize_Interval(HypoglycemicMedication?.Effective);
				var j_ = context.Operators.Start(i_);
				var k_ = this.HospitalizationWithObservation(QualifyingEncounter);
				var l_ = context.Operators.ElementInInterval<CqlDateTime>(j_, k_, null);

				return l_;
			};
			var f_ = context.Operators.WhereOrNull<MedicationAdministration>(d_, e_);
			Encounter g_(MedicationAdministration HypoglycemicMedication) => 
				QualifyingEncounter;
			var h_ = context.Operators.SelectOrNull<MedicationAdministration, Encounter>(f_, g_);

			return h_;
		};
		var c_ = context.Operators.SelectManyOrNull<Encounter, Encounter>(a_, b_);

		return c_;
	}

    [CqlDeclaration("Qualifying Encounter with Hypoglycemic Medication Administration")]
	public IEnumerable<Encounter> Qualifying_Encounter_with_Hypoglycemic_Medication_Administration() => 
		__Qualifying_Encounter_with_Hypoglycemic_Medication_Administration.Value;

}