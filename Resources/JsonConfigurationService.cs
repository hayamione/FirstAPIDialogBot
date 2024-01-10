// JsonConfigurationService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace NewDialogBot.Resources
{
    public class JsonConfigurationService
    {
        private readonly IHostEnvironment _environment;

        public JsonConfigurationService(IHostEnvironment environment)
        {
            _environment = environment;
        }

        public Dictionary<string, List<string>> LoadJsonFile(string fileName)
        {
            var filePath = Path.Combine(_environment.ContentRootPath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file {fileName} was not found at {filePath}");
            }

            var jsonContent = File.ReadAllText(filePath);
            var jsonDocument = JsonDocument.Parse(jsonContent);

            var result = new Dictionary<string, List<string>>();

            foreach (var property in jsonDocument.RootElement.EnumerateObject())
            {
                var key = property.Name;
                var values = new List<string>();

                foreach (var element in property.Value.EnumerateArray())
                {
                    values.Add(element.GetString());
                }

                result[key] = values;
            }

            return result;
        }
    }
}
