using System.Collections.Concurrent;

namespace CosmosPeddler.SDK.Observables;

public class IndividualLazyUniqueBySubject<UniqueByT, ValueT> where UniqueByT : notnull
{
    private readonly Func<UniqueByT, Task<ValueT>> valueFactory;
    private readonly ConcurrentDictionary<UniqueByT, LazySubject<ValueT>> _subjects = new();

    public IObservable<ValueT> this[UniqueByT key] => GetOrCreate(key);

    public IObservable<ValueT> Set(UniqueByT key, ValueT value)
    {
        return _subjects.AddOrUpdate(
            key,
            key =>
            {
                var subject = new LazySubject<ValueT>(() => valueFactory(key));

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

                return subject;
            }
        );
    }

    public IndividualLazyUniqueBySubject(Func<UniqueByT, Task<ValueT>> valueFactory)
    {
        this.valueFactory = valueFactory;
    }
}