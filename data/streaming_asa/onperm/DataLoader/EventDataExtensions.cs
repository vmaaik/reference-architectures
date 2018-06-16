using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.EventHubs;

namespace taxi
{
    public static class EventDataExtensions
    {
        public static IEnumerable<EventDataBatch> Partition
            (this IEnumerable<IGrouping<string, PartitionedEventData>> source, int batchSize = 10, string partitionKey = null)
        {

            foreach (IEnumerable<PartitionedEventData> partionedEventDataList in source)
            {
                EventDataBatch eventDataBatch = new EventDataBatch(batchSize, partionedEventDataList.First().PartitionID);
                foreach (var partionedEventData in partionedEventDataList)
                {
                    if (!eventDataBatch.TryAdd(partionedEventData.EventData))
                    {
                        yield return eventDataBatch;

                        eventDataBatch = new EventDataBatch(batchSize, partionedEventData.PartitionID);
                        eventDataBatch.TryAdd(partionedEventData.EventData);
                    }
                }
                if (eventDataBatch.Count > 0)
                {
                    yield return eventDataBatch;
                }


            }
        }
    }
}