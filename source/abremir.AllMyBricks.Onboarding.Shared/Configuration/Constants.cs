using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace abremir.AllMyBricks.Onboarding.Shared.Configuration
{
    public static class Constants
    {
        public const long TicksPerHundredthOfSecond = 100000;
        public const string HmacAuthenticationScheme = "amx";
        public static readonly Lazy<JsonSerializerOptions> JsonSerializerOptions = new(() =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        });
    }
}
