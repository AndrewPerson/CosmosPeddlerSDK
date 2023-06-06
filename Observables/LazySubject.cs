using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace CosmosPeddler.SDK.Observables;

public class LazySubject<T> : SubjectBase<T>
{
    private readonly Func<Task<T>> firstValue;
    private readonly TaskCompletionSource<T> firstValueTCS = new();
    private bool gettingFirstValue = false;
    private object firstValueLock = new();

    private readonly ConcurrentDictionary<IObserver<T>, IObserver<T>> observers = new();
    private T? value = default(T);
    private bool hasValue = false;
    private bool disposed = false;

    public override bool HasObservers => !observers.IsEmpty;

    public override bool IsDisposed => disposed;

    public LazySubject(Func<Task<T>> firstValue)
    {
        this.firstValue = firstValue;
    }

    public override void Dispose()
    {
        observers.Clear();
        disposed = true;
    }

    private Task EnsureFirstValue()
    {
        if (hasValue)
        {
            return Task.CompletedTask;
        }

        lock (firstValueLock)
        {
            if (gettingFirstValue)
            {
                return firstValueTCS.Task;
            }
            
            gettingFirstValue = true;

            firstValue().ContinueWith(t =>
            {
                value = t.Result;
                hasValue = true;
                firstValueTCS.SetResult(t.Result);
            });

            return firstValueTCS.Task;
        }
    }

    public Task<T> GetValue()
    {
        if (hasValue)
        {
            return Task.FromResult(value)!;
        }

        lock (firstValueLock)
        {
            if (!gettingFirstValue)
            {
                gettingFirstValue = true;

                firstValue().ContinueWith(t =>
                {
                    value = t.Result;
                    hasValue = true;
                    firstValueTCS.SetResult(t.Result);
                });
            }

            return firstValueTCS.Task;
        }
    }

    public override void OnCompleted()
    {
        foreach (var observer in observers.Values)
        {
            observer.OnCompleted();
        }

        this.Dispose();
    }

    public override void OnError(Exception error)
    {
        foreach (var observer in observers.Values)
        {
            observer.OnError(error);
        }
    }

    public override void OnNext(T value)
    {
        this.value = value;
        hasValue = true;

        foreach (var observer in observers.Values)
        {
            observer.OnNext(value);
        }
    }

    public override IDisposable Subscribe(IObserver<T> observer)
    {
        if (!hasValue)
        {
            lock (firstValueLock)
            {
                if (!gettingFirstValue)
                {
                    gettingFirstValue = true;

                    firstValue().ContinueWith(t =>
                    {
                        value = t.Result;
                        hasValue = true;
                        firstValueTCS.SetResult(t.Result);
                    });
                }
            }
        }

        observers.AddOrUpdate(observer, _ => observer, (_, _) => observer);

        if (value != null)
        {
            observer.OnNext(value);
        }

        return new Unsubscriber(this, observer);
    }

    private class Unsubscriber : IDisposable
    {
        private LazySubject<T> subject;
        private IObserver<T> observer;

        public Unsubscriber(LazySubject<T> subject, IObserver<T> observer)
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
