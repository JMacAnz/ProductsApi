using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class BulkCreateResultDto
    {
        public int CreatedCount { get; set; }
        public int RequestedCount { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public double ElapsedSeconds { get; set; }
        public double ProductsPerSecond { get; set; }
        public string Message => $"Created {CreatedCount}/{RequestedCount} products in {ElapsedSeconds:F2} seconds ({ProductsPerSecond:F0} products/sec)";
    }
}
