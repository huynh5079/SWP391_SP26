using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class StudentProfile: BaseEntity
{
    //public string Id { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string StudentCode { get; set; } = null!;

    public string? DepartmentId { get; set; }

    public string? CurrentSemester { get; set; }

    //public DateTime? CreatedAt { get; set; }

    //public DateTime? UpdatedAt { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<EventWaitlist> EventWaitlists { get; set; } = new List<EventWaitlist>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<StudentQuizScore> StudentQuizScores { get; set; } = new List<StudentQuizScore>();

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual User? User { get; set; }
}
