using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WildlifeControl
{
    public class AnimalLimitSettings : ModSettings
    {
        public int maxWildAnimals = 100;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxWildAnimals, "maxWildAnimals", 100);
        }
    }

    public class WildlifeControl : Mod
    {
        private readonly AnimalLimitSettings settings;

        public WildlifeControl(ModContentPack content) : base(content)
        {
            settings = new AnimalLimitSettings();
            AddGameComponent();
        }

        private static void AddGameComponent()
        {
            if (Current.Game != null && Current.Game.GetComponent<AnimalLimitController>() == null)
            {
                Current.Game.components.Add(new AnimalLimitController(Current.Game));
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new();
            listing.Begin(inRect);
            listing.Label("Max Wild Animals: " + settings.maxWildAnimals);
            settings.maxWildAnimals = (int)listing.Slider(settings.maxWildAnimals, 1, 1000);
            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Wildlife Control";
        }
    }

    public class WildlifeControlController : GameComponent
    {
        private int nextCheckTick;
        private static bool animalRemoved;

        public WildlifeControlController(Game game) : base(game) { }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame >= nextCheckTick)
            {
                CheckAnimalLimit();
                nextCheckTick = Find.TickManager.TicksGame + (animalRemoved ? 2 : GenDate.TicksPerDay);
            }
        }

        private static void CheckAnimalLimit()
        {
            AnimalLimitSettings settings = LoadedModManager.GetMod<WildlifeControl>().GetSettings<AnimalLimitSettings>();
            int maxWildAnimals = settings.maxWildAnimals;

            foreach (Map map in Find.Maps)
            {
                var wildAnimals = map.mapPawns.AllPawnsSpawned.Where(p => p.RaceProps.Animal && p.Faction == null).ToList();

                wildAnimals = wildAnimals
                    .OrderByDescending(a => a.health.summaryHealth.SummaryHealthPercent < 1)
                    .ThenByDescending(a => a.health.hediffSet.hediffs.Any(h => h.IsPermanent()))
                    .ThenByDescending(a => a.ageTracker.AgeBiologicalTicks / a.RaceProps.lifeExpectancy)
                    .ThenByDescending(a => wildAnimals.Count(b => b.def == a.def))
                    .ToList();

                if (wildAnimals.Count > maxWildAnimals)
                {
                    Pawn animalToRemove = wildAnimals.First();
                    animalToRemove.Destroy();
                    animalRemoved = true;
                }
                else
                {
                    animalRemoved = false;
                }
            }
        }
    }

    public class AnimalLimitController : GameComponent
    {
        public AnimalLimitController(Game game) : base(game)
        {
        }

        // Add your implementation here
    }
}