using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace RoyalVillaWeb.Extensions
{
    public static class MultipartFromDataExtension
    {
        public static MultipartFormDataContent ToMultipartFormData(this object obj)
        {
            var formData = new MultipartFormDataContent();
            var properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                var value = property.GetValue(obj);

                if (value == null)
                {
                    continue;
                }

                var propertyName = property.Name;

                if(value is IFormFile file && file.Length > 0)
                {
                    var streamContent = new StreamContent(file.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    formData.Add(streamContent, propertyName,file.FileName);
                }
                else if(value is not IFormFile)
                {
                    formData.Add(new StringContent(value.ToString()!),propertyName);
                }
               
            }
            return formData;
        }
    }
}
