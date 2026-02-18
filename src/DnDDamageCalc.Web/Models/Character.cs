using ProtoBuf;

namespace DnDDamageCalc.Web.Models;

public class Character
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<CharacterLevel> Levels { get; set; } = [];
}

[ProtoContract]
public class CharacterLevel
{
    [ProtoMember(1)]
    public int LevelNumber { get; set; }

    [ProtoMember(2)]
    public List<Attack> Attacks { get; set; } = [];

    [ProtoMember(3)]
    public LevelResources Resources { get; set; } = new();
}

[ProtoContract]
public class LevelResources
{
    [ProtoMember(1)]
    public bool HasActionSurge { get; set; }

    [ProtoMember(2)]
    public bool HasShieldMaster { get; set; }

    [ProtoMember(3)]
    public int ShieldMasterTopplePercent { get; set; }

    [ProtoMember(4)]
    public bool HasHeroicInspiration { get; set; }

    [ProtoMember(5)]
    public bool HasStudiedAttacks { get; set; }

    [ProtoMember(6)]
    public bool HasExtraActionSurge { get; set; }
}

[ProtoContract]
public class Attack
{
    [ProtoMember(1)]
    public string Name { get; set; } = "";

    [ProtoMember(2)]
    public int HitPercent { get; set; }

    [ProtoMember(3)]
    public int CritPercent { get; set; }

    [ProtoMember(4)]
    public bool MasteryVex { get; set; }

    [ProtoMember(5)]
    public bool MasteryTopple { get; set; }

    [ProtoMember(6)]
    public int FlatModifier { get; set; }

    [ProtoMember(7)]
    public List<DiceGroup> DiceGroups { get; set; } = [];

    [ProtoMember(8)]
    public int TopplePercent { get; set; }

    [ProtoMember(9)]
    public bool MasteryGraze { get; set; }

    [ProtoMember(10)]
    public int GrazeValue { get; set; }

    [ProtoMember(11)]
    public bool RequiresSetup { get; set; }

    [ProtoMember(12)]
    public string ActionType { get; set; } = "action";

    [ProtoMember(13)]
    public int ReactionChancePercent { get; set; } = 100;
}

[ProtoContract]
public class DiceGroup
{
    [ProtoMember(1)]
    public int Quantity { get; set; } = 1;

    [ProtoMember(2)]
    public int DieSize { get; set; } = 6;
}
