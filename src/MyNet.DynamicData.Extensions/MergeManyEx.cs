// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;

namespace MyNet.DynamicData.Extensions
{
    internal sealed class MergeManyEx<T, TDestination>(IObservable<IChangeSet<T>> source,
               Func<T, IObservable<IChangeSet<TDestination>>> observableSelector) where T : notnull where TDestination : notnull
    {
        private readonly IObservable<IChangeSet<T>> _source = source ?? throw new ArgumentNullException(nameof(source));
        private readonly Func<T, IObservable<IChangeSet<TDestination>>> _observableSelector = observableSelector ?? throw new ArgumentNullException(nameof(observableSelector));

        private void ForwardWhenRemove(IObserver<IChangeSet<TDestination>> observer, T sourceItem)
        {
            var oblist = _observableSelector(sourceItem).AsObservableList();
            var changeset = new ChangeSet<TDestination>(
            [
                new Change<TDestination>(ListChangeReason.RemoveRange, oblist.Items)
            ]);
            oblist.Dispose();
            observer.OnNext(changeset);
        }

        public IObservable<IChangeSet<TDestination>> Run() => Observable.Create<IChangeSet<TDestination>>
              (
                observer =>
                {
                    var locker = new object();
                    return _source
                // SubscribeMany will forwards inital cascaded inner items when it observers new item,
                // but if one item was removed, any item belonged to it won't be forwarded to observer.
                .SubscribeMany(t => _observableSelector(t).Synchronize(locker).Subscribe(observer.OnNext))
                // So I add a observer here to subscribe all items belonged to the removed item, buildup a 
                // ChangeSet and and forward it to the original observer.
                .Subscribe(t =>
                    {
                        foreach (var x in t)
                        {
                            switch (x.Reason)
                            {
                                case ListChangeReason.RemoveRange:
                                    {
                                        foreach (var item in x.Range) ForwardWhenRemove(observer, item);
                                        break;
                                    }
                                case ListChangeReason.Remove:
                                    {
                                        ForwardWhenRemove(observer, x.Item.Current);
                                        break;
                                    }
                            }
                        }
                    }, observer.OnError);
                });
    }

    internal sealed class MergeManyEx<T, TKey, TDestination, TDestinationKey>(IObservable<IChangeSet<T, TKey>> source,
               Func<T, IObservable<IChangeSet<TDestination, TDestinationKey>>> observableSelector,
               Func<TDestination, TDestinationKey> observableKeySelector) where T : notnull where TDestination : notnull where TKey : notnull where TDestinationKey : notnull
    {
        private readonly IObservable<IChangeSet<T, TKey>> _source = source ?? throw new ArgumentNullException(nameof(source));
        private readonly Func<T, IObservable<IChangeSet<TDestination, TDestinationKey>>> _observableSelector = observableSelector ?? throw new ArgumentNullException(nameof(observableSelector));
        private readonly Func<TDestination, TDestinationKey> _observableKeySelector = observableKeySelector ?? throw new ArgumentNullException(nameof(observableKeySelector));

        private void ForwardWhenRemove(IObserver<IChangeSet<TDestination, TDestinationKey>> observer, T sourceItem)
        {
            var oblist = _observableSelector(sourceItem).AsObservableCache();
            var changeset = new ChangeSet<TDestination, TDestinationKey>();
            changeset.AddRange(oblist.Items.Select(x => new Change<TDestination, TDestinationKey>(ChangeReason.Remove, _observableKeySelector.Invoke(x), x)));
            oblist.Dispose();
            observer.OnNext(changeset);
        }

        public IObservable<IChangeSet<TDestination, TDestinationKey>> Run() => Observable.Create<IChangeSet<TDestination, TDestinationKey>>
              (
                observer =>
                {
                    var locker = new object();
                    return _source
                // SubscribeMany will forwards inital cascaded inner items when it observers new item,
                // but if one item was removed, any item belonged to it won't be forwarded to observer.
                .SubscribeMany(t => _observableSelector(t).Synchronize(locker).Subscribe(observer.OnNext))
                // So I add a observer here to subscribe all items belonged to the removed item, buildup a 
                // ChangeSet and and forward it to the original observer.
                .Subscribe(t =>
                {
                    foreach (var x in t)
                    {
                        switch (x.Reason)
                        {
                            case ChangeReason.Remove:
                                {
                                    ForwardWhenRemove(observer, x.Current);
                                    break;
                                }
                        }
                    }
                }, observer.OnError);
                });
    }

}
