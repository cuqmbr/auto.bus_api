namespace SharedModels.QueryParameters.Objects;

public class AddressParameters : ParametersBase
{
    public const string DefaultFields = "id,name,cityId";
    
    public AddressParameters()
    {
        Fields = DefaultFields;
    }
    
    public string? Name { get; set; }
    public int? CityId { get; set; }
}