using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
        public DbSet<FacultyProfile> FacultyProfiles => Set<FacultyProfile>();
        public DbSet<FacultyCourseAssignment> FacultyCourseAssignments => Set<FacultyCourseAssignment>();
        public DbSet<CourseEnrolment> CourseEnrolments => Set<CourseEnrolment>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<AssignmentResult> AssignmentResults => Set<AssignmentResult>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<ExamResult> ExamResults => Set<ExamResult>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<StudentProfile>()
                .HasIndex(s => s.StudentNumber)
                .IsUnique();

            builder.Entity<AssignmentResult>()
                .HasIndex(ar => new { ar.AssignmentId, ar.StudentProfileId })
                .IsUnique();

            builder.Entity<ExamResult>()
                .HasIndex(er => new { er.ExamId, er.StudentProfileId })
                .IsUnique();

            builder.Entity<AttendanceRecord>()
                .HasIndex(a => new { a.CourseEnrolmentId, a.WeekNumber })
                .IsUnique();
        }
    }
}
