using System;
using System.ComponentModel.DataAnnotations;

namespace Tutorz.Application.DTOs.Withdrawal
{
    public class WithdrawalProcessDto
    {
        [Required]
        public Guid TutorId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Withdrawal amount must be greater than zero.")]
        public decimal WithdrawalAmount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty; // "OnHand" or "Online"
    }

    public class WithdrawalRequestDto
    {
        [Required]
        public Guid InstituteId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Withdrawal amount must be greater than zero.")]
        public decimal RequestedAmount { get; set; }
    }
}
