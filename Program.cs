using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json.Serialization;

namespace GeminaCSExamples
{
    class Program
    {
        public const string API_KEY = "== YOUR API KEY ==";
        public const string CLIENT_ID = "== YOUR CLIENT KEY ==";

        public const string GEMINA_API_URL = "https://api.gemina.co.il/v1";
        public const string UPLOAD_IMAGE_URL = "/uploads";
        public const string BUSINESS_DOCUMENTS_URL = "/business_documents";

        public const string INVOICE_PATH = "invoice.png";

        public const string INVOICE_WEB_URL = "== YOUR INVOICE URL ==";
        public const string UPLOAD_WEB_IMAGE_URL = "/uploads/web";



        public static async Task Main(string[] args)
        {
            var invoiceId = $"ex_id_{Guid.NewGuid()}";

            
            // *** Step I:  Upload Image to the Gemina API *** //

            var webResponse = await UploadImage(INVOICE_PATH, invoiceId);
            // Alternatively - Provide an Image URL instead of uploading -->  var webResponse = await UploadWebImage(INVOICE_WEB_URL, invoiceId);


            switch (webResponse.StatusCode)
            {
                case 201:
                    // Success - Let's move to the 2nd phase
                    break;

                case 202:
                    Console.WriteLine("Image is already being processed. No need to upload again.");
                    break;

                case 409:
                    Console.WriteLine("A prediction already exists for this image. No need to upload again.");
                    break;

                default:
                    Console.WriteLine("Server returned an error. Operation failed:");
                    PrintJson(webResponse.Data);
                    return;
            }


            // *** Step II:  Upload Image to the Gemina API *** //

            do
            {
                webResponse = await GetPrediction(invoiceId);

                switch(webResponse.StatusCode)
                {
                    case 202:
                        Console.WriteLine("Image is still being processed.Sleeping for 1 second before the next attempt.");
                        Task.Delay(1000).Wait();
                        break;

                    case 404:
                        Console.WriteLine("Can't find image. Let's give it 1 seconds to create before we try again...");
                        Task.Delay(1000).Wait();
                        break;

                    case 200:
                        Console.WriteLine($"Successfully retrieved Prediction for Invoice Image {invoiceId}:");
                        PrintJson(webResponse.Data);
                        Console.WriteLine($"The Prediction Object Data is stored in this object: {webResponse.Prediction}");
                        break;

                    default:
                        Console.WriteLine($"Failed to retrieve Prediction for Invoice Image {invoiceId}:");
                        PrintJson(webResponse.Data);
                        break;
                }
            }
            while (webResponse.StatusCode == 202 || webResponse.StatusCode == 404);
        }

        public static async Task<WebResponse> UploadImage(string invoicePath, string invoiceId)
        {
            var url = $"{GEMINA_API_URL}{UPLOAD_IMAGE_URL}";
            var token = $"Basic {API_KEY}"; //  Mind the space between 'Basic' and the API KEY

            var fileContent = File.ReadAllBytes(invoicePath);
            var jsonData = new Dictionary<string, object>
            {
                { "client_id", CLIENT_ID },
                { "external_id", invoiceId },
                { "file", Convert.ToBase64String(fileContent)},
            };

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(JsonSerializer.Serialize(jsonData), Encoding.UTF8, "application/json"),
                })
                {

                    request.Headers.Add("Authorization", token);

                    using (var response = await httpClient.SendAsync(request))
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        Dictionary<string, object> deserializedObject = null;
                        if (responseContent != null)
                        {
                            deserializedObject = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                        }

                        return new WebResponse
                        {
                            StatusCode = (int)response.StatusCode,
                            Data = deserializedObject
                        };
                    }
                }
            }
        }

        public static async Task<WebResponse> UploadWebImage(string invoiceURL, string invoiceId)
        {
            var url = $"{GEMINA_API_URL}{UPLOAD_WEB_IMAGE_URL}";
            var token = $"Basic {API_KEY}"; //  Mind the space between 'Basic' and the API KEY

            var jsonData = new Dictionary<string, object>
            {
                { "client_id", CLIENT_ID },
                { "external_id", invoiceId },
                { "url", invoiceURL},
            };

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(JsonSerializer.Serialize(jsonData), Encoding.UTF8, "application/json"),
                })
                {

                    request.Headers.Add("Authorization", token);

                    using (var response = await httpClient.SendAsync(request))
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        Dictionary<string, object> deserializedData = null;
                        if (responseContent != null)
                        {
                            deserializedData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                        }

                        return new WebResponse
                        {
                            StatusCode = (int)response.StatusCode,
                            Data = deserializedData
                        };
                    }
                }
            }
        }

        public static async Task<WebResponse> GetPrediction(string imageId)
        {
            var url = $"{GEMINA_API_URL}{BUSINESS_DOCUMENTS_URL}/{imageId}";
            var token = $"Basic {API_KEY}"; //  Mind the space between 'Basic' and the API KEY

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url),
                })
                {

                    request.Headers.Add("Authorization", token);

                    using (var response = await httpClient.SendAsync(request))
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        Dictionary<string, object> deserializedData = null;
                        Prediction deserializedPrediction = null;
                        if (responseContent != null)
                        {
                            deserializedData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                            deserializedPrediction = JsonSerializer.Deserialize<Prediction>(responseContent);

                        }

                        return new WebResponse
                        {
                            StatusCode = (int)response.StatusCode,
                            Data = deserializedData,
                            Prediction = deserializedPrediction
                        };
                    }
                }
            }
        }

        public static void PrintJson(Dictionary<string, object> jsonData)
        {
            if (jsonData != null)
                jsonData.Select(i => $"{i.Key}: {i.Value}").ToList().ForEach(Console.WriteLine);
            else
                Console.WriteLine("Received empty Json response.");
        }
    }

    public class WebResponse
    {
        public int StatusCode { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public Prediction Prediction { get; set; }
    }

    public class Coordinates
    {
        [JsonPropertyName("original")]
        public List<List<int>> Original { get; set; }
        [JsonPropertyName("normalized")]
        public List<List<int>> Normalized { get; set; }
    }

    public abstract class GeneralValue<T>
    {
        [JsonPropertyName("coordinates")]
        public Coordinates Coordinates { get; set; }
        [JsonPropertyName("confidence")]
        public string Confidence { get; set; }
        [JsonPropertyName("value")]
        public abstract T Value { get; set; }
    }

    public class LongValue : GeneralValue<long>
    {
        [JsonPropertyName("value")]
        public override long Value { get; set; }
    }

    public class IntValue : GeneralValue<int>
    {
        [JsonPropertyName("value")]
        public override int Value { get; set; }
    }

    public class DoubleValue : GeneralValue<double>
    {
        [JsonPropertyName("value")]
        public override double Value { get; set; }
    }

    public class StringValue : GeneralValue<string>
    {
        [JsonPropertyName("value")]
        public override string Value { get; set; }
    }

    public class Prediction
    {
        [JsonPropertyName("total_amount")]
        public DoubleValue TotalAmount { get; set; }
        [JsonPropertyName("vat_amount")]
        public DoubleValue VatAmount { get; set; }
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }
        [JsonPropertyName("timestamp")]
        public double TimeStamp { get; set; }
        [JsonPropertyName("primary_document_type")]
        public StringValue PrimaryDocumentType { get; set; }
        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; }
        [JsonPropertyName("currency")]
        public StringValue Currency { get; set; }
        [JsonPropertyName("business_number")]
        public IntValue BusinessNumber { get; set; }
        [JsonPropertyName("issue_date")]
        public StringValue IssueDate { get; set; }
        [JsonPropertyName("document_type")]
        public StringValue DocumentType { get; set; }
        [JsonPropertyName("document_number")]
        public LongValue DocumentNumber { get; set; }
        [JsonPropertyName("net_amount")]
        public DoubleValue NetAmount { get; set; }
        [JsonPropertyName("supplier_name")]
        public StringValue SupplierName { get; set; }
    }
}
