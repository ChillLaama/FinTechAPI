using System.ComponentModel.DataAnnotations;
using FinTechAPI.Domain.Models;

namespace FinTechAPI.Application.DTOs
{
    public class UpdateTransactionStatusDto
    {
        [Required]
        public TransactionStatus Status { get; set; }
    }
}
