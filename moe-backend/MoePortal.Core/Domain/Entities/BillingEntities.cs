using System;
using System.Collections.Generic;

namespace MoePortal.Core.Domain.Entities;

public class Course : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }

    public ICollection<CourseFeeComponent> FeeComponents { get; set; } = new List<CourseFeeComponent>();
}

public class CourseFeeComponent : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsGstApplicable { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }

    public Course? Course { get; set; }
}

public class CourseEnrollment : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CitizenId { get; set; }
    public Guid CourseId { get; set; }
    public DateTimeOffset EnrollmentDate { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }

    public CitizenRecord? CitizenRecord { get; set; }
    public Course? Course { get; set; }
}

public class InvoiceLineItem : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; set; }

    public Invoice? Invoice { get; set; }
}
