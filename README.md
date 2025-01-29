# .NET C# PDF Processing API

## Overview
This .NET C# application is designed to extract structured data from PDF invoices using iText7 and process the extracted information for storage in a relational database using Entity Framework Core.

## Features
- **File Upload API**: Accepts PDF files and extracts text data.
- **PDF Text Extraction**: Utilizes `PdfTextExtractor` to extract structured content.
- **Regular Expression Parsing**: Identifies key invoice details such as GSTN ID, PAN, invoice number, flight details, etc.
- **Database Storage**: Stores extracted invoice and flight details in a PostgreSQL database using EF Core.
- **Error Handling**: Implements exception handling to manage file parsing errors.
- **Asynchronous Processing**: Uses `async/await` to optimize API performance.

## Technologies Used
- **ASP.NET Core** (for Web API development)
- **Entity Framework Core** (for database interactions)
- **iText7** (for PDF text extraction)
- **Regular Expressions (Regex)** (for structured data extraction)
- **PostgreSQL** (as the database backend)

## Installation
1. **Clone the repository:**
   ```sh
   git clone https://github.com/your-repo/dotnet-pdf-processing.git
   cd dotnet-pdf-processing
   ```

2. **Install dependencies:**
   ```sh
   dotnet restore
   ```

3. **Configure Database:**
   - Update `appsettings.json` with your PostgreSQL connection string.
   - Run migrations:
     ```sh
     dotnet ef database update
     ```

4. **Run the Application:**
   ```sh
   dotnet run
   ```

## API Endpoints
### 1. Upload PDF
- **Endpoint:** `POST /api/values/upload`
- **Request:** Multipart file upload (`IFormFile`)
- **Response:** JSON with extracted data or error details

## Database Schema
### Invoice Table
| Column         | Type    |
|---------------|--------|
| Id            | int    |
| GSTN_ID       | string |
| AckNo         | string |
| PAN           | string |
| InvoiceNumber | string |
| InvoiceDate   | DateTime |
| TotalPayments | int    |
| Tax           | int    |

### Flight Table
| Column    | Type    |
|-----------|--------|
| Id        | int    |
| InvoiceId | int    |
| FlightID  | string |
| FlightDate| DateTime |
| Sector    | string |

## Contributing
Feel free to submit issues or pull requests to improve this project.

## License
This project is licensed under the MIT License.

