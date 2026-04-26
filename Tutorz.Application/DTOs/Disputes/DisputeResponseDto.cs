using System;
using Tutorz.Domain.Enums;

namespace Tutorz.Application.DTOs.Disputes
{
    public class DisputeResponseDto
    {
        public int Id { get; set; }
        public string DisputeNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ScreenshotUrl { get; set; }
        public DisputeCategory Category { get; set; }
        public string CategoryLabel { get; set; } = string.Empty;
        public DisputeStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string? AdminNote { get; set; }

        // Raised By info (useful for admin view)
        public Guid RaisedByUserId { get; set; }
        public string RaisedByName { get; set; } = string.Empty;
        public string RaisedByRole { get; set; } = string.Empty;
        public string RaisedByPhone { get; set; } = string.Empty;

        // Assignment info
        public Guid? AssignedAdminUserId { get; set; }
        public string? AssignedAdminName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
