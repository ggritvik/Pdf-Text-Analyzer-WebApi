using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf.Colorspace;
using iText.StyledXmlParser.Jsoup.Select;
using System.Globalization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [HttpPost("Upload")]
        public async Task<IActionResult> UploadFile(IFormFile pdfFile, [FromServices] AppDbContext dbContext)
        {
            try
            {
                if (pdfFile == null || pdfFile.Length == 0)
                {
                    return BadRequest("No file provided.");
                }

                using var memoryStream = new MemoryStream();
                await pdfFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Extract text
                string extractedText = ExtractTextFromPdf(memoryStream);

                // Extract fields
                var extractedFieldsSingle = ExtractSpecificFieldsSingle(extractedText) as Dictionary<string, object>;
                var extractedFieldsMultiple = ExtractSpecificFieldsMultiple(extractedText) as Dictionary<string, object>;

                // Validate single fields
                if (extractedFieldsSingle == null || !extractedFieldsSingle.Any())
                {
                    throw new Exception("Failed to extract single fields.");
                }

                // Save single fields
                var invoice = new Invoice
                {
                    GSTN_ID = extractedFieldsSingle["GSTN ID"]?.ToString(),
                    AckNo = extractedFieldsSingle["AckPattern"]?.ToString(),
                    PAN = extractedFieldsSingle["panPattern"]?.ToString(),
                    InvoiceNumber = extractedFieldsSingle["invoiceNumberPattern"]?.ToString(),
                    InvoiceDate = DateTime.ParseExact(
                        extractedFieldsSingle["invoiceDatePattern"]?.ToString() ?? string.Empty,
                        "dd MMM yyyy",
                        CultureInfo.InvariantCulture
                    ),
                    TotalPayments = int.Parse(extractedFieldsSingle["totalPaymentsPattern"]?.ToString() ?? "0"),
                    Tax = int.Parse(extractedFieldsSingle["taxPattern"]?.ToString() ?? "0")
                };

                dbContext.Invoices.Add(invoice);
                await dbContext.SaveChangesAsync();

                // Save multiple fields
                if (extractedFieldsMultiple.TryGetValue("flighPattern", out var flightDetailsList))
                {
                    var flightDetails = flightDetailsList as List<Dictionary<string, string>>;
                    if (flightDetails != null)
                    {
                        foreach (var detail in flightDetails)
                        {
                            dbContext.Flights.Add(new Flight
                            {
                                InvoiceId = invoice.Id,
                                FlightID = detail["Flight ID"],
                                FlightDate = DateTime.ParseExact(detail["Flight Date"], "dd MMM yyyy", CultureInfo.InvariantCulture),
                                Sector = detail["Sector"]
                            });
                        }

                        await dbContext.SaveChangesAsync();
                    }
                }

                return Ok("Data successfully saved to the database.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        private readonly AppDbContext _dbContext;

        public ValuesController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        private string ExtractTextFromPdf(Stream pdfStream)
        {
            using var pdfReader = new PdfReader(pdfStream);
            using var pdfDocument = new PdfDocument(pdfReader);

            var text = string.Empty;
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                text += PdfTextExtractor.GetTextFromPage(page) + "\n";
            }

            return text;
        }

        private object ExtractSpecificFieldsSingle(string text)
        {
            // Define regex patterns for fields
            var patterns = new Dictionary<string, string>
            {
                { "GSTN ID", @"GSTN\s*ID\s*:\s*([\w\d]{15})" },
                { "AckPattern", @"Acknowledge\s*No\s*:\s*([0-9]{15})"},
                { "panPattern" , @"PAN\s*:\s*([A-Z0-9]{10})"},
                { "invoiceNumberPattern" , @"Invoice\s*Number\s*:\s*([\w\d\-]{8})"},
                { "invoiceDatePattern", @"Invoice\s*Date\s*:\s*(\d{1,2}\s*(?:[A-Za-z]+|\d{1,2})[\s\-\/]?(?:\d{4})?)"},
                { "totalPaymentsPattern" , @"Total\s*Payments\s*:\s*([0-9]+)"},
                { "taxPattern" , @"Taxes\s*([0-9]+)"}
            };

            // Extract fields using regex

            var extractedFields = new Dictionary<string, object>();
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern.Value);
                if (match.Success)
                {
                    extractedFields[pattern.Key] = match.Groups[1].Value;
                }
            }
            return extractedFields;
        }

        private object ExtractSpecificFieldsMultiple(string text)
        {
            // Define regex patterns for fields
            var patterns = new Dictionary<string, string>
            {
                { "flighPattern" , @"([A-Z0-9]{2}\s\d{5})\s(\d{2}\s[A-Za-z]{3}\s\d{4})\s\d{2}:\d{2}\s([A-Z]{3}-[A-Z]{3})" }
            };

            // Extract fields using regex
            var extractedFields = new Dictionary<string, object>();
            foreach (var pattern in patterns)
            {
                if (pattern.Key == "flighPattern")
                {
                    var matches = Regex.Matches(text, pattern.Value);
                    var flightDetails = new List<Dictionary<string, string>>();

                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            flightDetails.Add(new Dictionary<string, string>
                    {
                        { "Flight ID", match.Groups[1].Value },
                        { "Flight Date", match.Groups[2].Value },
                        { "Sector", match.Groups[3].Value }
                    });
                        }
                    }

                    if (flightDetails.Count > 0)
                    {
                        extractedFields[pattern.Key] = flightDetails;
                    }
                }
                else
                {
                    var matches = Regex.Matches(text, pattern.Value);
                    var uniqueValues = new HashSet<string>();

                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            uniqueValues.Add(match.Groups[1].Value);
                        }
                    }

                    if (uniqueValues.Count > 0)
                    {
                        extractedFields[pattern.Key] = uniqueValues.ToList();
                    }
                }
            }
            return extractedFields;
        }
    }
}
