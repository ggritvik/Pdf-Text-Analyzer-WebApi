using Microsoft.EntityFrameworkCore;

[Index(nameof(InvoiceNumber), IsUnique = true)]
public class Invoice
{
    public int Id { get; set; }
    public string GSTN_ID { get; set; }
    public string AckNo { get; set; }
    public string PAN { get; set; }
    public string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; }
    public int TotalPayments { get; set; }
    public int Tax { get; set; }

    public ICollection<Flight> Flights { get; set; }
}

[Index(nameof(FlightID), nameof(FlightDate), nameof(Sector), IsUnique = true)] // Ensure Flight entries are unique
public class Flight
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string FlightID { get; set; }
    public DateTime FlightDate { get; set; }
    public string Sector { get; set; }

    public Invoice Invoice { get; set; }
}