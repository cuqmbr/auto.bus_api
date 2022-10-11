using System.ComponentModel.DataAnnotations.Schema;
using SharedModels.DataTransferObjects;

namespace Server.Models;

public class RouteAddress
{
    [ForeignKey("RouteId")]
    public int RouteId { get; set; }
    public Route Route { get; set; } = null!;
    
    [ForeignKey("AddressId")]
    public int AddressId { get; set; }
    public Address Address { get; set; } = null!;
    
    public int Order { get; set; }
    public TimeSpan TimeSpanToNextCity { get; set; }
    public TimeSpan WaitTimeSpan { get; set; }
    public double CostToNextCity { get; set; }
}