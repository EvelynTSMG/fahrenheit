// using Fahrenheit.Core.FFX;
// using Fahrenheit.Core.FFX.Battle;
// using Fahrenheit.Core.FFX.Events.Battle;
// using Fahrenheit.Core.FFX.Ids;
//
// namespace Fahrenheit.Core.Runtime;
//
// using DmgEventArgs = FhXEventsBattleDamageCalc.DamageCalcEventArgs;
//
// public unsafe partial class FhBattleAPIs {
//     // Replacement for the game's original function, so we can pass in the event args instead
//     private void DmgCalc_QueueRemoveStatusOnHit(DmgEventArgs e) {
//         if (!e.command->deals_physical_damage) return;
//
//         if (e.target_status_suffer.confuse() && !e.target->status_auto_full_permanent.confuse()) {
//             e.target_status_suffer &= ~StatusPermanentFlags.CONFUSE;
//             p5[1] += 1;
//         }
//
//         if (e.target_status_suffer_duration.sleep < 255) {
//             *FhUtil.ptr_at<byte>(e.target, 0x50f) = 1;
//             e.target_status_suffer_duration.sleep = 0;
//             p5[2] += 1;
//         }
//     }
//
//     // Replacement for the game's original function, so we can pass in the event args instead
//     private void DmgCalc_InflictStatus(DmgEventArgs e) {
//         const byte ALWAYS = 255;
//         const byte INFINITE = 254;
//
//         const int MASK_PERMANENT = 0b111111111111;
//         const int MASK_TEMPORARY = 0b1111111111111;
//
//         bool uses_weapon_props = e.command->uses_weapon_properties;
//
//         for (int status_idx = 0; status_idx <= 0x18; status_idx++) {
//             bool inflicted = false;
//             bool cleansed = false;
//             byte bVar4 = 0;
//
//             var permanent_mask = (StatusPermanentFlags)((1 << status_idx) & MASK_PERMANENT);
//
//             int temporal_idx  = status_idx - 12;
//             var temporary_mask = (StatusTemporaryFlags)((1 << temporal_idx) & MASK_TEMPORARY);
//
//             byte infliction = e.command->status_map[status_idx];
//
//             if (uses_weapon_props && e.user->status_inflict[status_idx] > infliction) {
//                 infliction = e.user->status_inflict[status_idx];
//             }
//
//             if (infliction == 0) continue;
//
//             byte resistance = e.target->status_resist[status_idx];
//
//             bool status_hit = false;
//
//             byte infliction2 = infliction;
//             byte resistance2 = resistance;
//             if (e.command->is_cleanse) {
//                 status_hit = true;
//             } else {
//                 int rng_idx = _MsGetRndChr(e.user->id, 2);
//                 int rng = _brnd(rng_idx);
//
//                 if (temporal_idx < 0 && permanent_mask.threaten()) {
//                     status_hit = rng % 100 < resistance;
//
//                     // This code has no effect, as the code that sets the threaten chance won't run if the status missed
//                     // if (!status_hit && resistance == 0) {
//                     //     resistance2 = ALWAYS;
//                     //     infliction2 = 0;
//                     // }
//                 } else {
//                     if (permanent_mask.death() && e.target_status_suffer.zombie()) {
//                         resistance2 = INFINITE;
//                     }
//
//                     if (infliction2 == ALWAYS) {
//                         status_hit = true;
//                     } else {
//                         resistance = resistance2;
//                         if (resistance != ALWAYS) {
//                             if (infliction2 == INFINITE) status_hit = true;
//                             if (rng % 101 < infliction2 - resistance) status_hit = true;
//                             if (Globals.Battle.btl->debug.INFINITE_hit) status_hit = true;
//                         }
//                     }
//                 }
//             }
//
//             if (!status_hit || Globals.Battle.btl->debug.never_hit) {
//                 if (resistance == ALWAYS) {
//                     if (temporal_idx < 0) {
//                         if (permanent_mask.petrification()) {
//                             e.dmg_calc_flags |= DmgCalcFlags.PETRIFICATION;
//                         }
//
//                         if (permanent_mask.zombie()) {
//                             e.dmg_calc_flags |= DmgCalcFlags.ZOMBIE;
//                         }
//                     } else {
//                         if (temporary_mask.sleep())    e.dmg_calc_flags |= DmgCalcFlags.SLEEP;
//                         if (temporary_mask.silence())  e.dmg_calc_flags |= DmgCalcFlags.SILENCE;
//                         if (temporary_mask.darkness()) e.dmg_calc_flags |= DmgCalcFlags.DARKNESS;
//                     }
//
//                     e.status_resists += 1;
//                     goto END_LOOP;
//                 }
//
//                 e.status_misses += 1;
//
//                 goto END_LOOP;
//             }
//
//             if (temporal_idx < 0) {
//                 DmgCalc_InflictStatus_Permanent(e, permanent_mask, status_idx, ref inflicted, ref cleansed, p7, out byte bVar4_perm);
//                 bVar4 = bVar4_perm;
//             } else {
//                 DmgCalc_InflictStatus_Temporary(e, temporary_mask, temporal_idx, ref inflicted, ref cleansed, out byte bVar4_temp);
//                 bVar4 = bVar4_temp;
//             }
//
//         END_LOOP:
//             if (!bVar4.get_bit(6)) {
//                 p6[0] += inflicted;
//                 p7[0] += cleansed;
//             } else { // Zombie, Poison, Confusion, Silence, Darkness
//                 p6[1] += inflicted;
//                 p7[1] += cleansed;
//             }
//
//             if (bVar4.get_bit(5)) { // Sleep
//                 p6[2] += inflicted;
//                 p7[2] += cleansed;
//             }
//
//             if (bVar4.get_bit(7)) { // debuffs
//                 e.debuff_inflicts += inflicted ? 1 : 0;
//                 e.debuff_cleanses += cleansed ? 1 : 0;
//             }
//
//             if (temporary_mask.haste() || temporary_mask.slow()) {
//                 e.damage_ctb = 0;
//             }
//         }
//     }
//
//     private void DmgCalc_InflictStatus_Permanent(
//         DmgEventArgs e,
//         StatusPermanentFlags permanent_mask,
//         int status_idx,
//         ref bool inflicted,
//         ref bool cleansed,
//         int* p7,
//         out byte bVar4
//     ) {
//         const byte ALWAYS = 255;
//
//         //TODO: Un-inline this MsCheckStat call, I guess?
//         bVar4 = Globals.Battle.permanent_status_data[status_idx].__0x3;
//
//         if (e.command->is_cleanse) {
//             // Can't cleanse statuses of a petrified target if we're not cleansing petrification
//             if (!permanent_mask.petrification() && e.target_status_suffer.petrification()) {
//                 e.status_misses += 1;
//                 return;
//             }
//
//             if (permanent_mask.death() && e.target_status_suffer.zombie()) {
//                 if (e.target->immune_physical_dmg) {
//                     e.status_resists += 1;
//                     return;
//                 }
//
//                 // Cleansing Death (i.e. inflicting Life) but the target is zombified.
//                 // So we kill the target! :3
//                 e.target_status_suffer |= permanent_mask;
//
//                 e.status_hits += 1;
//                 inflicted = true;
//             } else {
//                 bool auto_status = (permanent_mask & e.target->status_auto_full_permanent) != StatusPermanentFlags.NONE;
//                 if (!e.target_status_suffer.HasFlag(permanent_mask) || auto_status) {
//                     e.status_misses += 1;
//                     return;
//                 }
//
//                 cleansed = true;
//                 e.target_status_suffer &= ~permanent_mask;
//                 if (!permanent_mask.zombie()) {
//                     e.status_hits += 1;
//                 } else {
//                     p7[3] += 1;
//                     e.status_hits += 1;
//                 }
//             }
//
//             return;
//         } // Cleansing
//
//         if (e.target_status_suffer.petrification() || e.target->status_suffer.HasFlag(permanent_mask)) {
//             e.status_misses += 1;
//             return;
//         }
//
//         bool auto_uncontrollable = e.target->status_auto_full_permanent.threaten()
//                                 || e.target->status_auto_full_permanent.provoke()
//                                 || e.target->status_auto_full_permanent.berserk()
//                                 || e.target->status_auto_full_permanent.confuse();
//
//         if (permanent_mask.death()) {
//             short com_id = *FhUtil.ptr_at<short>(e.user, 0xf5c);
//             if (com_id == PlayerCommandId.PCOM_ZANMATO) {
//                 *FhUtil.ptr_at<byte>(e.target, 0x701) = 3;
//             } else if (com_id == PlayerCommandId.PCOM_DEATH_EX) {
//                 *FhUtil.ptr_at<byte>(e.target, 0x701) = 4;
//             }
//
//             e.target_status_suffer |= permanent_mask;
//             e.status_hits += 1;
//             inflicted = true;
//
//             return;
//         } // Death
//
//         if (permanent_mask.confuse()) {
//             if (auto_uncontrollable) {
//                 e.status_misses += 1;
//                 return;
//             }
//
//             e.target_status_suffer |= permanent_mask;
//             e.target_status_suffer &= ~(
//                 StatusPermanentFlags.THREATEN
//               | StatusPermanentFlags.PROVOKE
//               | StatusPermanentFlags.BERSERK
//             );
//             e.status_hits += 1;
//             inflicted = true;
//
//             return;
//         } // Confusion
//
//         if (permanent_mask.provoke()) {
//             if (auto_uncontrollable) {
//                 e.status_misses += 1;
//                 return;
//             }
//
//             e.target_status_suffer |= permanent_mask;
//             e.target->provoked_by_id = (byte)e.user->id;
//             e.target_status_suffer &= ~(
//                 StatusPermanentFlags.THREATEN
//               | StatusPermanentFlags.BERSERK
//               | StatusPermanentFlags.CONFUSE
//             );
//             e.status_hits += 1;
//             inflicted = true;
//
//             return;
//         } // Provoke
//
//         if (permanent_mask.threaten()) {
//             bool has_auto_haste_or_slow = e.target_status_suffer_duration.haste == ALWAYS
//                                        || e.target_status_suffer_duration.slow == ALWAYS;
//
//             if (auto_uncontrollable || has_auto_haste_or_slow) {
//                 e.status_misses += 1;
//                 return;
//             }
//
//             inflicted = true;
//             e.threatened = true;
//
//             // Original used `resistance2` from the main method (and dependent on the value assigned earlier),
//             // but this has no side effects
//             byte resistance = e.target->status_resist.threaten;
//             if (resistance != 0) {
//                 resistance = (byte)(resistance * 7 / 10);
//                 if (resistance == 0) resistance = 1;
//
//                 // Original used `e.target->status_resist[status_idx]`, but this has no side effects
//                 e.target->status_resist.threaten = resistance;
//             }
//
//             e.target_status_suffer |= permanent_mask;
//             e.dmg_calc_flags |= DmgCalcFlags.DAMAGE_CTB;
//             e.target->threatened_by_id = (byte)e.user->id;
//             int ctb = _MsGetNextCTB(
//                 e.user,
//                 *FhUtil.ptr_at<byte>(e.user, 0xde8),
//                 e.user_status_suffer_duration.haste,
//                 e.user_status_suffer_duration.slow
//             );
//
//             e.damage_ctb = ctb + e.user->ctb - e.target->ctb;
//             e.target_status_suffer &= ~(
//                 StatusPermanentFlags.PROVOKE
//               | StatusPermanentFlags.BERSERK
//               | StatusPermanentFlags.CONFUSE
//             );
//             e.target_status_suffer_duration.haste = 0;
//             e.target_status_suffer_duration.slow = 0;
//
//             e.status_hits += 1;
//
//             return;
//         } // Threaten
//
//         if (permanent_mask.berserk()) {
//             if (auto_uncontrollable) {
//                 e.status_misses += 1;
//                 return;
//             }
//
//             e.target_status_suffer |= permanent_mask;
//             e.target_status_suffer &= ~(
//                 StatusPermanentFlags.THREATEN
//               | StatusPermanentFlags.PROVOKE
//               | StatusPermanentFlags.CONFUSE
//             );
//             e.status_hits += 1;
//             inflicted = true;
//
//             return;
//         } // Berserk
//
//         inflicted = true;
//
//         if (permanent_mask.petrification()) {
//             e.target_status_suffer = permanent_mask;
//
//             // Clear all temporary status when applying petrification
//             e.target_status_suffer_duration = new() {
//                 sleep = 0,
//                 silence = 0,
//                 darkness = 0,
//                 shell = 0,
//                 protect = 0,
//                 reflect = 0,
//                 nul_tide = 0,
//                 nul_blaze = 0,
//                 nul_shock = 0,
//                 nul_frost = 0,
//                 regen = 0,
//                 haste = 0,
//                 slow = 0,
//             };
//
//             // Clear most extra status when applying petrification
//             // (Except Scan, Distills, and Eject)
//             e.target_status_suffer_extra &= ~(
//                 StatusExtraFlags.DOOM
//               | StatusExtraFlags.SENTINEL
//               | StatusExtraFlags.GUARD
//               | StatusExtraFlags.DEFEND
//               | StatusExtraFlags.CURSE
//               | StatusExtraFlags.AUTO_LIFE
//               | StatusExtraFlags.BOOST
//               | StatusExtraFlags.SHIELD
//             );
//
//             bool is_monster = _MsGetRamChrMonster(e.target->id);
//             bool btl_flag = *FhUtil.ptr_at<byte>(Globals.Battle.btl, 0x20fe) != 0;
//
//             if (!is_monster && !btl_flag) {
//                 e.status_hits += 1;
//
//                 return;
//             }
//
//             // If the target is a monster (or underwater?),
//             // kill and eject them immediately
//             e.target_status_suffer |= StatusPermanentFlags.DEATH;
//             e.target_status_suffer_extra |= StatusExtraFlags.EJECT;
//
//             e.status_hits += 1;
//
//             return;
//         } // Petrification
//
//         // Every other permanent status
//         e.target_status_suffer |= permanent_mask;
//         e.status_hits += 1;
//     }
//
//     private void DmgCalc_InflictStatus_Temporary(
//         DmgEventArgs e,
//         StatusTemporaryFlags temporary_mask,
//         int temporary_idx,
//         ref bool inflicted,
//         ref bool cleansed,
//         out byte bVar4
//     ) {
//         const byte ALWAYS = 255;
//
//         byte inflict_duration = e.command->status_duration_map[temporary_idx];
//
//         // Original used `uses_weapon_props` but this is equivalent
//         if (e.command->uses_weapon_properties) {
//             inflict_duration = Math.Max(inflict_duration, e.user->status_duration_inflict[temporary_idx]);
//         }
//
//         //TODO: Un-inline this `MsCheckStatCount` call
//         bVar4 = Globals.Battle.temporary_status_data[temporary_idx].__0x3;
//
//         if (e.target_status_suffer.petrification()) {
//             e.status_misses += 1;
//             return;
//         }
//
//         if (e.command->is_cleanse) {
//             byte turns_left = e.target_status_suffer_duration[temporary_idx];
//             if (turns_left is 0 or ALWAYS) {
//                 e.status_misses += 1;
//                 return;
//             }
//
//             cleansed = true;
//             turns_left = (byte)Math.Clamp(turns_left - inflict_duration, 0, byte.MaxValue);
//             e.target_status_suffer_duration[temporary_idx] = turns_left;
//             e.status_hits += 1;
//
//             return;
//         }
//
//         if (e.user->id == e.target->id && inflict_duration < 253 && bVar4.get_bit(2)) {
//             // Attacking self, so increment duration to account for it decrementing at the end of this turn.
//             inflict_duration += 1;
//         }
//
//         // Don't re-inflict already applied statuses
//         if (e.target->status_suffer_turns_left[temporary_idx] != 0) {
//             e.status_misses += 1;
//             return;
//         }
//
//         bool immune_to_haste_slow = e.target_status_suffer_duration.haste == ALWAYS
//                                  || e.target_status_suffer_duration.slow == ALWAYS
//                                  || e.target_status_suffer.threaten();
//
//         if (temporary_mask.sleep()) {
//             e.target_status_suffer_duration[temporary_idx] = inflict_duration;
//             e.target_status_suffer_extra &= ~StatusExtraFlags.DEFEND;
//
//             e.status_hits += 1;
//             inflicted = true;
//
//             return;
//         }
//
//         if (temporary_mask.haste()) {
//             if (immune_to_haste_slow) {
//                 e.status_misses += 1;
//                 return;
//             }
//
//             e.target_status_suffer_duration[temporary_idx] = inflict_duration;
//             e.target_status_suffer_duration.slow = 0;
//             e.target_status_suffer &= ~StatusPermanentFlags.THREATEN;
//
//             e.status_hits += 1;
//             inflicted = true;
//
//             return;
//         }
//
//         if (temporary_mask.slow()) {
//             if (immune_to_haste_slow) {
//                 e.status_misses += 1;
//                 return;
//             }
//
//             e.target_status_suffer_duration[temporary_idx] = inflict_duration;
//             e.target_status_suffer_duration.haste = 0;
//             e.target_status_suffer &= ~StatusPermanentFlags.THREATEN;
//
//             e.status_hits += 1;
//             inflicted = true;
//
//             return;
//         }
//
//         // Every other temporary status
//         e.target_status_suffer_duration[temporary_idx] = inflict_duration;
//
//         e.status_hits += 1;
//         inflicted = true;
//     }
//
//     // Replacement for the game's original function, so we can pass in the event args instead
//     private void DmgCalc_InflictStatus_Extra(DmgEventArgs e) {
//         const StatusExtraFlags APPLICABLE_TO_PETRIFIED = StatusExtraFlags.EJECT | StatusExtraFlags.SCAN | StatusExtraFlags.DISTILLS;
//
//         StatusExtraFlags inflict = e.status_inflict_extra;
//
//         if (e.target_status_suffer.petrification()) {
//             int rng_idx = _MsGetRndChr(e.user->id, 2);
//             int rng = _brnd(rng_idx);
//             if (rng % 101 < e.command->shatter_chance) {
//                 inflict |= StatusExtraFlags.EJECT;
//                 e.target_status_suffer |= StatusPermanentFlags.DEATH;
//             }
//         }
//
//         for (int idx = 0; idx < 16; idx++) {
//             var mask = (StatusExtraFlags)(1 << idx);
//
//             if (!inflict.HasFlag(mask)) continue;
//
//             //TODO: Un-inline this MsCheckStat2 call
//             byte bVar4 = Globals.Battle.extra_status_data[idx].__0x3;
//
//             bool inflicted = false;
//             bool cleansed = false;
//
//             bool should_apply = !e.target_status_suffer.petrification() || (inflict & APPLICABLE_TO_PETRIFIED) != 0;
//
//             if (Globals.Battle.btl->debug.never_hit || !should_apply) {
//                 e.status_misses += 1;
//                 goto END_LOOP;
//             }
//
//             if (e.command->is_cleanse) {
//                 if (!e.target_status_suffer_extra.HasFlag(mask) || e.target->status_auto_full_extra.HasFlag(mask)) {
//                     e.status_misses += 1;
//                     goto END_LOOP;
//                 }
//
//                 if (mask.auto_life()) {
//                     *FhUtil.ptr_at<byte>(e.target, 0x5ca) = 0;
//                 }
//
//                 e.target_status_suffer_extra &= ~mask;
//
//                 e.status_hits += 1;
//                 cleansed = true;
//
//                 goto END_LOOP;
//             }
//
//             if (e.target->status_resist_extra.HasFlag(mask)) {
//                 e.status_resists += 1;
//                 goto END_LOOP;
//             }
//
//             // Scan can be repeated
//             if (e.target->status_suffer_extra.HasFlag(mask) && !mask.scan()) {
//                 e.status_misses += 1;
//                 goto END_LOOP;
//             }
//
//             bool is_distilled = (e.target->status_auto_full_extra & StatusExtraFlags.DISTILLS) != 0;
//
//             if (is_distilled && mask.distills()) {
//                 e.status_misses += 1;
//                 goto END_LOOP;
//             }
//
//             inflicted = true;;
//
//             if (mask.distills()) {
//                 e.target_status_suffer_extra &= ~StatusExtraFlags.DISTILLS;
//                 e.target_status_suffer_extra |= mask;
//                 e.status_hits += 1;
//
//                 goto END_LOOP;
//             }
//
//             if (mask.doom()) {
//                 e.target->doom_counter = e.target->doom_counter_init;
//                 e.target_status_suffer_extra |= mask;
//                 e.status_hits += 1;
//
//                 goto END_LOOP;
//             }
//
//             if (mask.eject()) {
//                 e.target_status_suffer_extra |= mask;
//                 e.target_status_suffer &= ~StatusPermanentFlags.THREATEN;
//                 e.status_hits += 1;
//
//                 goto END_LOOP;
//             }
//
//             if (mask.auto_life() && _DmgCalc_MagicBooster(e.user, e.command, 0, null) != 0) {
//                 *FhUtil.ptr_at<byte>(e.target, 0x5ca) = 1;
//             }
//
//             // Every other extra status
//             e.target_status_suffer_extra |= mask;
//             e.status_hits += 1;
//
//
//         END_LOOP:
//             if (bVar4.get_bit(6)) {
//                 p6[0] += inflicted ? 1 : 0;
//                 p7[0] += cleansed ? 1 : 0;
//             } else {
//                 p6[1] += inflicted ? 1 : 0;
//                 p7[1] += cleansed ? 1 : 0;
//             }
//
//             if (bVar4.get_bit(7)) {
//                 p6[7] += inflicted ? 1 : 0;
//                 p7[7] += cleansed ? 1 : 0;
//             }
//         }
//     }
// }
