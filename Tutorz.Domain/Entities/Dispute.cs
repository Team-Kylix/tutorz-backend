using System;
using Tutorz.Domain.Enums;

namespace Tutorz.Domain.Entities
{
    public class Dispute
    {
        public int Id { get; set; }

        /// <summary>
        /// Auto-generated sequential number, e.g. CMP-00001
        /// </summary>
        public string DisputeNumber { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional screenshot uploaded to Azure Blob Storage.
        /// </summary>
        public string? ScreenshotUrl { get; set; }

        public DisputeCategory Category { get; set; } = DisputeCategory.Other;

        public DisputeStatus Status { get; set; } = DisputeStatus.Pending;

        /// <summary>
        /// Optional admin note added when updating the status.
        /// </summary>
        public string? AdminNote { get; set; }

        public Guid RaisedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation ---
        public User RaisedByUser { get; set; } = null!;
    }
}
