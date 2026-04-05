using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VgcCollege.Web.Models
{
    // ─── Identity ───────────────────────────────────────────────────────────
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }

    // ─── Branch ─────────────────────────────────────────────────────────────
    public class Branch
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }

    // ─── Course ─────────────────────────────────────────────────────────────
    public class Course
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public ICollection<CourseEnrolment> Enrolments { get; set; } = new List<CourseEnrolment>();
        public ICollection<FacultyCourseAssignment> FacultyAssignments { get; set; } = new List<FacultyCourseAssignment>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }

    // ─── Student Profile ────────────────────────────────────────────────────
    public class StudentProfile
    {
        public int Id { get; set; }

        [Required]
        public string IdentityUserId { get; set; } = string.Empty;
        public ApplicationUser IdentityUser { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string StudentNumber { get; set; } = string.Empty;

        public ICollection<CourseEnrolment> Enrolments { get; set; } = new List<CourseEnrolment>();
        public ICollection<AssignmentResult> AssignmentResults { get; set; } = new List<AssignmentResult>();
        public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
    }

    // ─── Faculty Profile ────────────────────────────────────────────────────
    public class FacultyProfile
    {
        public int Id { get; set; }

        [Required]
        public string IdentityUserId { get; set; } = string.Empty;
        public ApplicationUser IdentityUser { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        public ICollection<FacultyCourseAssignment> CourseAssignments { get; set; } = new List<FacultyCourseAssignment>();
    }

    // ─── Faculty ↔ Course ───────────────────────────────────────────────────
    public class FacultyCourseAssignment
    {
        public int Id { get; set; }
        public int FacultyProfileId { get; set; }
        public FacultyProfile FacultyProfile { get; set; } = null!;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public bool IsTutor { get; set; } = false;
    }

    // ─── Enrolment ──────────────────────────────────────────────────────────
    public class CourseEnrolment
    {
        public int Id { get; set; }
        public int StudentProfileId { get; set; }
        public StudentProfile StudentProfile { get; set; } = null!;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime EnrolDate { get; set; } = DateTime.Today;

        [MaxLength(50)]
        public string Status { get; set; } = "Active"; // Active, Withdrawn, Completed

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }

    // ─── Attendance ─────────────────────────────────────────────────────────
    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int CourseEnrolmentId { get; set; }
        public CourseEnrolment CourseEnrolment { get; set; } = null!;
        public int WeekNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public bool Present { get; set; }
    }

    // ─── Assignment ─────────────────────────────────────────────────────────
    public class Assignment
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Range(1, 1000)]
        public decimal MaxScore { get; set; }

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public ICollection<AssignmentResult> Results { get; set; } = new List<AssignmentResult>();
    }

    // ─── Assignment Result ──────────────────────────────────────────────────
    public class AssignmentResult
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        public int StudentProfileId { get; set; }
        public StudentProfile StudentProfile { get; set; } = null!;

        [Range(0, 1000)]
        public decimal Score { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }
    }

    // ─── Exam ───────────────────────────────────────────────────────────────
    public class Exam
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Range(1, 1000)]
        public decimal MaxScore { get; set; }

        public bool ResultsReleased { get; set; } = false;

        public ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
    }

    // ─── Exam Result ────────────────────────────────────────────────────────
    public class ExamResult
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public Exam Exam { get; set; } = null!;
        public int StudentProfileId { get; set; }
        public StudentProfile StudentProfile { get; set; } = null!;

        [Range(0, 1000)]
        public decimal Score { get; set; }

        [MaxLength(5)]
        public string? Grade { get; set; }
    }
}
