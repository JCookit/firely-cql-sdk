﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Ncqa.Cql.Model
{
    public static class Models
    {
        private static XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModelInfo));
        private static Lazy<ModelInfo> _Fhir401 = new Lazy<ModelInfo>(() => LoadEmbeddedResource("Fhir401"), true);
        
        public static ModelInfo Fhir401 => _Fhir401.Value;
        
        public static IDictionary<string, ClassInfo> ClassesById(ModelInfo model)
        {
            var baseUrl = model.url;
            var result = model.typeInfo.OfType<ClassInfo>()
                .ToDictionary(classInfo => $"{{{baseUrl}}}{classInfo.name}");
            return result;
        }

        public static ModelInfo LoadFromStream(System.IO.Stream stream)
        {
            return xmlSerializer.Deserialize(stream) as ModelInfo
                ?? throw new ArgumentException($"This resource is not a valid {nameof(ModelInfo)}");
        }

        private static ModelInfo LoadEmbeddedResource(string resourceName)
        {
            var stream = typeof(Models).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new ArgumentException($"Manifest resource stream {resourceName} is not included in this assembly.");
            return LoadFromStream(stream);
        }
    }
}