using DataAccess.Helper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class AEMSContext : DbContext
{
    public AEMSContext()
    {
    }

    public AEMSContext(DbContextOptions<AEMSContext> options)   :   base(options)   {   }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public virtual DbSet<ApprovalLog> ApprovalLogs { get; set; }

    public virtual DbSet<BudgetProposal> BudgetProposals { get; set; }

    public virtual DbSet<CheckInHistory> CheckInHistories { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventAgenda> EventAgenda { get; set; }

    public virtual DbSet<EventDocument> EventDocuments { get; set; }

    public virtual DbSet<EventQuiz> EventQuizzes { get; set; }

    public virtual DbSet<EventReminder> EventReminders { get; set; }

    public virtual DbSet<EventTeam> EventTeams { get; set; }

    public virtual DbSet<EventWaitlist> EventWaitlists { get; set; }

    public virtual DbSet<ExpenseReceipt> ExpenseReceipts { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<QuizQuestion> QuizQuestions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<StaffProfile> StaffProfiles { get; set; }

    public virtual DbSet<StudentProfile> StudentProfiles { get; set; }

    public virtual DbSet<StudentQuizScore> StudentQuizScores { get; set; }

    public virtual DbSet<SystemErrorLog> SystemErrorLogs { get; set; }

    public virtual DbSet<TeamMember> TeamMembers { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=LAPTOP-HYNH\\NGHUY;Database=SWP391_SP26_Group1;User Id=sa;Password=123456;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalLog>(entity =>
        {
            entity.ToTable("ApprovalLog");

            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .HasConversion<string>();
            entity.Property(e => e.ApproverId).HasMaxLength(450);
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.EventId).HasMaxLength(450);

            entity.HasOne(d => d.Approver).WithMany(p => p.ApprovalLogs)
                .HasForeignKey(d => d.ApproverId)
                .HasConstraintName("FK__ApprovalL__Appro__0B91BA14");

            entity.HasOne(d => d.Event).WithMany(p => p.ApprovalLogs)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ApprovalL__Event__0A9D95DB");
        });

        modelBuilder.Entity<BudgetProposal>(entity =>
        {
            entity.ToTable("BudgetProposal");

            entity.Property(e => e.ActualAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EventId).HasMaxLength(450);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PlannedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasConversion<string>();
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Event).WithMany(p => p.BudgetProposals)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BudgetPro__Event__7F2BE32F");
        });

        modelBuilder.Entity<CheckInHistory>(entity =>
        {
            entity.ToTable("CheckInHistory");

            entity.Property(e => e.DeviceName).HasMaxLength(255);
            entity.Property(e => e.ScanType)
                .HasMaxLength(50)
                .HasConversion<string>();
            entity.Property(e => e.ScannerId).HasMaxLength(450);
            entity.Property(e => e.TicketId).HasMaxLength(450);

            entity.HasOne(d => d.Scanner).WithMany(p => p.CheckInHistories)
                .HasForeignKey(d => d.ScannerId)
                .HasConstraintName("FK__CheckInHi__Scann__14270015");

            entity.HasOne(d => d.Ticket).WithMany(p => p.CheckInHistories)
                .HasForeignKey(d => d.TicketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInHi__Ticke__1332DBDC");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Department");

            entity.HasIndex(e => e.Code, "UQ__Departme__A25C5AA72B6A8CC5").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Event");

            entity.Property(e => e.DepartmentId).HasMaxLength(450);
            entity.Property(e => e.DepositAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsDepositRequired).HasDefaultValue(false);
            entity.Property(e => e.OrganizerId).HasMaxLength(450);
            entity.Property(e => e.SemesterId).HasMaxLength(450);
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasConversion<string>();
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasConversion<string>();
            entity.Property(e => e.Title).HasMaxLength(500);

            entity.HasOne(d => d.Department).WithMany(p => p.Events)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__Event__Departmen__07C12930");

            entity.HasOne(d => d.Organizer).WithMany(p => p.Events)
                .HasForeignKey(d => d.OrganizerId)
                .HasConstraintName("FK__Event__Organizer__05D8E0BE");

            entity.HasOne(d => d.Semester).WithMany(p => p.Events)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("FK__Event__SemesterI__06CD04F7");
        });

        modelBuilder.Entity<EventAgenda>(entity =>
        {
            entity.Property(e => e.EventId).HasMaxLength(450);
            entity.Property(e => e.SessionName).HasMaxLength(255);
            entity.Property(e => e.SpeakerName).HasMaxLength(255);

            entity.HasOne(d => d.Event).WithMany(p => p.EventAgenda)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__EventAgen__Event__08B54D69");
        });

        modelBuilder.Entity<EventDocument>(entity =>
        {
            entity.ToTable("EventDocument");

            entity.Property(e => e.EventId).HasMaxLength(450);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.Event).WithMany(p => p.EventDocuments)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__EventDocu__Event__09A971A2");
        });

        modelBuilder.Entity<EventQuiz>(entity =>
        {
            entity.ToTable("EventQuiz");

            entity.Property(e => e.EventId).HasMaxLength(450);
            entity.Property(e => e.PassingScore).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasConversion<string>();

            entity.HasOne(d => d.Event).WithMany(p => p.EventQuizzes)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventQuiz__Event__160F4887");
        });

        modelBuilder.Entity<EventReminder>(entity =>
        {
            entity.ToTable("EventReminder");

            entity.Property(e => e.EventId).HasMaxLength(450);
            entity.Property(e => e.IsSent).HasDefaultValue(false);

            entity.HasOne(d => d.Event).WithMany(p => p.EventReminders)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__EventRemi__Event__151B244E");
        });

        modelBuilder.Entity<EventTeam>(entity =>
        {
            entity.ToTable("EventTeam");

            entity.HasIndex(e => new { e.EventId, e.TeamName }, "UIX_EventTeam_Name").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Score)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TeamName).HasMaxLength(255);

            entity.HasOne(d => d.Event).WithMany(p => p.EventTeams)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventTeam__Event__7C4F7684");
        });

        modelBuilder.Entity<EventWaitlist>(entity =>
        {
            entity.ToTable("EventWaitlist");

            entity.HasIndex(e => new { e.EventId, e.StudentId }, "UIX_Waitlist_Event_Student").IsUnique();

            entity.Property(e => e.IsNotified).HasDefaultValue(false);

            entity.HasOne(d => d.Event).WithMany(p => p.EventWaitlists)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventWait__Event__123EB7A3");

            entity.HasOne(d => d.Student).WithMany(p => p.EventWaitlists)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventWait__Stude__114A936A");
        });

        modelBuilder.Entity<ExpenseReceipt>(entity =>
        {
            entity.ToTable("ExpenseReceipt");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BudgetProposalId).HasMaxLength(450);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasConversion<string>();
            entity.Property(e => e.SubmittedBy).HasMaxLength(450);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.BudgetProposal).WithMany(p => p.ExpenseReceipts)
                .HasForeignKey(d => d.BudgetProposalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ExpenseRe__Budge__00200768");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("Feedback");

            entity.HasIndex(e => new { e.EventId, e.StudentId }, "UIX_Feedback_Event_Student").IsUnique();

            entity.Property(e => e.Comment).HasMaxLength(1000);

            entity.HasOne(d => d.Event).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.EventId)
                .HasConstraintName("FK__Feedback__EventI__0E6E26BF");

            entity.HasOne(d => d.Student).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Feedback__Studen__0F624AF8");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification");

            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__10566F31");
        });

        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.ToTable("QuizQuestion");

            entity.Property(e => e.CorrectAnswer).HasMaxLength(1);
            entity.Property(e => e.OptionA).HasMaxLength(255);
            entity.Property(e => e.OptionB).HasMaxLength(255);
            entity.Property(e => e.OptionC).HasMaxLength(255);
            entity.Property(e => e.OptionD).HasMaxLength(255);
            entity.Property(e => e.QuestionText).HasMaxLength(500);
            entity.Property(e => e.QuizId).HasMaxLength(450);
            entity.Property(e => e.ScorePoint).HasDefaultValue(1);

            entity.HasOne(d => d.Quiz).WithMany(p => p.QuizQuestions)
                .HasForeignKey(d => d.QuizId)
                .HasConstraintName("FK__QuizQuest__QuizI__17036CC0");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B61604908F5F6").IsUnique();

            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasConversion<string>();
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.ToTable("Semester");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasConversion<string>();
        });

        modelBuilder.Entity<StaffProfile>(entity =>
        {
            entity.ToTable("StaffProfile");

            entity.HasIndex(e => e.UserId, "UQ__StaffPro__1788CC4DBC29800F").IsUnique();

            entity.HasIndex(e => e.StaffCode, "UQ__StaffPro__D83AD812EC58A448").IsUnique();

            entity.Property(e => e.DepartmentId).HasMaxLength(450);
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.StaffCode).HasMaxLength(50);

            entity.HasOne(d => d.Department).WithMany(p => p.StaffProfiles)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__StaffProf__Depar__04E4BC85");

            // Correct Relationship: StaffProfile depends on User
            entity.HasOne(d => d.User)
                .WithOne(p => p.StaffProfile)
                .HasForeignKey<StaffProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.ToTable("StudentProfile");

            entity.HasIndex(e => e.UserId, "UQ__StudentP__1788CC4DE789B167").IsUnique();

            entity.HasIndex(e => e.StudentCode, "UQ__StudentP__1FC8860425CD052F").IsUnique();

            entity.Property(e => e.CurrentSemester).HasMaxLength(50);
            entity.Property(e => e.DepartmentId).HasMaxLength(450);
            entity.Property(e => e.StudentCode).HasMaxLength(50);

            entity.HasOne(d => d.Department).WithMany(p => p.StudentProfiles)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__StudentPr__Depar__02FC7413");

            // Correct Relationship: StudentProfile depends on User
            entity.HasOne(d => d.User)
                .WithOne(p => p.StudentProfile)
                .HasForeignKey<StudentProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentQuizScore>(entity =>
        {
            entity.ToTable("StudentQuizScore");

            entity.Property(e => e.QuizId).HasMaxLength(450);
            entity.Property(e => e.StudentId).HasMaxLength(450);

            entity.HasOne(d => d.Quiz).WithMany(p => p.StudentQuizScores)
                .HasForeignKey(d => d.QuizId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentQu__QuizI__17F790F9");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentQuizScores)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StudentQu__Stude__18EBB532");
        });

        modelBuilder.Entity<SystemErrorLog>(entity =>
        {
            entity.ToTable("SystemErrorLog");

            entity.Property(e => e.ExceptionType).HasMaxLength(255);
            entity.Property(e => e.Source).HasMaxLength(255);
            entity.Property(e => e.UserId).HasMaxLength(450);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.ToTable("TeamMember");

            entity.HasIndex(e => new { e.TeamId, e.StudentId }, "UIX_TeamMember_Team_Student").IsUnique();

            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasConversion<string>();

            entity.HasOne(d => d.Student).WithMany(p => p.TeamMembers)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TeamMembe__Stude__7E37BEF6");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamMembers)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("FK__TeamMembe__TeamI__7D439ABD");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Ticket", tb =>
                {
                    tb.HasTrigger("TR_Ticket_CheckCapacity");
                    tb.HasTrigger("TR_Ticket_RemoveWaitlist");
                });

            entity.HasIndex(e => new { e.EventId, e.StudentId }, "UIX_Ticket_Event_Student").IsUnique();

            entity.HasIndex(e => e.TicketCode, "UQ__Ticket__598CF7A37C115FF1").IsUnique();

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasConversion<string>();
            entity.Property(e => e.TicketCode).HasMaxLength(100);

            entity.HasOne(d => d.Event).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ticket__EventId__0C85DE4D");

            entity.HasOne(d => d.Student).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Ticket__StudentI__0D7A0286");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__A9D10534CD0FD16E").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.IsBanned).HasDefaultValue(false);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.RoleId).HasMaxLength(450);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasConversion<string>();

            // Removed incorrect reversed relationship.
            // Relationships are defined in StudentProfile and StaffProfile configurations.

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__RoleId__01142BA1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
