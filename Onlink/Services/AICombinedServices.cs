
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Onlink.Services
{
    public static class TextSimilarityHelper
    {
        public static double CosineSimilarity(Dictionary<string, double> vec1, Dictionary<string, double> vec2)
        {
            double dot = 0, mag1 = 0, mag2 = 0;
            foreach (var key in vec1.Keys)
            {
                if (vec2.TryGetValue(key, out var value2))
                {
                    dot += vec1[key] * value2;
                    mag2 += value2 * value2;
                }
                mag1 += vec1[key] * vec1[key];
            }
            return (mag1 == 0 || mag2 == 0) ? 0 : dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
        }

        public static Dictionary<string, double> Tokenize(string text)
        {
            var words = text.ToLower().Split(new[] { ' ', '.', ',', ';', ':', '-', '_', '/', '\n', '\r', '(', ')', '"', '\'', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            return words.GroupBy(w => w).ToDictionary(g => g.Key, g => (double)g.Count());
        }
    }

    public class OpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAiService(IConfiguration config)
        {
            _apiKey = config["OpenAI:ApiKey"]
                ?? throw new ArgumentNullException("❌ Missing OpenAI API Key in appsettings.json");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> AskGPT(string prompt)
        {
            var request = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
            new { role = "system", content = "أنت مساعد خبير في الموارد البشرية." },
            new { role = "user", content = prompt }
        }
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var result = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(result);

            if (!json.RootElement.TryGetProperty("choices", out var choices) ||
                choices.GetArrayLength() == 0 ||
                !choices[0].TryGetProperty("message", out var message) ||
                !message.TryGetProperty("content", out var contentValue))
            {
                throw new Exception("❌ Invalid or unexpected response from OpenAI API:\n" + result);
            }

            return contentValue.ToString().Trim();
        }

    }

    public class TextSimilarityService
    {
        private readonly MLContext _mlContext;

        public TextSimilarityService()
        {
            _mlContext = new MLContext();
        }

        public double ComputeSimilarity(string text1, string text2)
        {
            var data = new List<TextPair>
            {
                new TextPair { Text = text1 },
                new TextPair { Text = text2 }
            };

            var dataView = _mlContext.Data.LoadFromEnumerable(data);
            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(TextPair.Text));
            var transformedData = pipeline.Fit(dataView).Transform(dataView);
            var features = _mlContext.Data.CreateEnumerable<TransformedText>(transformedData, reuseRowObject: false).ToList();

            if (features.Count < 2) return 0;

            return CosineSimilarity(features[0].Features, features[1].Features);
        }

        private static double CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            float dot = 0, magA = 0, magB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dot += vectorA[i] * vectorB[i];
                magA += vectorA[i] * vectorA[i];
                magB += vectorB[i] * vectorB[i];
            }

            if (magA == 0 || magB == 0)
                return 0;

            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        private class TextPair
        {
            public string Text { get; set; }
        }

        private class TransformedText
        {
            public float[] Features { get; set; }
        }
    }
}

