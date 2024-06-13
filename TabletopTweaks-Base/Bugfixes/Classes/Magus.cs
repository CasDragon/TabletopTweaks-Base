﻿using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Linq;
using TabletopTweaks.Core.NewComponents.Prerequisites;
using TabletopTweaks.Core.Utilities;
using static TabletopTweaks.Base.Main;
using static TabletopTweaks.Core.MechanicsChanges.ActivatableAbilitySpendLogic;

namespace TabletopTweaks.Base.Bugfixes.Classes {
    static class Magus {
        [HarmonyPatch(typeof(BlueprintsCache), "Init")]
        static class Magus_AlternateCapstone_Patch {
            static bool Initialized;
            [HarmonyPriority(Priority.Last)]
            static void Postfix() {
                if (Initialized) return;
                Initialized = true;
                PatchAlternateCapstone();
            }
            static void PatchAlternateCapstone() {
                if (Main.TTTContext.Fixes.AlternateCapstones.IsDisabled("Magus")) { return; }

                var TrueMagusFeature = BlueprintTools.GetBlueprintReference<BlueprintFeatureBaseReference>("789c7539ba659174db702e18d7c2d330");
                var MagusAlternateCapstone = NewContent.AlternateCapstones.Magus.MagusAlternateCapstone.ToReference<BlueprintFeatureBaseReference>();

                TrueMagusFeature.Get().TemporaryContext(bp => {
                    bp.AddComponent<PrerequisiteInPlayerParty>(c => {
                        c.CheckInProgression = true;
                        c.HideInUI = true;
                        c.Not = true;
                    });
                    bp.HideNotAvailibleInUI = true;
                    TTTContext.Logger.LogPatch(bp);
                });
                ClassTools.Classes.MagusClass.TemporaryContext(bp => {
                    bp.Progression.UIGroups
                        .Where(group => group.m_Features.Any(f => f.deserializedGuid == TrueMagusFeature.deserializedGuid))
                        .ForEach(group => group.m_Features.Add(MagusAlternateCapstone));
                    bp.Progression.LevelEntries
                        .Where(entry => entry.Level == 20)
                        .ForEach(entry => entry.m_Features.Add(MagusAlternateCapstone));
                    bp.Archetypes.ForEach(a => {
                        a.RemoveFeatures
                            .Where(remove => remove.Level == 20)
                            .Where(remove => remove.m_Features.Any(f => f.deserializedGuid == TrueMagusFeature.deserializedGuid))
                            .ForEach(remove => remove.m_Features.Add(MagusAlternateCapstone));
                    });
                    TTTContext.Logger.LogPatch("Enabled Alternate Capstones", bp);
                });
            }
        }
        [HarmonyPatch(typeof(BlueprintsCache), "Init")]
        static class BlueprintsCache_Init_Patch {
            static bool Initialized;

            static void Postfix() {
                if (Initialized) return;
                Initialized = true;
                TTTContext.Logger.LogHeader("Patching Magus Resources");

                PatchBase();
                PatchEldritchScion();
                PatchHexcrafter();
                PatchSwordSaint();
            }
            static void PatchBase() {
                PatchSpellCombatDisableImmediatly();
                PatchArcaneWeaponProperties();
                PatchFighterTraining();

                void PatchSpellCombatDisableImmediatly() {
                    if (TTTContext.Fixes.Magus.Base.IsDisabled("SpellCombatDisableImmediatly")) { return; }

                    var SpellCombatAbility = BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("8898a573e8a8a184b8186dbc3a26da74");
                    var SpellStrikeAbility = BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("e958891ef90f7e142a941c06c811181e");

                    SpellCombatAbility.DeactivateImmediately = true;
                    SpellStrikeAbility.DeactivateImmediately = true;

                    TTTContext.Logger.LogPatch("Patched", SpellCombatAbility);
                    TTTContext.Logger.LogPatch("Patched", SpellStrikeAbility);
                }
                void PatchFighterTraining() {
                    if (TTTContext.Fixes.Magus.Base.IsDisabled("FighterTraining")) { return; }

                    var FighterTraining = BlueprintTools.GetBlueprint<BlueprintFeature>("2b636b9e8dd7df94cbd372c52237eebf");
                    QuickFixTools.ReplaceClassLevelsForPrerequisites(FighterTraining, TTTContext, FeatureGroup.Feat);

                    TTTContext.Logger.LogPatch(FighterTraining);
                }
                void PatchArcaneWeaponProperties() {
                    if (TTTContext.Fixes.Magus.Base.IsDisabled("AddMissingArcaneWeaponEffects")) { return; }

                    var ArcaneWeaponFlamingBurstChoice_TTT = BlueprintTools.GetModBlueprint<BlueprintActivatableAbility>(TTTContext, "ArcaneWeaponFlamingBurstChoice_TTT");
                    var ArcaneWeaponIcyBurstChoice_TTT = BlueprintTools.GetModBlueprint<BlueprintActivatableAbility>(TTTContext, "ArcaneWeaponIcyBurstChoice_TTT");
                    var ArcaneWeaponShockingBurstChoice_TTT = BlueprintTools.GetModBlueprint<BlueprintActivatableAbility>(TTTContext, "ArcaneWeaponShockingBurstChoice_TTT");
                    var ArcaneWeaponPlus3 = BlueprintTools.GetBlueprint<BlueprintFeature>("70be888059f99a245a79d6d61b90edc5");

                    var AddFacts = ArcaneWeaponPlus3.GetComponent<AddFacts>();
                    AddFacts.m_Facts = AddFacts.m_Facts.AppendToArray(
                        ArcaneWeaponFlamingBurstChoice_TTT.ToReference<BlueprintUnitFactReference>(),
                        ArcaneWeaponIcyBurstChoice_TTT.ToReference<BlueprintUnitFactReference>(),
                        ArcaneWeaponShockingBurstChoice_TTT.ToReference<BlueprintUnitFactReference>()
                    );
                    TTTContext.Logger.LogPatch("Patched", ArcaneWeaponPlus3);
                }
            }
            static void PatchEldritchScion() {
                PatchFighterTraining();

                void PatchFighterTraining() {
                    if (TTTContext.Fixes.Magus.Archetypes["EldritchScion"].IsDisabled("FighterTraining")) { return; }

                    var EldritchScionFighterTraining = BlueprintTools.GetBlueprint<BlueprintFeature>("eadc14e84c7e3c34684252b5c6459a45");
                    QuickFixTools.ReplaceClassLevelsForPrerequisites(EldritchScionFighterTraining, TTTContext, FeatureGroup.Feat);

                    TTTContext.Logger.LogPatch(EldritchScionFighterTraining);
                }
            }
            static void PatchHexcrafter() {
                PatchHexcrafterSpells();

                void PatchHexcrafterSpells() {
                    if (TTTContext.Fixes.Magus.Archetypes["Hexcrafter"].IsDisabled("Spells")) { return; }

                    var HexcrafterSpells = BlueprintTools.GetBlueprint<BlueprintFeature>("8122e8b3ddb1e184ebf6decc8b1403b5");
                    var BestowCurseGreater = BlueprintTools.GetBlueprintReference<BlueprintAbilityReference>("6101d0f0720927e4ca413de7b3c4b7e5");
                    var AccursedGlareAbility = BlueprintTools.GetModBlueprintReference<BlueprintAbilityReference>(TTTContext, "AccursedGlareAbility");
                    var SpellCurseAbility = BlueprintTools.GetModBlueprintReference<BlueprintAbilityReference>(TTTContext, "SpellCurseAbility");

                    HexcrafterSpells.TemporaryContext(bp => {
                        bp.AddComponent<AddKnownSpell>(c => {
                            c.m_CharacterClass = ClassTools.ClassReferences.MagusClass;
                            c.m_Archetype = new BlueprintArchetypeReference();
                            c.m_Spell = BestowCurseGreater;
                            c.SpellLevel = 6;
                        });
                        if (TTTContext.AddedContent.Spells.IsEnabled("AccursedGlare")) {
                            bp.AddComponent<AddKnownSpell>(c => {
                                c.m_CharacterClass = ClassTools.ClassReferences.MagusClass;
                                c.m_Archetype = new BlueprintArchetypeReference();
                                c.m_Spell = AccursedGlareAbility;
                                c.SpellLevel = 3;
                            });
                        }
                        if (TTTContext.AddedContent.Spells.IsEnabled("SpellCurse")) {
                            bp.AddComponent<AddKnownSpell>(c => {
                                c.m_CharacterClass = ClassTools.ClassReferences.MagusClass;
                                c.m_Archetype = new BlueprintArchetypeReference();
                                c.m_Spell = SpellCurseAbility;
                                c.SpellLevel = 3;
                            });
                        }
                    });
                }
            }
            static void PatchSwordSaint() {
                PatchFighterTraining();
                PatchPerfectCritical();

                void PatchFighterTraining() {
                    if (TTTContext.Fixes.Magus.Archetypes["SwordSaint"].IsDisabled("FighterTraining")) { return; }

                    var SwordSaintFighterTraining = BlueprintTools.GetBlueprint<BlueprintFeature>("9ab2ec65977cc524a99600babc7fe3b6");
                    QuickFixTools.ReplaceClassLevelsForPrerequisites(SwordSaintFighterTraining, TTTContext, FeatureGroup.Feat);

                    TTTContext.Logger.LogPatch(SwordSaintFighterTraining);
                }
                void PatchPerfectCritical() {
                    if (TTTContext.Fixes.Magus.Archetypes["SwordSaint"].IsDisabled("PerfectCritical")) { return; }

                    var SwordSaintPerfectStrikeCritAbility = BlueprintTools.GetBlueprint<BlueprintActivatableAbility>("c6559839738a7fc479aadc263ff9ffff");

                    SwordSaintPerfectStrikeCritAbility.SetDescription(TTTContext, "At 4th level, when a sword saint confirms a critical hit, " +
                        "he can spend 2 points from his arcane pool to increase his weapon's critical multiplier by 1.");
                    SwordSaintPerfectStrikeCritAbility
                        .GetComponent<ActivatableAbilityResourceLogic>()
                        .SpendType = ValueSpendType.Crit.Amount(2);
                    TTTContext.Logger.LogPatch("Patched", SwordSaintPerfectStrikeCritAbility);
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntityWeapon), "HoldInTwoHands", MethodType.Getter)]
        static class ItemEntityWeapon_HoldInTwoHands_Patch {
            static void Postfix(ItemEntityWeapon __instance, ref bool __result) {
                if (TTTContext.Fixes.Magus.Base.IsDisabled("SpellCombatDisableImmediatly")) { return; }
                var magusPart = __instance?.Wielder?.Get<UnitPartMagus>();
                if (magusPart == null) { return; }
                if (magusPart.CanUseSpellCombatInThisRound) {
                    if (__instance.Blueprint.Type.IsOneHandedWhichCanBeUsedWithTwoHands && !__instance.Blueprint.IsTwoHanded) {
                        __result = false;
                    }
                }
            }
        }

        // Prevents non magus classes from throwing errors during spell combat/strike
        [HarmonyPatch(typeof(UnitPartMagus), "Spellbook", MethodType.Getter)]
        class UnitPartMagus_Spellbook_Patch {
            static bool Prefix(UnitPartMagus __instance, ref Spellbook __result) {
                if (__instance.m_Spellbook == null) {
                    ClassData classData = __instance.Owner.Progression.GetClassData(__instance.Class);
                    BlueprintSpellbook blueprintSpellbook;
                    if ((blueprintSpellbook = classData?.Spellbook) == null) {
                        BlueprintCharacterClass blueprintCharacterClass = __instance.Class;
                        blueprintSpellbook = blueprintCharacterClass?.Spellbook;
                    }
                    __instance.m_Spellbook = ((blueprintSpellbook != null) ? __instance.Owner.GetSpellbook(blueprintSpellbook) : null);
                }
                __result = __instance.m_Spellbook;
                return false;
            }
        }

        // Prevents non magus books from spell combat and spell strike
        [HarmonyPatch(typeof(UnitPartMagus), "IsSpellFromMagusSpellList", new Type[] { typeof(AbilityData) })]
        class UnitPartMagus_IsSpellFromMagusSpellList_Base_Patch {
            static bool Prefix(UnitPartMagus __instance, ref bool __result, AbilityData spell) {
                bool validWandSpell = __instance.WandWielder && spell.SourceItemUsableBlueprint != null && spell.SourceItemUsableBlueprint.Type == UsableItemType.Wand;
                __result = validWandSpell || (spell.Spellbook != null && spell.Spellbook == __instance.Spellbook);
                if (TTTContext.Fixes.Magus.Base.IsDisabled("SpellCombatSpellbookRestrictions")) {
                    __result |= __instance.Spellbook != null && spell.IsInSpellList(__instance.Spellbook.Blueprint.SpellList);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(UnitPartMagus), "IsSpellFromMagusSpellList", new Type[] { typeof(AbilityData) })]
        class UnitPartMagus_IsSpellFromMagusSpellList_VarriantAbilities_Patch {
            static void Postfix(UnitPartMagus __instance, ref bool __result, AbilityData spell) {
                if (TTTContext.Fixes.Magus.Base.IsDisabled("SpellCombatAbilityVariants")) { return; }
                if (spell.ConvertedFrom != null) {
                    __result |= __instance.IsSpellFromMagusSpellList(spell.ConvertedFrom);
                }
            }
        }
    }
}