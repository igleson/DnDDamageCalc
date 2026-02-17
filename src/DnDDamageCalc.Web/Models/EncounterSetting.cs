namespace DnDDamageCalc.Web.Models;

public class EncounterSetting
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<CombatDefinition> Combats { get; set; } = [];
}

public class CombatDefinition
{
    public int Rounds { get; set; } = 1;
    public bool ShortRestAfter { get; set; }
}
