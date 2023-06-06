using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;

namespace CosmosPeddler.SDK.Observables;

public class LazyUniqueBySubject<UniqueByT, ValueT> : IObserver<ValueT>, IObservable<(UniqueByT, ValueT)> where UniqueByT : notnull
{
    private Func<ValueT, UniqueByT> getKey;

    private readonly Func<IAsyncEnumerable<ValueT>> firstValues;
    private readonly TaskCompletionSource firstValuesTCS = new();
    private bool gettingFirstValues = false;
    private bool gottenFirstValues = false;
    private object firstValuesLock = new();

    private readonly ConcurrentDictionary<IObserver<(UniqueByT, ValueT)>, IObserver<(UniqueByT, ValueT)>> observers = new();
    private ConcurrentDictionary<UniqueByT, ValueT> values = new();
    private bool disposed = false;

    public bool HasObservers => !observers.IsEmpty;

    public bool IsDisposed => disposed;

    public LazyUniqueBySubject(Func<ValueT, UniqueByT> getKey, Func<IAsyncEnumerable<ValueT>> firstValues)
    {
        this.getKey = getKey;
        this.firstValues = firstValues;
    }

    public void Dispose()
    {
        observers.Clear();
        disposed = true;
    }

    private Task EnsureFirstValues()
    {
        if (gottenFirstValues)
        {
            return Task.CompletedTask;
        }

        lock (firstValuesLock)
        {
            if (gettingFirstValues)
            {
                return firstValuesTCS.Task;
            }

            gettingFirstValues = true;

            return Task.Run(async() =>
            {
                var enumerator = firstValues().GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync())
                {
                    this.OnNext(enumerator.Current);
                }

                firstValuesTCS.SetResult();

                gottenFirstValues = true;
                gettingFirstValues = false;
            });
        }
    }

    public async IAsyncEnumerable<ValueT> GetValues()
    {
        await EnsureFirstValues();

        foreach (var value in values.Values)
        {
            yield return value;
        }
    }

    /// <summary>
    /// Gets the value associated with the specified key. Will try and get all possible values first.
    /// This may be very expensive/slow.
    /// </summary>
    public async Task<(bool, ValueT)> TryGetValue(UniqueByT key)
    {
        await EnsureFirstValues();

        if (values.TryGetValue(key, out var foundValue))
        {
            return (true, foundValue);
        }

        return (false, default(ValueT));
    }

    /// <summary>
    /// Gets the value associated with the specified key. Only searches keys that have already been received.
    /// </summary>
    public bool TryGetValueInstant(UniqueByT key, out ValueT value)
    {
        if (values.TryGetValue(key, out value))
        {
            return true;
        }

        return false;
    }

    public void OnCompleted()
    {
        foreach (var observer in observers.Values)
        {
            observer.OnCompleted();
        }

        this.Dispose();
    }

    public void OnError(Exception error)
    {
        foreach (var observer in observers.Values)
        {
            observer.OnError(error);
        }
    }

    public void OnNext(ValueT value)
    {
        var key = getKey(value);
        this.values[key] = value;

        foreach (var observer in observers.Values)
        {
            observer.OnNext((key, value));
        }
    }

    public IDisposable Subscribe(IObserver<(UniqueByT, ValueT)> observer)
    {
        EnsureFirstValues();

        observers.AddOrUpdate(observer, _ => observer, (_, _) => observer);

        foreach (var keyValue in values)
        {
            observer.OnNext((keyValue.Key, keyValue.Value));
        }

        return new Unsubscriber(this, observer);
    }

    private class Unsubscriber : IDisposable
    {
        private LazyUniqueBySubject<UniqueByT, ValueT> subject;
        private IObserver<(UniqueByT, ValueT)> observer;

        public Unsubscriber(LazyUniqueBySubject<UniqueByT, ValueT> subject, IObserver<(UniqueByT, ValueT)> observer)
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