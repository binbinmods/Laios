using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using UnityEngine;
using static Laios.CustomFunctions;
using static Laios.Plugin;
using static Laios.DescriptionFunctions;
using static Laios.CharacterFunctions;
using System.Text;
using TMPro;
using Obeliskial_Essentials;
using System.Data.Common;
using BepInEx;

namespace Laios
{
    [HarmonyPatch]
    internal class Traits
    {
        // list of your trait IDs

        public static string[] simpleTraitList = ["trait0", "trait1a", "trait1b", "trait2a", "trait2b", "trait3a", "trait3b", "trait4a", "trait4b"];

        public static string[] myTraitList = simpleTraitList.Select(trait => subclassname.ToLower() + trait).ToArray(); // Needs testing

        public static string trait0 = myTraitList[0];
        // static string trait1b = myTraitList[1];
        public static string trait2a = myTraitList[3];
        public static string trait2b = myTraitList[4];
        public static string trait4a = myTraitList[7];
        public static string trait4b = myTraitList[8];

        // public static int infiniteProctection = 0;
        // public static int bleedInfiniteProtection = 0;
        public static bool isDamagePreviewActive = false;

        public static bool isCalculateDamageActive = false;
        public static int infiniteProctection = 0;

        public static string debugBase = "Binbin - Testing " + heroName + " ";


        [HarmonyPrefix]
        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static bool DoTrait(Enums.EventActivation _theEvent, string _trait, Character _character, Character _target, int _auxInt, string _auxString, CardData _castedCard, ref Trait __instance)
        {
            if ((UnityEngine.Object)MatchManager.Instance == (UnityEngine.Object)null)
                return false;
            if (Content.medsCustomTraitsSource.Contains(_trait) && myTraitList.Contains(_trait))
            {
                DoCustomTrait(_trait, ref __instance, ref _theEvent, ref _character, ref _target, ref _auxInt, ref _auxString, ref _castedCard);
                return false;
            }
            return true;
        }

        public static void DoCustomTrait(string _trait, ref Trait __instance, ref Enums.EventActivation _theEvent, ref Character _character, ref Character _target, ref int _auxInt, ref string _auxString, ref CardData _castedCard)
        {
            // get info you may need
            TraitData traitData = Globals.Instance.GetTraitData(_trait);
            List<CardData> cardDataList = [];
            List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
            Hero[] teamHero = MatchManager.Instance.GetTeamHero();
            NPC[] teamNpc = MatchManager.Instance.GetTeamNPC();

            if (!IsLivingHero(_character))
            {
                return;
            }

            if (_trait == trait0)
            {
                // Gain 1 zeal every turn.
                _character.SetAuraTrait(_character, "zeal", 1);
            }


            else if (_trait == trait2a)
            {
                // trait2a
                // Evasion +1. 
                // Evasion on you stacks and increases All Damage by 1 per charge. 
                // When you play a Defense card, gain 1 Energy and Draw 1. (2 times/turn)
                string traitName = traitData.TraitName;
                string traitId = _trait;

            }



            else if (_trait == trait2b)
            {
                // trait2b:
                // When you play an Attack or Spell, reduce the cost of your highest cost Defense by 1 until discarded. 
                // When you play a Defense, reduce the cost of your highest cost Attack or Spell by 1 until discarded.
                string traitName = traitData.TraitName;
                string traitId = _trait;
                int bonusActivations = _character.HaveTrait(trait4a) ? 1 : 0;
                DualityCardType(ref _character, ref _castedCard, [Enums.CardType.Attack, Enums.CardType.Spell], [Enums.CardType.Defense], traitId, bonusActivations: bonusActivations);

            }

            else if (_trait == trait4a)
            {
                // trait 4a;
                // Zeal on heroes is not lost at end of turn. When you hit an enemy, 
                // suffer 2 burn. 
                // Enforcer Duality can activate an extra time.                
                string traitName = traitData.TraitName;
                string traitId = _trait;
                _character.SetAuraTrait(_character, "burn", 2);
                LogDebug($"Handling Trait {traitId}: {traitName}");
            }

            else if (_trait == trait4b)
            {
                // trait 4b:
                // When you play a Defense, add a random Defense that costs 1 more to your hand. 
                // This card costs 0 and Vanish. (1 time/turn). 
                string traitName = traitData.TraitName;
                string traitId = _trait;
                if (CanIncrementTraitActivations(traitId) && _castedCard.HasCardType(Enums.CardType.Defense))
                {
                    LogDebug($"Handling Trait {traitId}: {traitName}");
                    int cost = MatchManager.Instance.energyJustWastedByHero + 1;
                    bool vanish = true;
                    // int costReduction = 3;
                    bool costZero = true;
                    bool permanentCostReduction = true;
                    Enums.HeroClass cardClass = MatchManager.Instance.GetRandomIntRange(1, 100) % 2 == 0 ? Enums.HeroClass.Warrior : Enums.HeroClass.Healer;
                    string randomCard = "";
                    while (randomCard.IsNullOrWhiteSpace())
                    {
                        randomCard = GetRandomCardOfTypeAndCost(cardClass, [Enums.CardType.Defense], cost);//, costReduction: -3, vanish: false, permanentCostReduction: true);
                        cost -= 1;
                        if (cost < 0)
                        {
                            break;
                        }
                    }
                    if (randomCard.IsNullOrWhiteSpace())
                    {
                        LogError($"No card found for trait {traitId} with cost {cost}");
                        return;
                    }
                    AddCardToHand(randomCard, randomlyUpgraded: false, vanish: vanish, costZero: costZero, permanentCostReduction: permanentCostReduction);

                    IncrementTraitActivations(traitId);
                }
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), "GlobalAuraCurseModificationByTraitsAndItems")]
        // [HarmonyPriority(Priority.Last)]
        public static void GlobalAuraCurseModificationByTraitsAndItemsPostfix(ref AtOManager __instance, ref AuraCurseData __result, string _type, string _acId, Character _characterCaster, Character _characterTarget)
        {
            // LogInfo($"GACM {subclassName}");

            Character characterOfInterest = _type == "set" ? _characterTarget : _characterCaster;
            string traitOfInterest;
            switch (_acId)
            {
                // trait2a:
                // Crack on monsters increases Holy Damage taken by 1 per charge. 
                // Sanctify reduces Blunt resistance by 0.5% per charge.

                // trait2b:
                // 

                // trait 4a;
                // 

                // trait 4b:
                // 
                case "zeal":
                    traitOfInterest = trait4a;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.Heroes))
                    {
                        __result.ConsumedAtTurn = false;
                        __result.ConsumedAtTurnBegin = false;
                    }
                    break;
                case "sanctify":
                    traitOfInterest = trait2a;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.Monsters))
                    {
                        __result = __instance.GlobalAuraCurseModifyResist(__result, Enums.DamageType.Blunt, 0, -0.5f);
                    }
                    break;
                case "crack":
                    traitOfInterest = trait2a;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.Monsters))
                    {
                        // __result = __instance.GlobalAuraCurseModifyDamage(__result, Enums.DamageType.Holy, 0, 1, 0);
                        __result.IncreasedDamageReceivedType2 = Enums.DamageType.Holy;
                        __result.IncreasedDirectDamageReceivedPerStack2 = 1;
                    }
                    break;
            }
        }



    }
}

