namespace PROG7312POE.Core.DataStructures;

public sealed class TelemetryWorkQueue<T>
{
    private readonly Queue<T> _standard = new();
    private readonly PriorityQueue<T, (int Severity, long Sequence)> _urgent = new();
    private readonly object _gate = new();
    private long _sequence;

    public void Enqueue(T item, int priority)
    {
        lock (_gate)
        {
            if (priority < 2) _urgent.Enqueue(item, (priority, _sequence++));
            else _standard.Enqueue(item);
        }
    }

    public bool TryDequeue(out T item)
    {
        lock (_gate)
        {
            if (_urgent.TryDequeue(out item!, out _)) return true;
            return _standard.TryDequeue(out item!);
        }
    }
}
