using System;
using System.Collections.Generic;
using DataAccess.Enum;
using DataAccess.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;

namespace DataAccess.Entities;

public partial class AEMSContext : DbContext
{
	private static readonly MethodInfo SetSoftDeleteFilterMethod = typeof(AEMSContext)
		.GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!;

	private readonly IHttpContextAccessor? _httpContextAccessor;

	public AEMSContext()
	{
	}

	public AEMSContext(DbContextOptions<AEMSContext> options) : base(options) { }

	public AEMSContext(DbContextOptions<AEMSContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	internal string? CurrentUserId => _httpContextAccessor?.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

	internal DateTime CurrentVietnamTime => DateTimeHelper.GetVietnamTime();

	public override int SaveChanges()
	{
		ApplyAuditInfo();
		return base.SaveChanges();
	}

	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		ApplyAuditInfo();
		return base.SaveChanges(acceptAllChangesOnSuccess);
	}

	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		=> await SaveChangesAsync(true, cancellationToken);

	public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		ApplyAuditInfo();
		return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}

	internal void PrepareBulkInsert<TEntity>(IEnumerable<TEntity> entities)
		where TEntity : class
	{
		var now = CurrentVietnamTime;
		var currentUserId = CurrentUserId;

		foreach (var entity in entities.OfType<BaseEntity>())
		{
			entity.CreatedAt = now;
			entity.UpdatedAt = now;
			entity.DeletedAt = null;
			entity.CreatedBy = currentUserId;
			entity.UpdatedBy = currentUserId;
		}
	}

	private void ApplyAuditInfo()
	{
		var now = CurrentVietnamTime;
		var currentUserId = CurrentUserId;

		foreach (var entry in ChangeTracker.Entries<BaseEntity>())
		{
			switch (entry.State)
			{
				case EntityState.Added:
					entry.Entity.CreatedAt = now;
					entry.Entity.UpdatedAt = now;
					entry.Entity.DeletedAt = null;
					entry.Entity.CreatedBy = currentUserId;
					entry.Entity.UpdatedBy = currentUserId;
					break;
				case EntityState.Modified:
					entry.Entity.UpdatedAt = now;
					entry.Entity.UpdatedBy = currentUserId;
					break;
				case EntityState.Deleted:
					entry.State = EntityState.Modified;
					entry.Entity.DeletedAt = now;
					entry.Entity.UpdatedAt = now;
					entry.Entity.UpdatedBy = currentUserId;
					break;
			}
		}
	}

	private static void ApplyGlobalSoftDeleteQueryFilters(ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes()
			.Where(entityType => typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)))
		{
			SetSoftDeleteFilterMethod.MakeGenericMethod(entityType.ClrType)
				.Invoke(null, new object[] { modelBuilder });
		}
	}

	private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
		where TEntity : BaseEntity
	{
		modelBuilder.Entity<TEntity>()
			.HasQueryFilter(entity => entity.DeletedAt == null);
	}

	private static void ConfigureBaseEntityAuditProperties(ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes()
			.Where(entityType => typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)))
		{
			modelBuilder.Entity(entityType.ClrType)
				.Property<string?>(nameof(BaseEntity.CreatedBy))
				.HasMaxLength(450);

			modelBuilder.Entity(entityType.ClrType)
				.Property<string?>(nameof(BaseEntity.UpdatedBy))
				.HasMaxLength(450);
		}
	}

	private static void ConfigureBaseEntityConcurrencyTokens(ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes()
			.Where(entityType => typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)))
		{
			modelBuilder.Entity(entityType.ClrType)
				.Property<byte[]>(nameof(BaseEntity.RowVersion))
				.IsRowVersion()
				.IsConcurrencyToken();
		}
	}

	public virtual DbSet<ApprovalLog> ApprovalLogs { get; set; }

	public virtual DbSet<BudgetProposal> BudgetProposals { get; set; }
    public virtual DbSet<BudgetItem> BudgetItems { get; set; }

    public virtual DbSet<ChatSession> ChatSessions { get; set; }

	public virtual DbSet<ChatMessage> ChatMessages { get; set; }

	public virtual DbSet<ChatbotSession> ChatbotSessions { get; set; }

	public virtual DbSet<ChatbotMessage> ChatbotMessages { get; set; }

	public virtual DbSet<CheckInHistory> CheckInHistories { get; set; }

	public virtual DbSet<Department> Departments { get; set; }

	public virtual DbSet<Location> Locations { get; set; }

	public virtual DbSet<Topic> Topics { get; set; }

	public virtual DbSet<Event> Events { get; set; }

	public virtual DbSet<EventAgenda> EventAgenda { get; set; }

	public virtual DbSet<EventDocument> EventDocuments { get; set; }

	public virtual DbSet<EventQuiz> EventQuizzes { get; set; }

	public virtual DbSet<EventQuizQuestion> EventQuizQuestions { get; set; }

	public virtual DbSet<EventReminder> EventReminders { get; set; }

	public virtual DbSet<EventTeam> EventTeams { get; set; }

	public virtual DbSet<EventWaitlist> EventWaitlists { get; set; }

	public virtual DbSet<ExpenseReceipt> ExpenseReceipts { get; set; }

	public virtual DbSet<Feedback> Feedbacks { get; set; }

	public virtual DbSet<Notification> Notifications { get; set; }

	public virtual DbSet<QuestionBank> QuestionBanks { get; set; }

	public virtual DbSet<QuizSet> QuizSets { get; set; }

	public virtual DbSet<QuizSetQuestion> QuizSetQuestions { get; set; }

	public virtual DbSet<Role> Roles { get; set; }

	public virtual DbSet<Semester> Semesters { get; set; }

	public virtual DbSet<StaffProfile> StaffProfiles { get; set; }

	public virtual DbSet<StudentProfile> StudentProfiles { get; set; }

	public virtual DbSet<StudentAnswer> StudentAnswers { get; set; }

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


            // Event FK and basic props
            entity.Property(e => e.EventId).HasMaxLength(450);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);

            // monetary fields
            entity.Property(e => e.PlannedAmount).HasColumnType("decimal(18, 2)");

            // Status enum stored as string
            entity.Property(e => e.Status)
                .HasMaxLength(50)
				.HasDefaultValue(ProposalStatusEnum.Pending) // default to Pending
                .HasConversion<string>();

            entity.Property(e => e.Note).HasMaxLength(500);

            // New approval fields
            entity.Property(e => e.ApprovedBy).HasMaxLength(450);
            entity.Property(e => e.ApprovedAt).HasColumnType("datetime2");

            // Relationships
            entity.HasOne(d => d.Event).WithMany(p => p.BudgetProposals)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BudgetPro__Event__7F2BE32F");

            // Approver relationship (nullable)
            entity.HasOne(d => d.Approver)
                .WithMany() // no collection navigation on User
                .HasForeignKey(d => d.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BudgetProposal_Approver");
        });

        // BudgetItem mapping
        modelBuilder.Entity<BudgetItem>(entity =>
        {
            entity.ToTable("BudgetItem");

            entity.Property(e => e.BudgetProposalId).HasMaxLength(450);
            entity.Property(e => e.Category).HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EstimatedAmount).HasColumnType("decimal(18, 2)");

            // Relationship to BudgetProposal (proposal may not have a collection navigation yet)
            entity.HasOne(d => d.BudgetProposal)
                .WithMany() // keep flexible: no required collection on BudgetProposal
                .HasForeignKey(d => d.BudgetProposalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BudgetItem_BudgetProposal");
        });

        modelBuilder.Entity<ChatSession>(entity =>
		{
			entity.ToTable("ChatSession");

			// Indexes
			entity.HasIndex(e => e.UserId, "IX_ChatSession_UserId");

			entity.Property(e => e.UserId).HasMaxLength(450);
			entity.Property(e => e.Title).HasMaxLength(255);
			entity.Property(e => e.Status)
				.HasMaxLength(50)
				.HasConversion<string>();
			entity.Property(e => e.IsDeleted).HasDefaultValue(false);

			// Relationship: ChatSession -> User
			entity.HasOne(d => d.User)
				.WithMany() // Or .WithMany(u => u.ChatSessions) if you add collection to User
				.HasForeignKey(d => d.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ChatMessage>(entity =>
		{
			entity.ToTable("ChatMessage");

			// Indexes
			entity.HasIndex(e => e.SessionId, "IX_ChatMessage_SessionId");
			entity.HasIndex(e => new { e.SessionId, e.CreatedAt }, "IX_ChatMessage_SessionId_CreatedAt");
			
			entity.Property(e => e.SessionId).HasMaxLength(450);
			entity.Property(e => e.Sender).HasMaxLength(20);
			entity.Property(e => e.Status)
				.HasMaxLength(50)
				.HasConversion<string>();
			entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
			entity.Property(e => e.IsDeleted).HasDefaultValue(false);
			entity.Property(e => e.ReplyToMessageId).HasMaxLength(450);

			// Relationship: ChatMessage -> ChatSession
			entity.HasOne(d => d.ChatSession)
				.WithMany(p => p.ChatMessages)
				.HasForeignKey(d => d.SessionId)
				.OnDelete(DeleteBehavior.Cascade);

			// Self-Referencing Relationship: ReplyToMessage
			entity.HasOne(d => d.ReplyToMessage)
				.WithMany(p => p.InverseReplyToMessage)
				.HasForeignKey(d => d.ReplyToMessageId)
				.OnDelete(DeleteBehavior.Restrict); // Prevent cycles on delete
		});

		modelBuilder.Entity<ChatbotSession>(entity =>
		{
			entity.ToTable("ChatbotSession");

			entity.HasIndex(e => e.UserId, "IX_ChatbotSession_UserId");
          
			entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.StartedAt).HasColumnType("datetime2");
			entity.Property(e => e.EndedAt).HasColumnType("datetime2");
			entity.Property(e => e.Status)
				.HasMaxLength(50)
              .HasDefaultValue(ChatSessionStatus.Active)
				.HasConversion<string>();

			entity.HasMany(d => d.Messages)
				.WithOne(p => p.Session)
				.HasForeignKey(p => p.SessionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ChatbotMessage>(entity =>
		{
			entity.ToTable("ChatbotMessage");

			entity.HasIndex(e => e.SessionId, "IX_ChatbotMessage_SessionId");
			entity.HasIndex(e => new { e.SessionId, e.CreatedAt }, "IX_ChatbotMessage_SessionId_CreatedAt");
          entity.Property(e => e.Role)
				.HasMaxLength(50)
				.HasConversion<string>();
			entity.Property(e => e.SessionId).HasMaxLength(450);
			entity.Property(e => e.Sender).HasMaxLength(20);
          entity.Property(e => e.Content).HasColumnType("nvarchar(max)");
			entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
			entity.Property(e => e.Status)
				.HasMaxLength(50)
				.HasDefaultValue(ChatMessageStatus.Streaming)
				.HasConversion<string>();
			entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

			entity.HasOne(d => d.Session)
				.WithMany(p => p.Messages)
				.HasForeignKey(d => d.SessionId)
				.OnDelete(DeleteBehavior.Cascade);
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
			entity.ToTable("Event", table =>
			{
				table.HasCheckConstraint("CK_Event_Mode_Online_Location", "[Mode] <> 'Online' OR [LocationId] IS NULL");
				table.HasCheckConstraint("CK_Event_Mode_Offline_MeetingUrl", "[Mode] <> 'Offline' OR [MeetingUrl] IS NULL");
			});

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

			// Mode stored as string (Offline/Online/Hybrid)
			entity.Property(e => e.Mode)
				.HasMaxLength(50)
				.HasConversion<string>();

			// MeetingUrl for online/hybrid events
			entity.Property(e => e.MeetingUrl)
				.HasMaxLength(2000);
			entity.Property(e => e.Status)
				.HasMaxLength(50)
				.HasConversion<string>()
				.HasDefaultValue(EventStatusEnum.Draft);
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

			entity.HasOne(d => d.Location).WithMany(p => p.Events)
				.HasForeignKey(d => d.LocationId)
				.HasConstraintName("FK_Event_Location");

			entity.HasOne(d => d.Topic).WithMany(p => p.Events)
				.HasForeignKey(d => d.TopicId)
				.HasConstraintName("FK_Event_Topic");
		});

		modelBuilder.Entity<EventAgenda>(entity =>
		{
			entity.Property(e => e.EventId).HasMaxLength(450);
			entity.Property(e => e.SessionName).HasMaxLength(255);
			entity.Property(e => e.SpeakerInfo).HasMaxLength(255);

			entity.HasOne(d => d.Event).WithMany(p => p.EventAgenda)
				.HasForeignKey(d => d.EventId)
				.HasConstraintName("FK__EventAgen__Event__08B54D69");

			entity.HasOne(d => d.StudentSpeaker)
				.WithMany(p => p.AgendasAsStudentSpeaker)
				.HasForeignKey(d => d.StudentSpeakerId)
				.OnDelete(DeleteBehavior.ClientSetNull);

			entity.HasOne(d => d.StaffSpeaker)
				.WithMany(p => p.AgendasAsStaffSpeaker)
				.HasForeignKey(d => d.StaffSpeakerId)
				.OnDelete(DeleteBehavior.ClientSetNull);
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
			entity.Property(e => e.QuizSetId).HasMaxLength(450);
			entity.Property(e => e.PassingScore).HasDefaultValue(0);
			entity.Property(e => e.TimeLimit).HasDefaultValue(0);
			entity.Property(e => e.Title).HasMaxLength(255);
			entity.Property(e => e.Type)
				.HasMaxLength(50)
				.HasConversion<string>();
			entity.Property(e=>e.Status).
			HasMaxLength(50).
			HasConversion<string>();
			// Map QuestionSetStatus enum to string (nvarchar(50)) with default 'Available'
			entity.Property(e => e.QuestionSetStatus)
				.IsRequired()
				.HasMaxLength(50)
				.HasConversion<string>()
				.HasDefaultValue(QuestionSetEnum.Available);

		// File for quiz (nullable)
		entity.Property(e => e.FileQuiz)
			.HasColumnType("nvarchar(max)");

		entity.Property(e => e.LiveQuizLink)
			.HasMaxLength(2000);
			entity.Property(e => e.AllowReview)
				.HasDefaultValue(false);

			entity.HasOne(d => d.Event).WithMany(p => p.EventQuizzes)
				.HasForeignKey(d => d.EventId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK__EventQuiz__Event__160F4887");

			entity.HasOne(d => d.QuizSet).WithMany(p => p.EventQuizzes)
				.HasForeignKey(d => d.QuizSetId)
				.HasConstraintName("FK_EventQuiz_QuizSet");
		});

		modelBuilder.Entity<EventQuizQuestion>(entity =>
		{
			entity.ToTable("EventQuizQuestion", table =>
			{
				table.HasCheckConstraint("CK_EventQuizQuestion_OrderIndex_NonNegative", "[OrderIndex] >= 0");
			});

			entity.HasIndex(e => new { e.EventQuizId, e.OrderIndex }, "IX_EventQuizQuestion_EventQuiz_OrderIndex")
				.IsUnique()
				.HasFilter("[DeletedAt] IS NULL AND [EventQuizId] IS NOT NULL");

			entity.HasIndex(e => new { e.EventQuizId, e.QuestionBankId }, "UIX_EventQuizQuestion_EventQuiz_QuestionBank")
				.IsUnique()
				.HasFilter("[DeletedAt] IS NULL AND [EventQuizId] IS NOT NULL AND [QuestionBankId] IS NOT NULL");

			entity.Property(e => e.EventQuizId).HasMaxLength(450);
			entity.Property(e => e.QuestionBankId).HasMaxLength(450);
			entity.Property(e => e.QuestionText).HasMaxLength(1000);
			entity.Property(e => e.OptionA).HasMaxLength(255);
			entity.Property(e => e.OptionB).HasMaxLength(255);
			entity.Property(e => e.OptionC).HasMaxLength(255);
			entity.Property(e => e.OptionD).HasMaxLength(255);
			entity.Property(e => e.CorrectAnswer).HasMaxLength(50);
			entity.Property(e => e.Explanation).HasColumnType("nvarchar(max)");
			entity.Property(e => e.Difficulty)
				.HasDefaultValue(QuestionDifficultyEnum.Medium);
			entity.Property(e => e.ScorePoint).HasDefaultValue(1);
			entity.Property(e => e.OrderIndex).HasDefaultValue(0);

			entity.HasOne(d => d.EventQuiz).WithMany(p => p.EventQuizQuestions)
				.HasForeignKey(d => d.EventQuizId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_EventQuizQuestion_EventQuiz");

			entity.HasOne(d => d.QuestionBank).WithMany(p => p.EventQuizQuestions)
				.HasForeignKey(d => d.QuestionBankId)
				.OnDelete(DeleteBehavior.SetNull)
				.HasConstraintName("FK_EventQuizQuestion_QuestionBank");
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
			entity.Property(x => x.Status)
			    .HasMaxLength(50)
	            .HasConversion<string>()
				.HasDefaultValue(EventWaitlistStatusEnum.Waiting);
		});

		modelBuilder.Entity<ExpenseReceipt>(entity =>
		{
			entity.ToTable("ExpenseReceipt");

			entity.Property(e => e.ActualAmount).HasColumnType("decimal(18, 2)");
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
			entity.Property(x => x.Rating)
				.HasDefaultValue(0.0)
				.HasColumnType("decimal(3, 2)");
			entity.Property(x => x.Status)
				.HasMaxLength(50)
				.HasConversion<string>()
				.HasDefaultValue(FeedbackStatusEnum.NA);
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

		modelBuilder.Entity<QuestionBank>(entity =>
		{
			entity.ToTable("QuestionBank");

			entity.Property(e => e.OrganizerId).HasMaxLength(450);
			entity.Property(e => e.TopicId).HasMaxLength(450);
			entity.Property(e => e.QuestionText).HasMaxLength(1000);
			entity.Property(e => e.OptionA).HasMaxLength(255);
			entity.Property(e => e.OptionB).HasMaxLength(255);
			entity.Property(e => e.OptionC).HasMaxLength(255);
			entity.Property(e => e.OptionD).HasMaxLength(255);
			entity.Property(e => e.CorrectAnswer).HasMaxLength(50);
			entity.Property(e => e.Explanation).HasColumnType("nvarchar(max)");
			entity.Property(e => e.Difficulty)
				.HasMaxLength(50)
				.HasConversion<string>();

			entity.HasOne(d => d.Topic).WithMany(p => p.QuestionBanks)
				.HasForeignKey(d => d.TopicId)
				.HasConstraintName("FK_QuestionBank_Topic");

			entity.HasOne(d => d.Organizer).WithMany(p => p.QuestionBanks)
				.HasForeignKey(d => d.OrganizerId)
				.HasConstraintName("FK_QuestionBank_Organizer");
		});

		modelBuilder.Entity<QuizSet>(entity =>
		{
			entity.ToTable("QuizSet");

			entity.HasIndex(e => new { e.OrganizerId, e.Title }, "UIX_QuizSet_Organizer_Title")
				.IsUnique()
				.HasFilter("[DeletedAt] IS NULL AND [OrganizerId] IS NOT NULL AND [Title] IS NOT NULL");

			entity.Property(e => e.OrganizerId).HasMaxLength(450);
			entity.Property(e => e.TopicId).HasMaxLength(450);
			entity.Property(e => e.Title).HasMaxLength(255);
			entity.Property(e => e.Description).HasMaxLength(1000);
			entity.Property(e => e.FileQuiz).HasColumnType("nvarchar(max)");
			entity.Property(e => e.SharingStatus)
				.HasMaxLength(50)
				.HasConversion<string>()
				.HasDefaultValue(QuizSetVisibilityEnum.Private);
			entity.Property(e => e.IsActive).HasDefaultValue(true);

			entity.HasOne(d => d.Topic).WithMany(p => p.QuizSets)
				.HasForeignKey(d => d.TopicId)
				.HasConstraintName("FK_QuizSet_Topic");

			entity.HasOne(d => d.Organizer).WithMany(p => p.QuizSets)
				.HasForeignKey(d => d.OrganizerId)
				.HasConstraintName("FK_QuizSet_Organizer");
		});

		modelBuilder.Entity<QuizSetQuestion>(entity =>
		{
			entity.ToTable("QuizSetQuestion");

			entity.Property(e => e.QuizSetId).HasMaxLength(450);
			entity.Property(e => e.QuestionBankId).HasMaxLength(450);
			entity.Property(e => e.ScorePoint).HasDefaultValue(1);
			entity.Property(e => e.OrderIndex).HasDefaultValue(0);

			entity.HasIndex(e => new { e.QuizSetId, e.QuestionBankId }, "UIX_QuizSetQuestion_QuizSet_Question")
				.IsUnique()
				.HasFilter("[DeletedAt] IS NULL AND [QuizSetId] IS NOT NULL AND [QuestionBankId] IS NOT NULL");

			entity.HasOne(d => d.QuizSet).WithMany(p => p.QuizSetQuestions)
				.HasForeignKey(d => d.QuizSetId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_QuizSetQuestion_QuizSet");

			entity.HasOne(d => d.QuestionBank).WithMany(p => p.QuizSetQuestions)
				.HasForeignKey(d => d.QuestionBankId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_QuizSetQuestion_QuestionBank");
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
				.HasConversion<string>()
				.HasDefaultValue(SemesterStatusEnum.Upcoming);
			
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

			entity.HasIndex(e => new { e.EventQuizId, e.StudentId }, "UIX_StudentQuizScore_EventQuiz_Student")
				.IsUnique()
				.HasFilter("[DeletedAt] IS NULL AND [EventQuizId] IS NOT NULL AND [StudentId] IS NOT NULL");

			entity.Property(e => e.EventQuizId).HasMaxLength(450);
			entity.Property(e => e.Status)
				.HasMaxLength(50)
				.HasConversion<string>()
				.HasDefaultValue(StudentQuizScoreStatusEnum.NotStarted);
			entity.Property(e => e.StudentId).HasMaxLength(450);

			entity.HasOne(d => d.EventQuiz).WithMany(p => p.StudentQuizScores)
				.HasForeignKey(d => d.EventQuizId)
				.HasConstraintName("FK_StudentQuizScore_EventQuiz");

			entity.HasOne(d => d.Student).WithMany(p => p.StudentQuizScores)
				.HasForeignKey(d => d.StudentId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK__StudentQu__Stude__18EBB532");
		});

		modelBuilder.Entity<StudentAnswer>(entity =>
		{
			entity.ToTable("StudentAnswer");

			entity.HasIndex(e => new { e.StudentQuizScoreId, e.QuestionBankId }, "UIX_StudentAnswer_StudentQuizScore_Question")
				.IsUnique()
				.HasFilter("[DeletedAt] IS NULL AND [StudentQuizScoreId] IS NOT NULL AND [QuestionBankId] IS NOT NULL");

			entity.Property(e => e.StudentQuizScoreId).HasMaxLength(450);
			entity.Property(e => e.QuestionBankId).HasMaxLength(450);
			entity.Property(e => e.SelectedAnswer).HasMaxLength(50);

			entity.HasOne(d => d.StudentQuizScore).WithMany(p => p.StudentAnswers)
				.HasForeignKey(d => d.StudentQuizScoreId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_StudentAnswer_StudentQuizScore");

			entity.HasOne(d => d.QuestionBank).WithMany(p => p.StudentAnswers)
				.HasForeignKey(d => d.QuestionBankId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_StudentAnswer_QuestionBank");
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

			entity.HasIndex(e => new { e.TeamId, e.StudentId }, "UIX_TeamMember_Team_Student")
				.IsUnique()
				.HasFilter("[DeletedAt] IS NULL AND [TeamId] IS NOT NULL AND [StudentId] IS NOT NULL");

			entity.Property(e => e.Role)
				.HasMaxLength(50)
				.HasConversion<string>();

			entity.HasOne(d => d.Student).WithMany(p => p.TeamMembers)
				.HasForeignKey(d => d.StudentId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK__TeamMembe__Stude__7E37BEF6");

			entity.HasOne(d => d.Staff).WithMany(p => p.TeamMembers)
				.HasForeignKey(d => d.StaffId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK__TeamMembe_StaffId");

			entity.HasOne(d => d.Team).WithMany(p => p.TeamMembers)
				.HasForeignKey(d => d.TeamId)
				.HasConstraintName("FK__TeamMembe__TeamI__7D439ABD");
		});

		modelBuilder.Entity<Ticket>(entity =>
		{
			entity.ToTable("Ticket");

			entity.HasIndex(e => new { e.EventId, e.StudentId }, "UIX_Ticket_Event_Student").IsUnique();

			entity.HasIndex(e => e.TicketCode, "UQ__Ticket__598CF7A37C115FF1").IsUnique();

			entity.Property(e => e.Status)
				.HasMaxLength(50)
				.HasConversion<string>()
				.HasDefaultValue(TicketStatusEnum.Registered);
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
			entity.HasIndex(e => new { e.Status, e.ReactivateAt }, "IX_User_Status_ReactivateAt");

			entity.Property(e => e.Email).HasMaxLength(255);
			entity.Property(e => e.FullName).HasMaxLength(255);
			entity.Property(e => e.IsBanned).HasDefaultValue(false);
			entity.Property(e => e.Phone).HasMaxLength(50);
			entity.Property(e => e.ReactivateAt).HasColumnType("datetime2");
			entity.Property(e => e.RoleId).HasMaxLength(450);
			entity.Property(e => e.Status)
				.HasMaxLength(50)
				.HasConversion<string>()
				.HasDefaultValue(UserStatusEnum.Pending);

			// Removed incorrect reversed relationship.
			// Relationships are defined in StudentProfile and StaffProfile configurations.

			entity.HasOne(d => d.Role).WithMany(p => p.Users)
				.HasForeignKey(d => d.RoleId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK__User__RoleId__01142BA1");
		});

		modelBuilder.Entity<Location>(entity =>
		{
			entity.ToTable("Locations");

			entity.Property(x => x.Status)
				.HasMaxLength(50)
				.HasConversion<string>()
				.HasDefaultValue(LocationStatusEnum.Available);

			entity.Property(x => x.Type)
				.HasMaxLength(50)
				.HasConversion<string>();
		});

		ApplyGlobalSoftDeleteQueryFilters(modelBuilder);
		ConfigureBaseEntityAuditProperties(modelBuilder);
		ConfigureBaseEntityConcurrencyTokens(modelBuilder);
		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
