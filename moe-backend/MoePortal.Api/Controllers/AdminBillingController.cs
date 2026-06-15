using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoePortal.Core.Domain.Entities;
using MoePortal.Infrastructure.Data;

namespace MoePortal.Api.Controllers;

[ApiController]
[Route("api/admin/billing")]
[Authorize(Policy = "AnyAdmin")]
public class AdminBillingController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminBillingController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _db.Courses
            .Include(c => c.FeeComponents)
            .ToListAsync();
        return Ok(courses);
    }

    public class CreateCourseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<FeeComponentRequest> FeeComponents { get; set; } = new();
    }

    public class FeeComponentRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsGstApplicable { get; set; }
    }

    [HttpPost("courses")]
    [Authorize(Policy = "HqAdminOnly")] // strict policy
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest req)
    {
        var course = new Course
        {
            Name = req.Name,
            Description = req.Description,
            FeeComponents = req.FeeComponents.Select(f => new CourseFeeComponent
            {
                Name = f.Name,
                Amount = f.Amount,
                IsGstApplicable = f.IsGstApplicable
            }).ToList()
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        return Ok(course);
    }

    public class EnrollRequest
    {
        public Guid CitizenId { get; set; }
    }

    [HttpPost("courses/{courseId}/enroll")]
    public async Task<IActionResult> EnrollStudent(Guid courseId, [FromBody] EnrollRequest req)
    {
        var citizen = await _db.CitizenRecords.FindAsync(req.CitizenId);
        if (citizen == null) return NotFound("Citizen not found.");

        var course = await _db.Courses.Include(c => c.FeeComponents).FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound("Course not found.");

        // Create enrollment
        var enrollment = new CourseEnrollment
        {
            CitizenId = req.CitizenId,
            CourseId = courseId
        };
        _db.CourseEnrollments.Add(enrollment);

        // Generate Invoice
        var invoice = new Invoice
        {
            CitizenRecordId = req.CitizenId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
            TotalAmount = course.FeeComponents.Sum(f => f.Amount),
            LineItems = course.FeeComponents.Select(f => new InvoiceLineItem
            {
                Description = f.Name,
                Amount = f.Amount
            }).ToList()
        };
        _db.Invoices.Add(invoice);

        await _db.SaveChangesAsync();

        return Ok(new { Enrollment = enrollment, Invoice = invoice });
    }
}
