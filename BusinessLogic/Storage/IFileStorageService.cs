using DataAccess.Enum;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Storage
{
    public interface IFileStorageService
    {
        Task<IReadOnlyList<UploadedFileResult>> UploadManyAsync(
            IEnumerable<IFormFile> files,
            UploadContext context,
            string ownerUserId,
            CancellationToken ct = default
        );

        /// <summary>
        /// Delete a file from storage using its provider-specific public ID
        /// </summary>
        Task<bool> DeleteAsync(string providerPublicId, string contentType, CancellationToken ct = default);
    }
}
