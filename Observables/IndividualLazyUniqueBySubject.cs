using System.Collections.Concurrent;

namespace CosmosPeddler.SDK.Observables;

public class IndividualLazyUniqueBySubject<UniqueByT, ValueT> : IObservable<(UniqueByT, ValueT)> where UniqueByT : notnull
{
    private readonly Func<UniqueByT, Task<ValueT>> valueFactory;
    private readonly ConcurrentDictionary<UniqueByT, LazySubject<ValueT>> _subjects = new();

    private readonly ConcurrentDictionary
    <
        IObserver<(UniqueByT, ValueT)>,
        (IObserver<(UniqueByT, ValueT)>, ConcurrentDictionary<UniqueByT, IDisposable>)
    >
    observers = new();

    public IObservable<ValueT> this[UniqueByT key] => GetOrCreate(key);

    public IObservable<ValueT> Set(UniqueByT key, ValueT value)
    {
        return _subjects.AddOrUpdate(
            key,
            key =>
            {
                var subject = new LazySubject<ValueT>(() => valueFactory(key));

                foreach (var (observer, subscriptions) in observers.Values)
                {
                    var subscription = subject.Subscribe(val => observer.OnNext((key, val)));
                    subscriptions.AddOrUpdate(key, _ => subscription, (_, old) =>
                    {
                        old.Dispose();
                        return subscription;
                    });
                }

                return subject;
            },
            (_, subject) =>
            {
                subject.OnNext(value);
                return subject;
            }
        );
    }

    public IObservable<ValueT> GetOrCreate(UniqueByT key)
    {       
        return _subjects.GetOrAdd(
            key,
            key =>
            {
                var subject = new LazySubject<ValueT>(() => valueFactory(key));

                foreach (var (observer, subscriptions) in observers.Values)
                {
                    var subscription = subject.Subscribe(val => observer.OnNext((key, val)));
                    subscriptions.AddOrUpdate(key, _ => subscription, (_, old) =>
                    {
                        old.Dispose();
                        return subscription;
                    });
                }

                return subject;
            }
        );
    }

    public IDisposable Subscribe(IObserver<(UniqueByT, ValueT)> observer)
    {
        var subscriptions = new ConcurrentDictionary<UniqueByT, IDisposable>();
        observers.TryAdd(observer, (observer, subscriptions));

        foreach (var (key, subject) in _subjects)
        {
            var subscription = subject.Subscribe(val => observer.OnNext((key, val)));
            subscriptions.AddOrUpdate(key, _ => subscription, (_, old) =>
            {
                old.Dispose();
                return subscription;
            });
        }

        return new Unsubscriber(this, observer);
    }

    public IndividualLazyUniqueBySubject(Func<UniqueByT, Task<ValueT>> valueFactory)
    {
        this.valueFactory = valueFactory;
    }

    private class Unsubscriber : IDisposable
    {
        private IndividualLazyUniqueBySubject<UniqueByT, ValueT> subject;
        private IObserver<(UniqueByT, ValueT)> observer;

        public Unsubscriber(IndividualLazyUniqueBySubject<UniqueByT, ValueT> subject, IObserver<(UniqueByT, ValueT)> observer)
        {
            this.subject = subject;
            this.observer = observer;
        }

        public void Dispose()
        {
            subject.observers.Remove(observer, out _);
        }
    }
}