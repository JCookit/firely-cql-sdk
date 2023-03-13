﻿using Ncqa.Cql.Runtime.Primitives;
using System;
using System.Linq;

namespace Ncqa.Cql.Runtime.Comparers
{
    /// <summary>
    /// Compares the code and system using the specified comparers.
    /// </summary>
    public class CqlConceptCqlComparer : ICqlComparer<CqlConcept>, ICqlComparer
    {
        public CqlConceptCqlComparer(CqlComparers cqlComparers)
        {
            CqlComparers = cqlComparers;
        }

        public CqlComparers CqlComparers { get; }

        public int? Compare(object x, object y, string? precision = null) => Compare((CqlConcept)x, (CqlConcept)y, precision);
        public int? Compare(CqlConcept x, CqlConcept y, string? precision = null)
        {
            if (x == null || y == null || x.codes == null || y.codes == null)
                return null;
            var xCodes = x.codes.OrderBy(code => code.code)
                .ToArray();
            var yCodes = y.codes.OrderBy(code => code.code)
                .ToArray();
            if (xCodes.Length != yCodes.Length)
                return xCodes.Length - yCodes.Length;
            for(int i = 0; i < xCodes.Length; i++)
            {
                var xCode = xCodes[i];
                var yCode = yCodes[i];
                var compare = CqlComparers.Compare(xCode, yCode, precision);
                if (compare != 0)
                    return compare;
            }
            return 0;
        }

        public bool? Equals(object x, object y, string? precision = null) =>
            Equals((CqlConcept)x, (CqlConcept)y, precision);

        public bool? Equals(CqlConcept x, CqlConcept y, string? precision = null)
        {
            if (x == null || y == null)
                return null;
            else return Compare(x, y, precision) == 0;
        }

        public bool Equivalent(CqlConcept x, CqlConcept y, string? precision = null)
        {
            if (x == null || y == null || x.codes == null || y.codes == null)
                return false;
            var xCodes = x.codes.Select(code => code.code)
                .ToArray();
            var yCodes = y.codes.Select(code => code.code)
                .ToArray();
            
            for (int i = 0; i < xCodes.Length; i++)
            {
                var xCode = xCodes[i];
                var yCode = yCodes[i];
                var equivalent = CqlComparers.Equivalent(xCode!, yCode!, precision);
                if (equivalent)
                    return true;
            }
            return false;
        }

        public bool Equivalent(object x, object y, string? precision = null) =>
             Equivalent((x as CqlConcept)!, (y as CqlConcept)!, precision);

        public int GetHashCode(CqlConcept? x)
        {
            int baseCode = typeof(CqlConcept).GetHashCode();
            if (x == null || x.codes == null)
                return baseCode;
            foreach(var code in x.codes)
            {
                var codeHashCode = CqlComparers.GetHashCode(code);
                baseCode ^= codeHashCode;
            }
            return baseCode;
        }
        public int GetHashCode(object x) => GetHashCode(x as CqlConcept);

    }
}