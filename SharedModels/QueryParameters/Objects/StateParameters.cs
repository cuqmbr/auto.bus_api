namespace SharedModels.QueryParameters.Objects;

public class StateParameters : ParametersBase
{
    public const string DefaultFields = "id,name,countryId";
    
    public StateParameters()
    {
        Fields = DefaultFields;
    }
    
    public string? Name { get; set; }
    public int? CountryId { get; set; }
}