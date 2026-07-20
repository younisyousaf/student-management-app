using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace StudentManagementApp.WPF.DTOs
{
    public class LoginRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
    }
}
