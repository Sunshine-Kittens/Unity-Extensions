using System;
using System.Text;

namespace UnityEngine.Extension.WebAPI.Http
{
    public readonly struct MultipartFormSection
    {
        public string SectionName { get; }
        public byte[] SectionData { get; }
        public string FileName { get; }
        public string ContentType { get; }
        
        public MultipartFormSection(string name, byte[] data, string fileName, string contentType)
        {
            if (data == null || data.Length < 1)
                throw new ArgumentException("Cannot create a multipart form file section without body data");
            if (string.IsNullOrEmpty(fileName))
                throw new  ArgumentException("Cannot create a multipart form file section without a file name");
            if (string.IsNullOrEmpty(contentType))
                throw new  ArgumentException("Cannot create a multipart form file section without a content type");
            
            SectionName = name;
            SectionData = data;
            FileName = fileName;
            ContentType = contentType;
        }
        
        public MultipartFormSection(string name, string data, Encoding dataEncoding, string fileName, string contentType)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Cannot create a multipart form file section without body data");
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Cannot create a multipart form file section without data encoding");
            if (string.IsNullOrEmpty(fileName))
                throw new  ArgumentException("Cannot create a multipart form file section without a file name");
            if (string.IsNullOrEmpty(contentType))
                throw new  ArgumentException("Cannot create a multipart form file section without a content type");
            
            SectionName = name;
            SectionData = dataEncoding.GetBytes(data);
            FileName = fileName;
            ContentType = contentType;
        }
    }
}