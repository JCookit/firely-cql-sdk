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

    internal Lazy<decimal?> __Thing;
    internal Lazy<decimal?> __Thing2;
    internal Lazy<decimal?> __Thing3;

    #endregion
    public JeffCou1_1_0_0(CqlContext context)
    {
        this.context = context ?? throw new ArgumentNullException("context");


        __Thing = new Lazy<decimal?>(this.Thing_Value);
        __Thing2 = new Lazy<decimal?>(this.Thing2_Value);
        __Thing3 = new Lazy<decimal?>(this.Thing3_Value);
    }
    #region Dependencies


    #endregion

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

}