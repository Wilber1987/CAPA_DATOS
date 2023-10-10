using System;
using System.Collections.Generic;
using System.Text;
using AE.Net.Mail;
using CAPA_DATOS;

namespace CAPA_DATOS.Services
{
    public class FileService
    {
        public static ResponseService upload(string path, ModelFiles Attach)
        {
            try
            {

                string Carpeta = @"\wwwroot\Media\" + path;
                string Ruta = Directory.GetCurrentDirectory() + Carpeta;
                if (!Directory.Exists(Ruta))
                {
                    Directory.CreateDirectory(Ruta);
                }
                if (!IsBase64String(Attach.Value))
                {
                    return new ResponseService()
                    {
                        status = 403,
                        body = new ModelFiles
                        {
                            Value = Attach.Value,
                        },
                        message = "Formato incorrecto, bse64 invalido"
                    };
                }

                byte[] File64 = Convert.FromBase64String(Attach.Value);
                string[] extension = Attach.Type.Split(new string[] { "data:" }, StringSplitOptions.RemoveEmptyEntries);
                string MimeType = "";
                if (extension.Length > 0)
                {
                    MimeType = extension[0];
                }
                string FileType = GetFileType(MimeType);
                Guid Uuid = Guid.NewGuid();
                string FileName = Uuid.ToString() + FileType;
                string FileRoute = Ruta + FileName;
                File.WriteAllBytes(FileRoute, File64);
                string RutaRelativa = Path.GetRelativePath(Directory.GetCurrentDirectory(), FileRoute);

                ModelFiles AttachFiles = new ModelFiles
                {
                    Value = RutaRelativa,
                    Type = FileType
                };

                return new ResponseService()
                {
                    status = 200,
                    body = AttachFiles,
                    message = "Archivo creado correctamente"
                };

            }
            catch (Exception ex)
            {
                return new ResponseService()
                {
                    status = 500,
                    value = ex.ToString(),
                    message = "Error, intentelo nuevamente"
                };
            }

        }

        public static ModelFiles ReceiveFiles(string path, Attachment Attach)
        {
            string Carpeta = @"\wwwroot\Media\" + path;
            string Ruta = Directory.GetCurrentDirectory() + Carpeta;
            if (!Directory.Exists(Ruta))
            {
                Directory.CreateDirectory(Ruta);
            }
            string FileType = GetFileType(Attach.ContentType);
            Guid Uuid = Guid.NewGuid();
            string FileName = Uuid.ToString() + FileType;
            string FileRoute = Ruta + FileName;
            File.WriteAllBytes(FileRoute, Attach.GetData());
            string RutaRelativa = Path.GetRelativePath(Directory.GetCurrentDirectory(), FileRoute);

            ModelFiles AttachFiles = new ModelFiles
            {
                Name = Attach.Filename,
                Value = RutaRelativa,
                Type = FileType
            };
            return AttachFiles;
        }



        public static string GetFileType(string mimeType)
        {
            Dictionary<string, string> TypeFile = new Dictionary<string, string>
            {
                { "image/png;base64,", ".png" },
                { "application/pdf;base64,", ".pdf" },
                { "application/pdf", ".pdf" },
                { "image/jpeg", ".png" },
                { "image/png", ".png" },
                { "png", ".png" },
                { "jpg", ".png" },
                { "jpeg", ".png" },
                { "pdf", ".pdf" },
                { "xlsx", ".xlsx" },
                { "xls", ".xls" },
                { "doc", ".doc" },
                { "docx", ".docx" }
            };

            if (TypeFile.TryGetValue(mimeType, out string Type))
            {
                return Type;
            }
            else
            {
                return ".unknown";
            }
        }
        public static bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
        }
        public static string setImage(string image)
        {
            return ((ModelFiles)FileService.upload("image_tests\\", new ModelFiles { Type = "png", Value = image, Name = "" }).body).Value.Replace("wwwroot", "");
        }
    }


    public class ModelFiles
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? Type { get; set; }
    }

}
