using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Topic : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Navigation property
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    public virtual ICollection<QuestionBank> QuestionBanks { get; set; } = new List<QuestionBank>();

    public virtual ICollection<QuizSet> QuizSets { get; set; } = new List<QuizSet>();
}
