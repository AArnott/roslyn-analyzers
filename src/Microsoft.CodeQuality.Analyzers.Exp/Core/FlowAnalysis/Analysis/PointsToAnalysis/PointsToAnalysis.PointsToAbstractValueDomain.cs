﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Operations.DataFlow.PointsToAnalysis
{
    using PointsToAnalysisData = IDictionary<AnalysisEntity, PointsToAbstractValue>;

    internal partial class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToAnalysisData, PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        /// <summary>
        /// Abstract value domain for <see cref="PointsToAnalysis"/> to merge and compare <see cref="PointsToAbstractValue"/> values.
        /// </summary>
        private class PointsToAbstractValueDomain : AbstractValueDomain<PointsToAbstractValue>
        {
            public static PointsToAbstractValueDomain Default = new PointsToAbstractValueDomain();
            private readonly SetAbstractDomain<AbstractLocation> _locationsDomain = new SetAbstractDomain<AbstractLocation>();

            private PointsToAbstractValueDomain() { }

            public override PointsToAbstractValue Bottom => PointsToAbstractValue.Undefined;

            public override PointsToAbstractValue UnknownOrMayBeValue => PointsToAbstractValue.Unknown;

            public override int Compare(PointsToAbstractValue oldValue, PointsToAbstractValue newValue)
            {
                Debug.Assert(oldValue != null);
                Debug.Assert(newValue != null);

                if (ReferenceEquals(oldValue, newValue) ||
                    oldValue.Kind.IsInvalidOrUndefined() ||
                    newValue.Kind.IsInvalidOrUndefined())
                {
                    return 0;
                }

                if (oldValue.Kind == newValue.Kind)
                {
                    int locationsCompareResult = _locationsDomain.Compare(oldValue.Locations, newValue.Locations);
                    var nullCompareResult = NullAbstractValueDomain.Default.Compare(oldValue.NullState, newValue.NullState);
                    if (locationsCompareResult > 0 || nullCompareResult > 0)
                    {
                        Debug.Fail("Non-monotonic Merge function");
                        return 1;
                    }
                    else if (locationsCompareResult < 0 || nullCompareResult < 0)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (oldValue.Kind < newValue.Kind)
                {
                    Debug.Assert(NullAbstractValueDomain.Default.Compare(oldValue.NullState, newValue.NullState) <= 0);
                    return -1;
                }
                else
                {
                    Debug.Fail("Non-monotonic Merge function");
                    return 1;
                }
            }

            public override PointsToAbstractValue Merge(PointsToAbstractValue value1, PointsToAbstractValue value2)
            {
                Debug.Assert(value1 != null);
                Debug.Assert(value2 != null);

                if (value1 == value2)
                {
                    return value1;
                }
                else if (value1.Kind.IsInvalidOrUndefined())
                {
                    return value2;
                }
                else if (value2.Kind.IsInvalidOrUndefined())
                {
                    return value1;
                }
                else if (value1.Kind == PointsToAbstractValueKind.Unknown ||
                    value2.Kind == PointsToAbstractValueKind.Unknown)
                {
                    return PointsToAbstractValue.Unknown;
                }

                var mergedLocations = _locationsDomain.Merge(value1.Locations, value2.Locations);
                var mergedNullState = NullAbstractValueDomain.Default.Merge(value1.NullState, value2.NullState);
                var result = PointsToAbstractValue.Create(mergedLocations, mergedNullState);

                Debug.Assert(Compare(value1, result) <= 0);
                Debug.Assert(Compare(value2, result) <= 0);

                return result;
            }
        }
    }
}
