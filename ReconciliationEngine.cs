namespace CIEL.Reconciliation.Models;

public sealed class BookingRecord
{
    public string BookingNumber { get; set; } = "";
    public string GuestName { get; set; } = "";
    public DateTime? Arrival { get; set; }
    public DateTime? Departure { get; set; }
    public string Status { get; set; } = "";
    public string NormalizedName { get; set; } = "";
}

public sealed class OperaRecord
{
    public string OperaConf { get; set; } = "";
    public string GuestName { get; set; } = "";
    public DateTime? Arrival { get; set; }
    public DateTime? Departure { get; set; }
    public string Status { get; set; } = "";
    public string RoomNumber { get; set; } = "";
    public string NormalizedName { get; set; } = "";
}

public sealed class ResultRecord
{
    public string BookingNumber { get; set; } = "";
    public string BookingGuest { get; set; } = "";
    public DateTime? BookingArrival { get; set; }
    public DateTime? BookingDeparture { get; set; }
    public string BookingStatus { get; set; } = "";
    public string OperaConf { get; set; } = "";
    public string OperaGuest { get; set; } = "";
    public DateTime? OperaArrival { get; set; }
    public DateTime? OperaDeparture { get; set; }
    public string OperaStatus { get; set; } = "";
    public int MatchScore { get; set; }
    public string MatchMethod { get; set; } = "";
    public string Result { get; set; } = "";
    public string Reason { get; set; } = "";
}
