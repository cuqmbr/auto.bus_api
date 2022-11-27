using System.ComponentModel.DataAnnotations;

namespace SharedModels.DataTransferObjects;

public class TicketDto : CreateTicketDto
{
    public int Id { get; set; }
    
    [DataType(DataType.DateTime)]
    public DateTime PurchaseDateTimeUtc { get; set; }
    public bool IsReturned { get; set; } = false;
    public bool IsMissed { get; set; } = false;
}

public class CreateTicketDto
{
    [Required]
    public string UserId { get; set; } = null!;
    
    [Required]
    public int VehicleEnrollmentId { get; set; }
}

public class UpdateTicketDto
{
    [Required]
    public int Id { get; set; }
    
    public string? UserId { get; set; } = null!;
    public int? VehicleEnrollmentId { get; set; }
    
    [DataType(DataType.DateTime)]
    public DateTime PurchaseDateTimeUtc { get; set; }
    public bool IsReturned { get; set; } = false;
    public bool IsMissed { get; set; } = false;
}