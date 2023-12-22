using shared.Models.ListResponse;

namespace application.Commands;

public class SnapshotForDay
{
    public SnapshotForDay(DateOnly dayOfSnapshot)
    {
        DayOfSnapshot = dayOfSnapshot;
    }

    public DateOnly DayOfSnapshot;

    // ItemSnapshots on the snapshot day
    public readonly List<ItemSnapshotForDay> ItemSnapshots = [];

    public ListSnapshotResponse ListSnapshot
    {
        get
        {
            if (ItemSnapshots.Count == 0)
            {
                return new ListSnapshotResponse(
                    0,
                    0,
                    0,
                    0,
                    null,
                    null,
                    DayOfSnapshot
                );
            }

            long totalInvestedCapital = 0;
            long totalItemCount = 0;
            long salesValue = 0;
            long profit = 0;
            long? steamValue = 0;
            long? buff163Value = 0;
            foreach (var snapshotForItem in ItemSnapshots)
            {
                totalInvestedCapital += snapshotForItem.TotalInvestedCapital;
                totalItemCount += snapshotForItem.TotalItemCount;
                salesValue += snapshotForItem.SalesValue;
                profit += snapshotForItem.Profit;
                if (snapshotForItem.SteamValueForOne is not null)
                {
                    steamValue += snapshotForItem.SteamValueForOne * snapshotForItem.TotalItemCount;
                }

                if (snapshotForItem.Buff163ValueForOne is not null)
                {
                    buff163Value += snapshotForItem.Buff163ValueForOne * snapshotForItem.TotalItemCount;
                }
            }

            return new ListSnapshotResponse(
                totalInvestedCapital,
                totalItemCount,
                salesValue,
                profit,
                steamValue,
                buff163Value,
                DayOfSnapshot
            );
        }
    }
}

public record ItemSnapshotForDay(
    long TotalInvestedCapital,
    long TotalItemCount,
    long SalesValue,
    long Profit,
    long? SteamValueForOne,
    long? Buff163ValueForOne
);