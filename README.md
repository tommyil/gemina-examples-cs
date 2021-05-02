# Gemina API - Quick Implementation Guide - C#



It's fast and easy to implement the Gemina Invoice Analysis.



First, define the API key that you were given, as well as the Client Id:

```c#
public const string API_KEY = "== YOUR API KEY ==";
public const string CLIENT_ID = "== YOUR CLIENT KEY ==";
```



Also define the Gemina URL and endpoints:

```c#
public const string GEMINA_API_URL = "https://api.gemina.co.il/v1";
public const string UPLOAD_IMAGE_URL = "/uploads";
public const string BUSINESS_DOCUMENTS_URL = "/business_documents";
```



If you use a web image (instead of uploading one), then set the URL of the invoice.
In addition, don't forget to update the upload URL to web.

```c#
public const string INVOICE_WEB_URL = "== YOUR INVOICE URL ==";
public const string UPLOAD_WEB_IMAGE_URL = "/uploads/web";
```



Next, start implementing Gemina.

It happens in  2 steps:



------



## Step 1 - Upload Invoice

Here you upload a Business Document (for example: an invoice / credit invoice / receipt, and more) in an image format (we support all the available formats e.g. Jpeg / PNG / PDF).

The server will return the status code **201** to signify that the image has been added and that processing has started.

*If you use the same endpoint again*, you will find out that the server returns a *202 code*, to let you know that the same image has already been accepted, and there's no need to upload it again.

It could also return *409 if a prediction already exists for that image*.

Please note that the image file needs to be encoded as Base64 and then added to the json payload as "*file*".



```c#
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
```



**Alternatively,** you can submit an existing web image here:

```c#
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
```



Here's how you use the above methods:

```c#
// *** Step I:  Upload Image to the Gemina API *** //

var webResponse = await UploadImage(INVOICE_PATH, invoiceId);
// Alternatively - Provide an Image URL instead of uploading -->  var webResponse = await UploadWebImage(INVOICE_WEB_URL, invoiceId);

switch (webResponse.StatusCode)
{
    case 201:
    	// Success - Let's move to the 2nd step
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
```



------



## Step 2 - Get Prediction

Here you retrieve a prediction for the invoice that you uploaded during the first step.



You have to wait until the document finished processing.

Therefore you need to keep asking the server when the prediction is ready.



When it's not yet ready, the server will return either 404 (not found) or 202 (accepted and in progress).

*When Ready, the server will return **200**, with the prediction payload*.



```c#
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
```



```python
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
        	break;

        default:
        	Console.WriteLine($"Failed to retrieve Prediction for Invoice Image {invoiceId}:");
        	PrintJson(webResponse.Data);
        	break;
	}
}
while (webResponse.StatusCode == 202 || webResponse.StatusCode == 404);
```



------



## Output

```bash
Image is still being processed.Sleeping for 1 second before the next attempt.
Image is still being processed.Sleeping for 1 second before the next attempt.
Successfully retrieved Prediction for Invoice Image ex_id_8e585410-4347-4f22-a8e8-7ecc54d59354:
supplier_name: {"value": "\u05d7\u05de\u05e9\u05ea \u05d4\u05e4\u05e1\u05d9\u05dd \u05e7\u05dc\u05d9\u05df \u05d1\u05e2\"\u05de", "confidence": "high", "coordinates": null}
document_number: {"value": 7890, "confidence": "high", "coordinates": {"original": [[548, 385], [667, 389], [665, 434], [547, 430]], "normalized": [[412, 289], [501, 292], [500, 326], [411, 323]]}}
total_amount: {"value": 1572.0, "confidence": "high", "coordinates": {"original": [[208, 1246], [322, 1246], [322, 1275], [208, 1275]], "normalized": [[156, 936], [242, 936], [242, 958], [156, 958]]}}
issue_date: {"value": "31/08/2020", "confidence": "high", "coordinates": {"original": [[1041, 1087], [1178, 1089], [1178, 1115], [1041, 1114]], "normalized": [[782, 817], [885, 818], [885, 838], [782, 837]]}}
document_type: {"value": "invoice", "confidence": "high", "coordinates": null}
timestamp: 1619972590.44228
net_amount: {"value": 1343.59, "confidence": "high", "coordinates": {"original": [[230, 1167], [325, 1167], [325, 1192], [230, 1192]], "normalized": [[173, 877], [244, 877], [244, 896], [173, 896]]}}
external_id: ex_id_8e585410-4347-4f22-a8e8-7ecc54d59354
business_number: {"value": 514713288, "confidence": "high", "coordinates": {"original": [[242, 265], [403, 268], [403, 297], [242, 294]], "normalized": [[182, 199], [303, 201], [303, 223], [182, 221]]}}
created: 2021-05-02T16:23:10.442280
vat_amount: {"value": 228.41, "confidence": "high", "coordinates": {"original": [[251, 1206], [321, 1207], [321, 1231], [251, 1230]], "normalized": [[189, 906], [241, 907], [241, 925], [189, 924]]}}
```



------



## Other Features



#### Pass the Client Tax Id

To facilitate the algorithm's work and increase accuracy, you can pass the Client's Tax Id to the API with each Json call.

This will help to avoid situations where the Client's Tax Id is mistakenly interpreted as the Supplier's Tax Id (or Business Number).



To do so, add the following line to the Dictionary (that is, to your Json):

```c#
`{ "client_business_number", "== Your Client's Business Number =="},`
```

------

Full example:

```c#
var jsonData = new Dictionary<string, object>
            {
                { "client_id", CLIENT_ID },
                { "external_id", invoiceId },
                { "client_business_number", "== Your Client's Business Number =="},
                { "file", Convert.ToBase64String(fileContent)},
            };

```

The `client_business_number` can be represented either by `string` or `int`.



------



The full example code is available here:

[Image / Web Image Upload](https://github.com/tommyil/gemina-examples-cs/blob/master/Program.cs)



For more details, please refer to the [API documentation](https://api.gemina.co.il/swagger/).

You can also contact us [here](mailto:info@gemina.co.il).

