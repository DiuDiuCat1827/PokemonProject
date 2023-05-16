using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Pokemon", menuName = "Pokemon/Create new pokemon")]
public class PokemonBase : ScriptableObject
{
    [SerializeField] string name;

    [TextArea]
    [SerializeField] string description;

    [SerializeField] Sprite frontSprite;

    [SerializeField] Sprite backSprite;

    [SerializeField] PokemonType type1;

    [SerializeField] PokemonType type2;

    // base status
    [SerializeField] int maxHP;
    [SerializeField] int attack;
    [SerializeField] int defense;
    [SerializeField] int spAttack;
    [SerializeField] int spDefense;
    [SerializeField] int speed;

    [SerializeField] List<LearnableMove> learnableMoves;


    public string GetName()
    {
        return name;
    }

    public string Name
    {
        get { return name; }
    }

    public string Description
    {
        get { return description; }
    }

    public int MaxHP
    {
        get { return maxHP; }
    }

    public int Attack
    {
        get { return attack; }
    }

    public int Defense
    {
        get { return defense; }
    }

    public int SpAttack
    {
        get { return spAttack; }
    }

    public int SpDefense
    {
        get { return spDefense; }
    }

    public int Speed
    {
        get { return speed; }
    }

    public List<LearnableMove> LearnableMoves {
        get { return learnableMoves; }
    }

    public Sprite BackSprite
    {
        get { return backSprite; }
    }

    public Sprite FrontSprite
    {
        get { return frontSprite; }
    }

    public PokemonType Type1
    {
        get { return type1; }
    }

    public PokemonType Type2
    {
        get { return type2; }
    }
}

[System.Serializable]
public class LearnableMove
{
    [SerializeField] MoveBase moveBase;
    [SerializeField] int level;

    public MoveBase Base
    {
        get { return moveBase; }
    }

    public int Level
    {
        get { return level; }
    }
}

public enum PokemonType
{
    Normal,
    Fire,
    Water,
    Electric,
    Grass,
    Poison,
    Ice,
    Fighting,
    Ground,
    Flying,
    Psychic,
    Bug,
    Rock,
    Ghost,
    Dragon,
    None,
}

public enum Stat
{
    Attack,
    Defense,
    SpAttack,
    SpDefense,
    Speed,

    //命中等级和闪避等级
    Accuracy,
    Evasion
}

public class TypeChart {
    static float[][] chart = {
        //                       NOR   FIR   WAT   ELE   GRA   ICE   FIG   POI   GRO   FLY   PSY   BUG   ROC   GHO   DRA   DAR   STE   FAI
       /*Normal*/   new float[]{ 1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Fire*/     new float[]{ 1f, 0.5f, 0.5f,   1f,   2f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Water*/    new float[]{ 1f,   2f, 0.5f,   2f, 0.5f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Electric*/ new float[]{ 1f,   1f,   2f, 0.5f, 0.5f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Grass*/    new float[]{ 1f, 0.5f,   2f,   2f, 0.5f,   1f,   1f, 0.5f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Ice*/      new float[]{ 1f, 0.5f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Fighting*/ new float[]{ 2f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Poison*/   new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Ground*/   new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Flying*/   new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Psychic*/  new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Bug*/      new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Rock*/     new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Ghost*/    new float[]{ 0f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Dragon*/   new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Dark*/     new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Steel*/    new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
       /*Fairy*/    new float[]{ 1f,   1f,   1f,   1f,   2f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f},
    }; 
    
    public static float GetEffectiveness(PokemonType attackType,PokemonType defenseType)
    {
        if (attackType == PokemonType.None || defenseType == PokemonType.None)
        {
            return 1;
        }

        int row = (int)attackType ;
        int col = (int)defenseType ;
        return chart[row][col];
    }
}
