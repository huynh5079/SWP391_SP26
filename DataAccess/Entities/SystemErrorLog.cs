using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class SystemErrorLog: BaseEntity
{
    //public string Id { get; set; } = null!;

    public int? StatusCode { get; set; }

    public string? ExceptionType { get; set; }

    public string? ExceptionMessage { get; set; }

    public string? StackTrace { get; set; }

    public string? Source { get; set; }

    public string? UserId { get; set; }

    //public DateTime? CreatedAt { get; set; }
}
