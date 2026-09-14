using PROG7312POE.Core.Models;

namespace PROG7312POE.Core.DataStructures;

public sealed class TelemetryBatchBuffer
{
    private readonly TelemetryRecord[][] _rawBatches;

    public TelemetryBatchBuffer(int batchCount, int batchSize)
    {
        _rawBatches = new TelemetryRecord[batchCount][];
        for (var index = 0; index < batchCount; index++)
        {
            _rawBatches[index] = new TelemetryRecord[batchSize];
        }
    }

    public List<TelemetryRecord> Stage(TelemetryRecord record)
    {
        _rawBatches[0][0] = record;

        var stagedRecords = new List<TelemetryRecord>();
        foreach (var batch in _rawBatches)
        {
            foreach (var item in batch)
            {
                if (item is not null)
                {
                    stagedRecords.Add(item);
                }
            }
        }

        return stagedRecords;
    }
}
