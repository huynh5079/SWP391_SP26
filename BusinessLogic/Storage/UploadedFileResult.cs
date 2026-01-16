using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Storage
{
    public class UploadedFileResult
    {
        public string Url { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public long FileSize { get; set; }
        public FileKind Kind { get; set; }
        public string? ProviderPublicId { get; set; }
    }
}
