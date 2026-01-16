using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Options
{
    public class StorageOptions
    {
        public string BaseFolder { get; set; } = "tpedu";
        public string ImagesFolder { get; set; } = "images";
        public string VideosFolder { get; set; } = "videos";
        public string AudioFolder { get; set; } = "audio";
        public string DocumentsFolder { get; set; } = "documents";
        public string TextsFolder { get; set; } = "texts";
        public string ArchivesFolder { get; set; } = "archives";
        public string FilesFolder { get; set; } = "files";

        public string Avatars { get; set; } = "avatars";
        public string Certificates { get; set; } = "certificates";
        public string Materials { get; set; } = "materials";
        public string IdentityDocuments { get; set; } = "identity";
        public string Chat { get; set; } = "chat";
    }
}
