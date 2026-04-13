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
        
        public MultipartFormSection(string name, byte[] data, string contentType = null, string fileName = null)
        {
            if (data == null || data.Length < 1)
                throw new ArgumentException("Cannot create a multipart form file section without body data");
            
            SectionName = name;
            SectionData = data;
            FileName = fileName;
            ContentType = contentType;
        }
        
        public MultipartFormSection(string name, string data, Encoding dataEncoding, string contentType = null, string fileName = null)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Cannot create a multipart form file section without body data");
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Cannot create a multipart form file section without data encoding");
            
            SectionName = name;
            SectionData = dataEncoding.GetBytes(data);
            FileName = fileName;
            ContentType = contentType;
        }

        public static MultipartFormSection CreateFromJson(string name, string json)
        {
            return new MultipartFormSection(name, json, Encoding.UTF8, "application/json");
        }

        public static MultipartFormSection CreateFromImagePng(string name, byte[] image)
        {
            return new MultipartFormSection(name, image, "image/png", $"{name}.png");
        }
    }
}