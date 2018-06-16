namespace taxi
{
    using System;
    using System.Globalization;
    using Microsoft.Azure.EventHubs;
    public class PartitionedEventData
    {
        public PartitionedEventData(string partitionId, EventData eventData)
        {
            this.PartitionID = partitionId;
            this.EventData = eventData;
        }
        public String PartitionID { get; set; }
        public EventData EventData { get; set; }
    }
}