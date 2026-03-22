using BusinessLogic.Options;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DataAccess.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using ResourceType = CloudinaryDotNet.Actions.ResourceType;

namespace BusinessLogic.Storage
{
    public class CloudinaryStorageService : IFileStorageService
    {
        private readonly Cloudinary _cloud;
        private readonly StoragePathResolver _resolver;

        public CloudinaryStorageService(IOptions<CloudinaryOptions> cfg, StoragePathResolver resolver)
        {
            var c = cfg.Value;
            _cloud = new Cloudinary(new Account(c.CloudName, c.ApiKey, c.ApiSecret)) { Api = { Secure = true } };
            _resolver = resolver;
        }

        public async Task<IReadOnlyList<UploadedFileResult>> UploadManyAsync(IEnumerable<IFormFile> files, UploadContext context, string ownerUserId, CancellationToken ct = default)
        {
            var results = new List<UploadedFileResult>();
            foreach (var f in files)
            {
                var result = await UploadSingleAsync(f, context, ownerUserId, ct);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            return results;
        }

        public async Task<UploadedFileResult?> UploadSingleAsync(IFormFile? f, UploadContext context, string ownerUserId, CancellationToken ct = default)
        {
            if (f == null || f.Length == 0) return null;

            using var s = f.OpenReadStream();
            return await UploadSingleInternalAsync(s, f.FileName, f.ContentType, context, ownerUserId, ct);
        }

        public async Task<UploadedFileResult?> UploadSingleAsync(Stream fileStream, string fileName, UploadContext context, string ownerUserId, CancellationToken ct = default)
        {
            if (fileStream == null || fileStream.Length == 0) return null;
            
            // Infer content type from extension for streams if not provided
            var contentType = "application/octet-stream";
            return await UploadSingleInternalAsync(fileStream, fileName, contentType, context, ownerUserId, ct);
        }

        private async Task<UploadedFileResult?> UploadSingleInternalAsync(Stream s, string fileName, string? contentType, UploadContext context, string ownerUserId, CancellationToken ct)
        {
            var kind = StoragePathResolver.InferKind(contentType, fileName);
            var folder = _resolver.Resolve(context, kind, ownerUserId);

            long fileLength = 0;
            try { fileLength = s.Length; } catch { }

            UploadResult res = kind switch
            {
                FileKind.Image => await _cloud.UploadAsync(new ImageUploadParams
                {
                    File = new FileDescription(fileName, s),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                }, ct),

                FileKind.Video or FileKind.Audio => await _cloud.UploadAsync(new VideoUploadParams
                {
                    File = new FileDescription(fileName, s),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                }, ResourceType.Video.ToString(), ct),

                // PDF/DOC/TXT/ZIP and others:
                _ => await _cloud.UploadAsync(new RawUploadParams
                {
                    File = new FileDescription(fileName, s),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                }, ResourceType.Raw.ToString(), ct)
            };

            if (res.StatusCode is not (System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created))
                throw new InvalidOperationException($"Upload fail: {res.Error?.Message}");

            return new UploadedFileResult
            {
                Url = res.SecureUrl?.ToString() ?? res.Url?.ToString() ?? "",
                FileName = fileName,
                ContentType = contentType ?? "application/octet-stream",
                FileSize = fileLength,
                Kind = kind,
                ProviderPublicId = res.PublicId
            };
        }

        /// <summary>
        /// Delete a file from Cloudinary storage using its public ID
        /// </summary>
        public async Task<bool> DeleteAsync(string providerPublicId, string contentType, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(providerPublicId))
                return false;

            try
            {
                var kind = StoragePathResolver.InferKind(contentType, "");

                DeletionResult result = kind switch
                {
                    FileKind.Image => await _cloud.DestroyAsync(new DeletionParams(providerPublicId)
                    {
                        ResourceType = ResourceType.Image
                    }),

                    FileKind.Video or FileKind.Audio => await _cloud.DestroyAsync(new DeletionParams(providerPublicId)
                    {
                        ResourceType = ResourceType.Video
                    }),

                    _ => await _cloud.DestroyAsync(new DeletionParams(providerPublicId)
                    {
                        ResourceType = ResourceType.Raw
                    })
                };

                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                // Log error but don't throw - deletion failure shouldn't block soft delete
                Console.WriteLine($"Cloudinary deletion error for {providerPublicId}: {ex.Message}");
                return false;
            }
        }
    }

}
