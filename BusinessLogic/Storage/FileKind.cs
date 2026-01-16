using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Storage
{
    public enum FileKind
    {
        Image,
        Video,
        Audio,
        Document,   // pdf/doc/docx/xls/xlsx/ppt/pptx/odt/ods/odp...
        Text,       // txt/csv/json/xml/md...
        Archive,    // zip/rar/7z/gz/tar...
        Raw         // còn lại (psd, ai, exe, dll, ... nếu cho phép)
    }
}
