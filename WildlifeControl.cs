using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// Define a unique namespace for your mod
namespace WildlifeControl
{
    // This class stores the maximum number of wild animals allowed on the map
    public class AnimalLimitSettings : ModSettings
    {
        public int maxWildAnimals = 100;

        // This method is used to save and load the maxWildAnimals value when the game is saved or loaded
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxWildAnimals, "maxWildAnimals", 100);
        }
    }

    // This class is the main entry point for your mod and initializes the AnimalLimitSettings instance
    public class WildlifeControl : Mod
    {
        private AnimalLimitSettings settings;

        public WildlifeControl(ModContentPack content) : base(content)
        {
            settings = GetSettings<AnimalLimitSettings>();
            LongEventHandler.ExecuteWhenFinished(AddGameComponent);
        }

        private void AddGameComponent()
        {
            if (Current.Game != null && Current.Game.GetComponent<AnimalLimitController>() == null)
            {
                Current.Game.components.Add(new AnimalLimitController(Current.Game));
            }
        }

        // This method creates a settings window for the user to adjust the maximum number of wild animals
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Label("Max Wild Animals: " + settings.maxWildAnimals);
            settings.maxWildAnimals = (int)listing.Slider(settings.maxWildAnimals, 1, 1000);
            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        // This method returns the settings category name displayed in the game's mod settings menu
        public override string SettingsCategory()
        {
            return "Wildlife Control";
        }
    }

    // This class is responsible for checking and enforcing the wild animal limit
    public class AnimalLimitController : GameComponent
    {
        private int nextCheckTick;

        public AnimalLimitController(Game game) { }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame >= nextCheckTick)
            {
                CheckAnimalLimit();
                nextCheckTick = Find.TickManager.TicksGame + GenDate.TicksPerDay;
            }
        }

        // This method checks the number of wild animals on each map and removes excess animals
        private static void CheckAnimalLimit()
        {
            AnimalLimitSettings settings = LoadedModManager.GetMod<WildlifeControl>().GetSettings<AnimalLimitSettings>();
            int maxWildAnimals = settings.maxWildAnimals;

            foreach (Map map in Find.Maps)
            {
                // Get a list of wild animals on the map (not belonging to any faction)
                var wildAnimals = map.mapPawns.AllPawnsSpawned.Where(p => p.RaceProps.Animal && p.Faction == null).ToList();

                // Sort animals based on health conditions, age, and species group size
                wildAnimals = wildAnimals
                    .OrderByDescending(a => a.health.summaryHealth.SummaryHealthPercent < 1)
                    .ThenByDescending(a => a.health.hediffSet.hediffs.Any(h => h.IsPermanent()))
                    .ThenByDescending(a => a.ageTracker.AgeBiologicalTicks / a.RaceProps.lifeExpectancy)
                    .ThenByDescending(a => wildAnimals.Count(b => b.def == a.def))
                    .ToList();

                // Remove excess wild animals until the count is below the maximum limit
                while (wildAnimals.Count > maxWildAnimals)
                {
                    Pawn animalToRemove = wildAnimals.First();
                    animalToRemove.Destroy();
                    wildAnimals.Remove(animalToRemove);
                }
            }
        }
    }
}