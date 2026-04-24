using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorz.Application.DTOs.Institute
{
    public class RevenueSummaryDto
    {
        /// <summary>Total expected revenue = SUM(Class.Fee × approved enrollment count) for all active institute classes</summary>
        public decimal TotalGrossRevenue { get; set; }

        /// <summary>Net revenue the institute receives = TotalGrossRevenue × (CommissionPercentage / 100)</summary>
        public decimal InstituteNetRevenue { get; set; }

        /// <summary>Total payments already received (always 0 until payment tracking is implemented)</summary>
        public decimal TotalReceived { get; set; }

        /// <summary>Outstanding balance = TotalGrossRevenue − TotalReceived</summary>
        public decimal TotalDue { get; set; }

        /// <summary>The institute's configured commission percentage (0–100)</summary>
        public decimal CommissionPercentage { get; set; }
    }

    public class UpdateCommissionRequest
    {
        public decimal CommissionPercentage { get; set; }
    }
}
