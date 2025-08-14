using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;//important to add
using System.Threading.Tasks;

namespace IdeaCenterExamPrepOne.Models
{
    internal class ApiResponseDTO
    {
        [JsonPropertyName("msg")]

        public string? Msg { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
