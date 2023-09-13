namespace BCore.ClanSystem;

public class ClanLevel
{
    public uint BonusCraftingSpeed;
    public uint BonusGatheringAnimal;
    public uint BonusGatheringRock;
    public uint BonusGatheringWood;
    public uint BonusMembersDamage;
    public uint BonusMembersDefense;
    public uint BonusMembersPayMurder;
    public uint CurrencyTax;
    public bool FlagAbbr;
    public bool FlagFFire;
    public bool FlagHouse;
    public bool FlagMotd;
    public bool FlagTax;
    public int Id;
    public int MaxMembers;
    public ulong RequireCurrency;
    public ulong RequireExperience;
    public int RequireLevel;
    public uint WarpCountdown;
    public uint WarpTimeWait;

    public ClanLevel(int level = 0)
    {
        Id = level;
        RequireLevel = -1;
        RequireCurrency = 0;
        RequireExperience = 0;
        MaxMembers = 5;
        CurrencyTax = 10;
        WarpTimeWait = 30;
        WarpCountdown = 3600;
        FlagMotd = false;
        FlagAbbr = false;
        FlagFFire = false;
        FlagTax = false;
        FlagHouse = false;
        BonusCraftingSpeed = 0;
        BonusGatheringWood = 0;
        BonusGatheringRock = 0;
        BonusGatheringAnimal = 0;
        BonusMembersPayMurder = 0;
        BonusMembersDefense = 0;
        BonusMembersDamage = 0;
    }
}