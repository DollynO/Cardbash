using CardBase.Scripts.Abilities;
using CardBase.Scripts.Cards;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.PlayerScripts;

public partial class PlayerStats : Resource
{
    public int TeamId { get; set; }
    public bool IsDead { get; set; }
    public int Life { get; set; }
    public int Armor { get; set; }
    public int EnergyShield { get; set; }
    public int MovementSpeed { get; set; }
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int BaseCrit { get; set; }
    public int CritChanceBonus { get; set; }
    
    public int CurrentLife;
    
    public Array<Card> Cards = new Array<Card>();
    public Array<Card> SelectedCards = new Array<Card>();

    public Dictionary ToDict()
    {
        var dict = new Dictionary();
        dict.Add("TeamId", TeamId);
        dict.Add("IsDead", IsDead);
        dict.Add("Life", Life);
        dict.Add("Armor", Armor);
        dict.Add("EnergyShield", EnergyShield);
        dict.Add("MovementSpeed", MovementSpeed);
        dict.Add("Strength", Strength);
        dict.Add("Intelligence", Intelligence);
        dict.Add("BaseCrit", BaseCrit);
        dict.Add("CritChanceBonus", CritChanceBonus);
        dict.Add("CurrentLife", CurrentLife);
        
        var cardDict = new Dictionary();
        foreach (var card in Cards)
        {
            cardDict[card.EffectGUID] = (int)card.CardType;
        }
        dict.Add("Cards", cardDict);
        
        var selectedCardDict = new Dictionary();
        foreach (var card in SelectedCards)
        {
            selectedCardDict[card.EffectGUID] = (int)card.CardType;
        }
        dict.Add("SelectedCards", selectedCardDict);
        return dict;
    }

    public static PlayerStats FromDict(Dictionary dict)
    {
        var player = new PlayerStats();
        
        player.TeamId = (int)dict["TeamId"];
        player.IsDead = (bool)dict["IsDead"];
        player.Life = (int)dict["Life"];
        player.Armor = (int)dict["Armor"];
        player.EnergyShield = (int)dict["EnergyShield"];
        player.MovementSpeed = (int)dict["MovementSpeed"];
        player.Strength = (int)dict["Strength"];
        player.Intelligence = (int)dict["Intelligence"];
        player.BaseCrit = (int)dict["BaseCrit"];
        player.CritChanceBonus = (int)dict["CritChanceBonus"];
        player.CurrentLife = (int)dict["CurrentLife"];
        foreach (var (guid, type) in (Dictionary<string, int>)dict["Cards"])
        {
            player.Cards.Add(GlobalCardManager.Instance.GetCard(guid, (CardType)type));
        }
        foreach (var (guid, type) in (Dictionary<string, int>)dict["SelectedCards"])
        {
            player.SelectedCards.Add(GlobalCardManager.Instance.GetCard(guid, (CardType)type));
        }
        
        return player;
    }
    
    public void Update(PlayerStats stats)
    {
        TeamId = stats.TeamId;
        IsDead = stats.IsDead;
        Life = stats.Life;
        Armor = stats.Armor;
        EnergyShield = stats.EnergyShield;
        MovementSpeed = stats.MovementSpeed;
        Strength = stats.Strength;
        Intelligence = stats.Intelligence;
        BaseCrit = stats.BaseCrit;
        CritChanceBonus = stats.CritChanceBonus;
        Cards = stats.Cards;
        SelectedCards = stats.SelectedCards;
        CurrentLife = stats.CurrentLife;
    }

    public void SetDefault()
    {
        if (Life == 0)
        {
            Life = 100;
        }
        
        IsDead = false;
        CurrentLife = Life;
        if (MovementSpeed == 0)
        {
            MovementSpeed = 300;
        }
    }
}