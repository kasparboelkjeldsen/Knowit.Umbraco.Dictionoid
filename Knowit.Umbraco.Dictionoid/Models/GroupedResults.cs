namespace Knowit.Umbraco.Dictionoid.Models;

public class GroupedResults
{
    public string key { get; set; }
    public int id { get; set; }
    public List<Translation> translations { get; set; }
}

public class Translation
{
    public string lang { get; set; }
    public string text { get; set; }
}