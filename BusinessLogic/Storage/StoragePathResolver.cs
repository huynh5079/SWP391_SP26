using BusinessLogic.Options;
using DataAccess.Enum;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Storage
{
    public class StoragePathResolver
    {
        private readonly StorageOptions _opt;
        public StoragePathResolver(IOptions<StorageOptions> opt) => _opt = opt.Value;

        public string Resolve(UploadContext context, FileKind kind, string ownerUserId)
        {
            string kindFolder = kind switch
            {
                FileKind.Image => _opt.ImagesFolder,
                FileKind.Video => _opt.VideosFolder,
                FileKind.Audio => _opt.AudioFolder,
                FileKind.Document => _opt.DocumentsFolder,
                FileKind.Text => _opt.TextsFolder,
                FileKind.Archive => _opt.ArchivesFolder,
                _ => _opt.FilesFolder
            };

            var ctx = context switch
            {
                UploadContext.Avatar => _opt.Avatars,
                UploadContext.Certificate => _opt.Certificates,
                UploadContext.Material => _opt.Materials,
                UploadContext.IdentityDocument => _opt.IdentityDocuments,
                UploadContext.Chat => _opt.Chat,
                _ => "others"
            };

            return $"{_opt.BaseFolder}/{ctx}/{ownerUserId}/{kindFolder}";
        }

        private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg",".jpeg",".png",".gif",".webp",".heic",".heif",".bmp",".tiff",".svg" };

        private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4",".mov",".webm",".mkv",".avi" };

        private static readonly HashSet<string> AudioExts = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3",".wav",".aac",".m4a",".flac",".ogg" };

        private static readonly HashSet<string> PdfExts = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf" };

        private static readonly HashSet<string> DocExts = new(StringComparer.OrdinalIgnoreCase)
        { ".doc",".docx",".odt" };

        private static readonly HashSet<string> XlsExts = new(StringComparer.OrdinalIgnoreCase)
        { ".xls",".xlsx",".ods",".csv" };

        private static readonly HashSet<string> PptExts = new(StringComparer.OrdinalIgnoreCase)
        { ".ppt",".pptx",".odp" };

        private static readonly HashSet<string> TextExts = new(StringComparer.OrdinalIgnoreCase)
        { ".txt",".json",".xml",".md",".yaml",".yml",".log" };

        private static readonly HashSet<string> ArchiveExts = new(StringComparer.OrdinalIgnoreCase)
        { ".zip",".rar",".7z",".gz",".tar",".tgz" };

        public static FileKind InferKind(string? contentType, string fileName)
        {
            var ct = (contentType ?? "").ToLowerInvariant();
            var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();

            // Ưu tiên content-type nếu có
            if (ct.StartsWith("image/")) return FileKind.Image;
            if (ct.StartsWith("video/")) return FileKind.Video;
            if (ct.StartsWith("audio/")) return FileKind.Audio;
            if (ct is "application/pdf") return FileKind.Document;
            if (ct.Contains("msword") || ct.Contains("officedocument.wordprocessingml")) return FileKind.Document;
            if (ct.Contains("ms-excel") || ct.Contains("officedocument.spreadsheetml")) return FileKind.Document;
            if (ct.Contains("ms-powerpoint") || ct.Contains("officedocument.presentationml")) return FileKind.Document;
            if (ct is "text/plain" or "text/csv" or "application/json" or "application/xml") return FileKind.Text;
            if (ct is "application/zip" or "application/x-7z-compressed" or "application/x-rar-compressed") return FileKind.Archive;

            // Fallback theo extension
            if (ImageExts.Contains(ext)) return FileKind.Image;
            if (VideoExts.Contains(ext)) return FileKind.Video;
            if (AudioExts.Contains(ext)) return FileKind.Audio;
            if (PdfExts.Contains(ext) || DocExts.Contains(ext) || XlsExts.Contains(ext) || PptExts.Contains(ext)) return FileKind.Document;
            if (TextExts.Contains(ext)) return FileKind.Text;
            if (ArchiveExts.Contains(ext)) return FileKind.Archive;

            return FileKind.Raw;
        }
    }

}
