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

        /// <summary>
        /// The UserId of the admin who first changed the status away from Pending.
        /// Once set, only this admin (or SuperAdmin) can update the dispute.
        /// </summary>
        public Guid? AssignedAdminUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        // --- Navigation ---
        public User RaisedByUser { get; set; } = null!;

        /// <summary>Navigation to the admin assigned to this dispute (nullable).</summary>
        public User? AssignedAdmin { get; set; }
    }
}
