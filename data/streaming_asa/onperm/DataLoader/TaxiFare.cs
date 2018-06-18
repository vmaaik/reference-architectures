namespace taxi
{
    using System;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class TaxiFare : ITaxiEvent
    {
        public TaxiFare()
        {
        }

        [JsonProperty]
        public long Medallion { get; set; }

        [JsonProperty]
        public long HackLicense { get; set; }

        [JsonProperty]
        public string VendorId { get; set; }

        [JsonProperty]
        public string PartitionKey { get; set; }
        [JsonProperty]
        public DateTimeOffset PickupTime { get; set; }

        [JsonProperty]
        public string PaymentType { get; set; }

        [JsonProperty]
        public float FareAmount { get; set; }

        [JsonProperty]
        public float Surcharge { get; set; }

        [JsonProperty("mtaTax")]
        public float MTATax { get; set; }

        [JsonProperty]
        public float TipAmount { get; set; }

        [JsonProperty]
        public float TollsAmount { get; set; }

        [JsonProperty]
        public float TotalAmount { get; set; }

        public string BuildPartitionKey()
        {

                return String.Format("{0}_{1}_{2}"
               , Medallion.ToString()
               , HackLicense.ToString()
               , VendorId
               );

            //    return Medallion.ToString();
           
        }

        public static TaxiFare FromString(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new ArgumentException($"{nameof(line)} cannot be null, empty, or only whitespace");
            }

            string[] tokens = line.Split(',');
            if (tokens.Length != 11)
            {
                throw new ArgumentException($"Invalid record: {line}");
            }

            var ride = new TaxiFare();
            try
            {
                ride.Medallion = long.Parse(tokens[0]);
                ride.HackLicense = long.Parse(tokens[1]);
                ride.VendorId = tokens[2];
                ride.PickupTime = DateTimeOffset.ParseExact(
                    tokens[3], "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal);
                ride.PaymentType = tokens[4];
                ride.FareAmount = float.TryParse(tokens[5], out float result) ? result : 0.0f;
                ride.Surcharge = float.TryParse(tokens[6], out result) ? result : 0.0f;
                ride.MTATax = float.TryParse(tokens[7], out result) ? result : 0.0f;
                ride.TipAmount = float.TryParse(tokens[8], out result) ? result : 0.0f;
                ride.TollsAmount = float.TryParse(tokens[9], out result) ? result : 0.0f;
                ride.TotalAmount = float.TryParse(tokens[10], out result) ? result : 0.0f;
                ride.PartitionKey = ride.BuildPartitionKey();
                return ride;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid record: {line}", ex);
            }
        }

        public static string GetPartitionKey(TaxiFare taxiFare)
        {
            return taxiFare.PartitionKey;
        }
    }
}