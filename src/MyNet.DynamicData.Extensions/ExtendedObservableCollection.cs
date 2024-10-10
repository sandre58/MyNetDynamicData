// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using DynamicData;
using DynamicData.Binding;
using MyNet.Utilities.Collections;

namespace MyNet.DynamicData.Extensions
{
    public class ExtendedObservableCollection<T> : OptimizedObservableCollection<T>, IObservableCollection<T>, IExtendedList<T>
    {
        public ExtendedObservableCollection() : base() { }

        public ExtendedObservableCollection(List<T> list) : base(list) { }

        public ExtendedObservableCollection(IEnumerable<T> collection) : base(collection) { }
    }
}
