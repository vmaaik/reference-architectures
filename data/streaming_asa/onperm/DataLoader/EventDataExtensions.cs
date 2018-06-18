using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.EventHubs;

namespace taxi
{
    public static class EventDataExtensions
    {
        // public static Dictionary<string,EventDataBatch>  dictionary = new Dictionary<string,EventDataBatch>();
        public static IEnumerable<EventDataBatch> Partition(this IEnumerable<IGrouping<string, PartitionedEventData>> source, int batchSize = 3, string partitionKey = null)
        {
            foreach (var group in source)
            {
                // batch size is based on the goup by results 
                EventDataBatch eventDataBatch = new EventDataBatch(group.Count(), group.Key);

                int i = 0;
                foreach (var eventData in group)
                {
                    ++i;
                    if (!eventDataBatch.TryAdd(eventData.EventData))
                    {
                        yield return eventDataBatch;
                        eventDataBatch = new EventDataBatch(group.LongCount()-i, group.Key);
                        // It will be small enough in our case, but we should probably figure out a better way later
                        eventDataBatch.TryAdd(eventData.EventData);
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