
namespace taxi
{
    using System;
    using System.Threading.Tasks.Dataflow;
    public static class EventDataExtensions
    {
        public static void Transform(this string source, TransformBlock<string, PartitionedEventData> block)
        {
            block.Post(source);
        }
    }
}