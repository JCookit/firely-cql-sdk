﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Ncqa.Cql.Runtime.Primitives
{
    [CqlPrimitiveType(CqlPrimitiveType.Interval)]
    public class CqlInterval<T>
    {
        /// <summary>
        /// Returns a null point interval of this type.
        /// This instance is provided as a convenience for test cases, but should not be used otherwise.
        /// </summary>
        public static readonly CqlInterval<T> PointNull = new CqlInterval<T>();

        public CqlInterval(T low, T high, bool lowClosed, bool highClosed)
        {
            if (low == null && high == null)
                throw new ArgumentNullException(nameof(low), $"Intervals can have null values for either low or high, but not both.");
            this.low = low;
            this.high = high;
            this.lowClosed = lowClosed;
            this.highClosed = highClosed;
            String = new Lazy<string>(() => $"{(this.lowClosed ?? false ? "[" : "(")}{this.low}, {this.high}{(this.highClosed ?? false ? "]" : ")")}");
        }

        private CqlInterval()
        {
            low = (T)((object)null!);
            high = (T)((object)null!);
            lowClosed = false;
            highClosed = false;
            String = new Lazy<string>(() => $"[null, null]");
        }

        /// <summary>
        /// When the interval's high value is expressed as an expression, it could be null.  
        /// It is unclear how lexically this could occur, but ELM supports it as a construct.
        /// </summary>
        public CqlInterval(T low, T high, bool? lowClosed, bool? highClosed)
        {
            this.low = low;
            this.high = high;
            this.lowClosed = lowClosed ?? false;
            this.highClosed = highClosed ?? false;
            String = new Lazy<string>(() => $"{(this.lowClosed ?? false ? "[" : "(")}{this.low}, {this.high}{(this.highClosed ?? false ? "]" : ")")}");
        }


        public Type PointType => typeof(T);

        public T low { get; }
        public T high { get; }

        /// <summary>
        /// If <see langword="true"/>, include the low value in comparisons, else, exclude it (e.g., [x,y] is a closed interval)
        /// </summary>
        public bool? lowClosed { get; }

        /// <summary>
        /// If <see langword="true"/>, include the high value in comparisons, else, exclude it (e.g., [x,y] is a closed interval)
        /// </summary>
        public bool? highClosed { get; }

        private readonly Lazy<string> String;

        public override string? ToString() => String?.Value ?? "[]";

        public override int GetHashCode() => String?.Value?.GetHashCode() ?? 0;

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is CqlInterval<T> other)
            {
                return ToString() == other.ToString();
            }
            return false;
        }

    }

}