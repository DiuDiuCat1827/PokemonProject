using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapArea : MonoBehaviour
{
    [SerializeField] List<PokemonEncounterRecord> wildPokemons;
    [SerializeField] List<PokemonEncounterRecord> wildPokemonsInWater;
    
    [HideInInspector]
    [SerializeField]  int totalChance = 0;

    [HideInInspector]
    [SerializeField]  int totalChanceWater = 0;
    private void OnValidate()
    {
        totalChance = 0;
        foreach(var record in wildPokemons)
        {
            record.chanceLower = totalChance;
            record.chanceUpper = totalChance + record.chancePercentage;

            totalChance = totalChance + record.chancePercentage;
        }

        totalChanceWater = 0;
        foreach(var record in wildPokemonsInWater)
        {
            record.chanceLower = totalChanceWater;
            record.chanceUpper = totalChanceWater + record.chancePercentage;

            totalChanceWater = totalChanceWater + record.chancePercentage;
        }
    }

    private void Start()
    {
        int totalChance = 0;
        foreach(var record in wildPokemons)
        {
            record.chanceLower = totalChance;
            record.chanceUpper = totalChance + record.chancePercentage;

            totalChance = totalChance + record.chancePercentage;
        }
    }

    public Pokemon GetRandomWildPokemon(BattleTrigger trigger)
    {
        var pokemonList = (trigger == BattleTrigger.LongGrass) ? wildPokemons : wildPokemonsInWater;

        int ranVal = Random.Range(1, 101);
        var pokemonRecord = pokemonList.First( p => ranVal >= p.chanceLower && ranVal <= p.chanceUpper);

        var levelRange = pokemonRecord.levelRange;
        int level = levelRange.y ==0 ? levelRange.x:Random.Range(levelRange.x, levelRange.y + 1);
        
        var wildPokemon = new Pokemon(pokemonRecord.pokemon,level);
        //var wildPokemon =  wildPokemons[Random.Range(0, wildPokemons.Count)];
        wildPokemon.Init();
        return wildPokemon;
      
    }
}

[System.Serializable]
public class PokemonEncounterRecord
{
    public PokemonBase pokemon;
    public Vector2Int levelRange;
    public int chancePercentage;

    public int chanceLower { get;set;}

    public int chanceUpper { get; set;}
}
