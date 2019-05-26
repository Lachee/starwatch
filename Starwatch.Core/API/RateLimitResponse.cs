using System;

namespace Starwatch.API
{
    internal class RateLimitResponse
    {
        public DateTime? RetryAfter { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
    }
}
