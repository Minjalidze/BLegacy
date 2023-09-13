using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BCore.ClanSystem;
using BCore.Configs;
using BCore.EventSystem;
using BCore.Users;
using BCore.WorldManagement;
using Oxide.Core.Libraries;
using UnityEngine;
using Random = UnityEngine.Random;
using String = Facepunch.Utility.String;

namespace BCore;

public static class ByteCollectionHelper
{
    public static int ToInt32(this byte[] bytes, int offset = 0)
    {
        if (offset + 4 > bytes.Length) return 0;
        return bytes[offset++] | (bytes[offset++] << 8) | (bytes[offset++] << 16) | (bytes[offset] << 24);
    }
}

public static class NumericHelper
{
    public static string ToHex(this int value, bool asString = true)
    {
        return (asString ? "0x" : "") + $"{value:X8}";
    }

    public static string ToHex(this uint value, bool asString = true)
    {
        return (asString ? "0x" : "") + $"{value:X8}";
    }

    public static string ToHex(this long value, bool asString = true)
    {
        return (asString ? "0x" : "") + $"{value:X16}";
    }

    public static string ToHex(this ulong value, bool asString = true)
    {
        return (asString ? "0x" : "") + $"{value:X16}";
    }
}

public static class StringHelper
{
    public static bool IsEmpty(this string input)
    {
        return string.IsNullOrEmpty(input);
    }

    public static int ToInt32(this string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) value = value.Substring(2);
        try
        {
            return int.Parse(value, NumberStyles.HexNumber);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public static string Args(this string str, object arg0)
    {
        return string.Format(str, arg0);
    }

    public static string Args(this string str, object arg0, object arg1)
    {
        return string.Format(str, arg0, arg1);
    }

    public static string Args(this string str, object arg0, object arg1, object arg2)
    {
        return string.Format(str, arg0, arg1, arg2);
    }

    public static string Args(this string str, params object[] args)
    {
        return string.Format(str, args);
    }

    public static uint ToUInt32(this string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) value = value.Substring(2);
        try
        {
            return uint.Parse(value, NumberStyles.HexNumber);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    public static T ToEnum<T>(this string input)
    {
        if (string.IsNullOrEmpty(input)) input = "0";
        return (T)Enum.Parse(typeof(T), input, true);
    }

    public static bool ToBool(this string input)
    {
        input = input.Trim().ToUpper();
        return input is "ENABLED" or "ENABLE" or "TRUE" or "YES" or "ON" or "Y" or "1";
    }
}

#region Helper: Enumerable

public static class EnumHelper
{
    public static bool Has<T>(this Enum flags, T value) where T : struct
    {
        var iFlags = Convert.ToUInt64(flags);
        var iValue = Convert.ToUInt64(value);
        return (iFlags & iValue) == iValue;
    }

    public static T SetFlag<T>(this Enum flags, T value, bool state = true)
    {
        if (!Enum.IsDefined(typeof(T), value)) throw new ArgumentException("Enum value and flags types don't match.");
        if (state) return (T)Enum.ToObject(typeof(T), Convert.ToUInt64(flags) | Convert.ToUInt64(value));
        return (T)Enum.ToObject(typeof(T), Convert.ToUInt64(flags) & ~Convert.ToUInt64(value));
    }
}

#endregion

public static class StringCollectionExtensions
{
    public static T[] Add<T>(this T[] array, T item)
    {
        return (array ?? Enumerable.Empty<T>()).Concat(new[] { item }).ToArray();
    }

    public static T[] AddRange<T>(this T[] array, T[] items)
    {
        return (array ?? Enumerable.Empty<T>()).Concat(items).ToArray();
    }

    public static T[] Remove<T>(this T[] array, T item)
    {
        var index = Array.IndexOf(array, item);
        return index == -1 ? array : array.RemoveAt(index);
    }

    private static T[] RemoveAt<T>(this T[] array, int index)
    {
        var dest = new T[array.Length - 1];
        if (index > 0) Array.Copy(array, 0, dest, 0, index);
        if (index < array.Length - 1) Array.Copy(array, index + 1, dest, index, array.Length - index - 1);
        return dest;
    }
}

#region Helper: UnityEngine.Vector2

public static class Vector2Helper
{
    public static string AsString(this Vector2 vector)
    {
        return vector.ToString().Trim('(', ')');
    }
}

#endregion

#region Helper: UnityEngine.Vector3

public static class Vector3Helper
{
    public static string AsString(this Vector3 vector)
    {
        return vector.ToString().Trim('(', ')');
    }

    public static bool Invalid(this Vector3 value)
    {
        return float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.x) ||
               float.IsInfinity(value.y) || float.IsInfinity(value.z);
    }
}

#endregion

public static class Helper
{
    
    public static bool HurtShared(TakeDamage take, ref DamageEvent damage, ref TakeDamage.Quantity quantity)
    {
        if (take.dead)
        {
	        return true;
        }
        bool flag = damage.victim.idMain is Character;
        int num = (damage.attacker.idMain is Character) ? 1 : 0;
        bool flag2 = !flag;
        bool flag3 = num == 0;
        TakeDamage takeDamage = flag2 ? damage.victim.idMain.GetLocal<TakeDamage>() : ((Character)damage.victim.idMain).takeDamage;
        WorldZone worldZone = Zones.Get(damage.victim.idMain.transform.position);
        WorldZone worldZone2 = Zones.Get(damage.attacker.idMain.transform.position);
        WeaponImpact weaponImpact = damage.extraData as WeaponImpact;
        if (flag2 && flag3 && damage.victim.idMain == damage.attacker.idMain)
        {
	        if (Math.Abs(damage.amount - 3.4028235E+38f) < 0)
	        {
		        return true;
	        }
	        if (!Config._Settings.Decay)
	        {
		        damage.amount = 0f;
		        if (server.log > 2)
		        {
			        UnityEngine.Debug.Log("Object: " + Helper.NiceName(takeDamage.name) + " without a decay, because decay is disabled in config.");
		        }
	        }
	        DeployableObject component = takeDamage.GetComponent<DeployableObject>();
	        if (component != null && Data.FindUser(component.ownerID).HasFlag(Data.UserFlags.Admin))
	        {
		        damage.amount = 0f;
		        if (server.log > 2)
		        {
			        UnityEngine.Debug.Log("Object: " + Helper.NiceName(takeDamage.name) + " without a decay, because owner is administrator.");
		        }
	        }
	        StructureComponent component2 = takeDamage.GetComponent<StructureComponent>();
	        if (component2 != null && Data.FindUser(component2._master.ownerID).HasFlag(Data.UserFlags.Admin))
	        {
		        damage.amount = 0f;
		        if (server.log > 2)
		        {
			        UnityEngine.Debug.Log("Object: " + Helper.NiceName(takeDamage.name) + " without a decay, because owner is administrator.");
		        }
	        }
	        if (worldZone != null && worldZone.Flags.Has(ZoneFlags.NoDecay))
	        {
		        damage.amount = 0f;
		        if (server.log > 2)
		        {
			        UnityEngine.Debug.Log("Object: " + Helper.NiceName(takeDamage.name) + " without a decay, because zone have NoDecay flag.");
		        }
	        }
	        return true;
        }
        else
        {
	        PlayerClient victimPlayer = damage.victim.client;
	        PlayerClient client = damage.attacker.client;
	        Metabolism component3 = damage.victim.id.GetComponent<Metabolism>();
	        HumanBodyTakeDamage component4 = damage.victim.id.GetComponent<HumanBodyTakeDamage>();
	        string text = "";
	        ulong num2 = 0UL;
	        if (damage.victim.client)
	        {
		        num2 = damage.victim.client.userID;
	        }
	        DeployableObject deployableObject;
	        if ((deployableObject = (damage.victim.idMain as DeployableObject)) != null)
	        {
		        num2 = deployableObject.ownerID;
	        }
	        StructureComponent structureComponent;
	        if ((structureComponent = (damage.victim.idMain as StructureComponent)) != null)
	        {
		        num2 = structureComponent._master.ownerID;
	        }
	        string text2 = "";
	        ulong num3 = 0UL;
	        if (damage.attacker.client)
	        {
		        num3 = damage.attacker.client.userID;
	        }
	        IDMain idMain = damage.attacker.idMain;
	        if (idMain != null)
	        {
		        DeployableObject deployableObject2;
		        if ((deployableObject2 = (idMain as DeployableObject)) == null)
		        {
			        StructureComponent structureComponent2;
			        if ((structureComponent2 = (idMain as StructureComponent)) != null)
			        {
				        num3 = structureComponent2._master.ownerID;
			        }
		        }
		        else
		        {
			        num3 = deployableObject2.ownerID;
		        }
	        }
	        string text3 = "";
	        if (weaponImpact != null)
	        {
		        text3 = weaponImpact.dataBlock.name;
	        }
	        float num4 = Vector3.Distance(damage.attacker.id.transform.position, damage.victim.id.transform.position);
	        User userData = (num2 != 0UL) ? Data.FindUser(num2) : null;
	        User userData2 = (num3 != 0UL) ? Data.FindUser(num3) : null;
	        ClanData clanData = (userData != null) ? Clans.Find(userData.Clan) : null;
	        ClanData clanData2 = (userData2 != null) ? Clans.Find(userData2.Clan) : null;
	        if (damage.victim.client && !flag2 && userData != null && userData.HasFlag(Data.UserFlags.GodMode))
	        {
		        if (component4 != null && component4._bleedingLevel > 0f)
		        {
			        component4.Bandage(1000f);
		        }
		        damage.amount = 0f;
		        return false;
	        }
	        if (client != null && userData != null && userData2 != null && userData != userData2)
	        {
		        if (!flag3 || !(damage.attacker.idMain is DeployableObject))
		        {
			        if (clanData2 != null && clanData != null && clanData2 == clanData && Clans.Find(userData.Clan).Flags.Has(ClanFlags.CanFFire) && Clans.Find(userData.Clan).FriendlyFire)
			        {
				        string message = flag2
					        ? Messages.RuMessages.RuMessage.PlayerNoDamageClanMemberOwned
					        : Messages.RuMessages.RuMessage.PlayerNoDamageClanMember;
				        Broadcast.Notice(client.netUser, "☢", message.Replace("%KILLER%", userData2.UserName).Replace("%VICTIM%", userData.UserName), 5f);
				        damage.amount = 0f;
				        return false;
			        }
			        if (userData2.HasFlag(Data.UserFlags.NoPvp) || userData.HasFlag(Data.UserFlags.NoPvp))
			        {
				        string message2 = flag2
					        ? Messages.RuMessages.RuMessage.PlayerNoDamageWithoutPvPOwned
					        : Messages.RuMessages.RuMessage.PlayerNoDamageWithoutPvP;
				        Broadcast.Notice(client.netUser, "☢", message2.Replace("%KILLER%", userData2.UserName).Replace("%VICTIM%", userData.UserName), 5f);
				        damage.amount = 0f;
				        return false;
			        }
			        if (userData2.Zone != null && (userData2.Zone.NoPvP || userData2.Zone.Safe))
			        {
				        string message3 = flag2
					        ? Messages.RuMessages.RuMessage.PlayerNoDamageZoneWithoutPvPOwned
					        : Messages.RuMessages.RuMessage.PlayerNoDamageZoneWithoutPvP;
				        Broadcast.Notice(client.netUser, "☢", message3.Replace("%KILLER%", userData2.UserName).Replace("%VICTIM%", userData.UserName), 5f);
				        damage.amount = 0f;
				        return false;
			        }
		        }
		        WorldZone worldZone3 = flag2 ? Zones.Get(damage.victim.idMain.transform.position) : userData.Zone;
		        if (worldZone3 != null && (worldZone3.NoPvP || worldZone3.Safe))
		        {
			        string message4 = flag2
				        ? Messages.RuMessages.RuMessage.PlayerNoDamageZoneWithSafetyOwned
				        : Messages.RuMessages.RuMessage.PlayerNoDamageZoneWithSafety;
			        Broadcast.Notice(client.netUser, "☢", message4.Replace("%KILLER%", userData2.UserName).Replace("%VICTIM%", userData.UserName), 5f);
			        damage.amount = 0f;
			        return false;
		        }
	        }
	        if (damage.attacker.client && damage.attacker.client.netUser.admin && Config._Settings.AdminInstantDestroy)
	        {
		        weaponImpact?.item.SetCondition(weaponImpact.item.maxcondition);
		        damage.amount = float.PositiveInfinity;
	        }
	        else if (!WorldManagement.Override.DamageOverride(take, ref damage, ref quantity))
	        {
		        damage.amount = 0f;
		        return false;
	        }
	        if (flag3)
	        {
		        text2 = Helper.NiceName(damage.attacker.idMain.name);
	        }
	        else if (damage.attacker.client)
	        {
		        text2 = damage.attacker.client.userName;
		        if (damage.amount > 0f && clanData2 != null && clanData2.Level.BonusMembersDamage > 0U)
		        {
			        damage.amount += damage.amount * clanData2.Level.BonusMembersDamage / 100f;
		        }
		        if (server.pvp && !flag2 && damage.amount >= takeDamage.health && damage.victim.id != damage.attacker.id)
		        {
			        string text4;
			        if (damage.victim.client)
			        {
				        text = damage.victim.client.userName;
				        text4 = Config.GetMessageMurder(Messages.RuMessages.RuMessage.PlayerNoticeMurder, client.netUser, text, null);
				        if (text4.Equals("PlayerNotice.Murder", StringComparison.CurrentCultureIgnoreCase))
				        {
					        text4 = "";
				        }
			        }
			        else
			        {
				        text = Helper.NiceName(damage.victim.character.name);
				        text = Messages.RuMessages.Names[text];
				        text4 = Config.GetMessageMurder(Messages.RuMessages.RuMessage.PlayerNoticeNPC, client.netUser, text, null);
				        if (text4.Equals("PlayerNotice.NPC", StringComparison.CurrentCultureIgnoreCase))
				        {
					        text4 = "";
				        }
			        }
			        if (Config._Settings.KillNotice && text4 != "")
			        {
				        if (text3 != "")
				        {
					        text4 = text4.Replace("%WEAPON%", text3);
				        }
				        DamageTypeFlags damageTypes = damage.damageTypes;
				        switch (damageTypes)
				        {
					        case DamageTypeFlags.damage_generic:
						        text4 = text4.Replace("%WEAPON%", "Melee");
						        break;
					        case DamageTypeFlags.damage_bullet:
						        if (weaponImpact != null)
						        {
							        text4 = text4.Replace("%WEAPON%", text3);
						        }
						        break;
					        case DamageTypeFlags.damage_generic | DamageTypeFlags.damage_bullet:
						        break;
					        case DamageTypeFlags.damage_melee:
						        if (weaponImpact == null)
						        {
							        text3 = "Hunting Bow";
						        }
						        text4 = text4.Replace("%WEAPON%", text3);
						        break;
					        default:
						        if (damageTypes == DamageTypeFlags.damage_explosion)
						        {
							        if (damage.attacker.id.name.StartsWith("F1Grenade"))
							        {
								        text3 = "F1 Grenade";
							        }
							        if (damage.attacker.id.name.StartsWith("ExplosiveCharge"))
							        {
								        text3 = "Explosive Charge";
							        }
							        text4 = text4.Replace("%WEAPON%", text3);
						        }
						        break;
				        }
				        string niceName = damage.bodyPart.GetNiceName();
				        niceName = Messages.RuMessages.BodyPart[niceName];
				        text4 = text4.Replace("%BODYPART%", niceName);
				        text4 = text4.Replace("%DISTANCE%", num4.ToString("N1"));
				        text4 = text4.Replace("%DAMAGE%", damage.amount.ToString("0.0"));
				        Broadcast.Notice(client.netUser, "☠", text4, 2.5f);
			        }
		        }
	        }
	        else
	        {
		        text2 = Helper.NiceName(damage.attacker.character.name);
		        text2 = Messages.RuMessages.Names[text2];
	        }
	        if (flag2)
	        {
		        text = Helper.NiceName(damage.victim.idMain.name);
		        if (client != null && client.netUser.admin && damage.amount >= takeDamage.health)
		        {
			        Helper.Log(Config.GetMessageObject(Messages.RuMessages.RuMessage.PlayerOwnershipLoggerDestroyed, text, client, text3, userData), false);
			        return true;
		        }
		        if (num3 != num2 && userData != null && userData2 != null && userData.HasFlag(Data.UserFlags.Admin) && !userData2.HasFlag(Data.UserFlags.Admin))
		        {
			        damage.amount = 0f;
			        return false;
		        }
		        if (num2 != num3 && Config._Settings.AttackedAnnounce)
		        {
			        NetUser netUser = NetUser.FindByUserID(num2);
			        text = Messages.RuMessages.Names[text];
			        if (netUser != null && damage.amount != 0f)
			        {
				        if (damage.amount >= takeDamage.health)
				        {
					        Broadcast.Message(netUser, Config.GetMessageObject(Messages.RuMessages.RuMessage.PlayerOwnershipObjectDestroyed, text, client, text3, userData), null, 1f);
				        }
				        else
				        {
					        Broadcast.Message(netUser, Config.GetMessageObject(Messages.RuMessages.RuMessage.PlayerOwnershipObjectAttacked, text, client, text3, userData), null, 1f);
				        }
			        }
		        }
		        else if ((num2 == num3 || Data.FindUser(num2).HasShared(num3)) && (Config.DestoryOwnership.ContainsKey(client.userID)))
		        {
			        StructureComponent component5 = damage.victim.idMain.GetComponent<StructureComponent>();
			        if (!Config._Settings.OwnershipDestroyNoCarryWeight || component5 == null || !component5._master.ComponentCarryingWeight(component5))
			        {
				        if (damage.amount == 0f && damage.attacker.id is TimedGrenade grenade)
				        {
					        damage.amount = grenade.damage;
				        }
				        if (damage.amount == 0f && damage.attacker.id is TimedExplosive explosive)
				        {
					        damage.amount = explosive.damage;
				        }
				        if (damage.amount == 0f && weaponImpact != null)
				        {
					        damage.amount = UnityEngine.Random.Range(weaponImpact.dataBlock.damageMin, weaponImpact.dataBlock.damageMax);
				        }
				        if (damage.amount == 0f && weaponImpact == null)
				        {
					        damage.amount = (float)UnityEngine.Random.Range(75, 75);
				        }
				        if (Config._Settings.InstantDestroy)
				        {
					        damage.amount = float.PositiveInfinity;
					        if (damage.victim.idMain.GetComponent<SpikeWall>() != null)
					        {
						        damage.damageTypes = DamageTypeFlags.damage_generic;
					        }
				        }
			        }
			        if (damage.amount >= takeDamage.health)
			        {
				        if (Config._Settings.OwnershipDestroyReceiveResources)
				        {
					        string key = damage.victim.idMain.name.Replace("(Clone)", "");
					        if (Destroy.DestroyResources.ContainsKey(key))
					        {
						        string[] array = Destroy.DestroyResources[key].Split(new char[]
						        {
							        ','
						        });
						        for (int i = 0; i < array.Length; i++)
						        {
							        string[] array2 = Facepunch.Utility.String.SplitQuotesStrings(array[i]);
							        if (array2.Length < 2)
							        {
								        array2 = new string[]
								        {
									        "1",
									        array2[0]
								        };
							        }
							        ItemDataBlock byName = DatablockDictionary.GetByName(array2[1]);
							        if (byName != null)
							        {
								        string text5 = byName.name;
								        int num5;
								        if (!int.TryParse(array2[0], out num5))
								        {
									        num5 = 1;
								        }
								        if (num5 > 0)
								        {
									        text5 = num5.ToString() + " " + text5;
								        }
								        Helper.GiveItem(client, byName, num5, -1);
								        text = Messages.RuMessages.Names[text];
								        Broadcast.Message(client.netUser, Config.GetMessage(Messages.RuMessages.RuMessage.CommandDestroyResourceReceived, client.netUser, null).Replace("%ITEMNAME%", text5).Replace("%OBJECT%", text), null, 0f);
							        }
							        else
							        {
								        Helper.Log(string.Format("Resource {0} not exist for receive after destroy {1}.", array2[1], text), false);
							        }
						        }
					        }
					        else
					        {
						        Helper.Log("Resources not found for object '" + text + "' to receive for player.", false);
					        }
				        }
				        Helper.Log(Config.GetMessageObject(Messages.RuMessages.RuMessage.PlayerOwnershipLoggerDestroyed, text, client, text3, userData), false);
			        }
		        }
	        }
	        else if (damage.victim.client)
	        {
		        if (damage.amount > 0f && clanData != null && clanData.Level.BonusMembersDefense > 0U)
		        {
			        damage.amount -= damage.amount * clanData.Level.BonusMembersDefense / 100f;
		        }
		        EventTimer eventTimer = Events.Timer.Find((EventTimer E) => E.Sender == victimPlayer.netUser && E.Command == "home");
		        if (eventTimer != null)
		        {
			        Broadcast.Notice(victimPlayer.netUser, "☢", Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandHomeInterrupt, "", client.netUser), 5f);
			        eventTimer.Dispose();
			        Events.Timer.Remove(eventTimer);
		        }
		        EventTimer eventTimer2 = Events.Timer.Find((EventTimer E) => E.Sender == victimPlayer.netUser && E.Command == "clan");
		        if (eventTimer2 != null)
		        {
			        Broadcast.Notice(victimPlayer.netUser, "☢", Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandClanWarpInterrupt, "", client.netUser), 5f);
			        eventTimer2.Dispose();
			        Events.Timer.Remove(eventTimer2);
		        }
		        EventTimer eventTimer3 = Events.Timer.Find((EventTimer E) => (E.Sender == victimPlayer.netUser || E.Target == victimPlayer.netUser) && E.Command == "tp");
		        if (eventTimer3 != null)
		        {
			        if (eventTimer3.Sender != null)
			        {
				        Broadcast.Notice(eventTimer3.Sender.networkPlayer, "☢", Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandTeleportInterrupt, "", client.netUser), 5f);
			        }
			        if (eventTimer3.Target != null)
			        {
				        Broadcast.Notice(eventTimer3.Target.networkPlayer, "☢", Config.GetMessageCommand(Messages.RuMessages.RuMessage.CommandTeleportInterrupt, "", client.netUser), 5f);
			        }
			        eventTimer3.Dispose();
			        Events.Timer.Remove(eventTimer3);
		        }
		        if (!server.pvp && victimPlayer != null && client != null && victimPlayer != client)
		        {
			        return false;
		        }
		        if (damage.amount >= takeDamage.health)
		        {
			        if (userData != null)
			        {
			        }
			        string text7 = "";
			        bool flag4 = false;
			        if (client == null)
			        {
				        flag4 = Config._Settings.DeathNpc;
				        text7 = Config.GetMessageDeath(Messages.RuMessages.RuMessage.PlayerDeathNpc, victimPlayer.netUser, text2, null);
			        }
			        else if (victimPlayer == client || flag3)
			        {
				        flag4 = Config._Settings.DeathSelf;
				        text7 = Config.GetMessageDeath(Messages.RuMessages.RuMessage.PlayerDeathSuicide, victimPlayer.netUser, null, null);
			        }
			        else if (victimPlayer != client)
			        {
				        flag4 = Config._Settings.DeathMurder;
				        text7 = Config.GetMessageDeath(Messages.RuMessages.RuMessage.PlayerDeathMurder, victimPlayer.netUser, text2, null);
				        if (!flag2 && userData != null && userData2 != null && clanData2 != null && client != null)
				        {
					        float num7 = 0f;
					        if (userData != null)
					        {
						        if (clanData == null)
						        {
							        num7 = Math.Abs(50f * Clans.ExperienceMultiplier);
						        }
						        else if (clanData2.Hostile.ContainsKey(clanData.ID))
						        {
							        num7 = Math.Abs(250f * Clans.ExperienceMultiplier);
							        if (Clans.ClanWarDeathPay)
							        {
								        clanData2.Balance += clanData.Balance * (ulong)Clans.ClanWarDeathPercent / 100UL;
							        }
							        if (Clans.ClanWarMurderFee)
							        {
								        clanData.Balance -= clanData.Balance * (ulong)Clans.ClanWarMurderPercent / 100UL;
							        }
						        }
						        else if (clanData != clanData2)
						        {
							        num7 = Math.Abs(100f * Clans.ExperienceMultiplier);
						        }
					        }
					        if (num7 >= 0f && num7 >= 1f)
					        {
						        clanData2.Experience += (ulong)num7;
						        if (clanData2.Members[userData2].Has(ClanMemberFlags.ExpDetails))
						        {
							        Broadcast.Message(client.netPlayer, Config.GetMessage(Messages.RuMessages.RuMessage.ClanExperienceMurder, client.netUser, null).Replace("%EXPERIENCE%", num7.ToString("N0")).Replace("%VICTIM%", userData.UserName), null, 0f);
						        }
					        }
				        }
			        }
			        if (damage.damageTypes == (DamageTypeFlags)0)
			        {
				        if (component4 != null && component4.IsBleeding())
				        {
					        text7 = Config.GetMessageDeath(Messages.RuMessages.RuMessage.PlayerDeathBleeding, victimPlayer.netUser, null, null);
				        }
				        else if (component3 != null && component3.HasRadiationPoisoning())
				        {
					        text7 = Config.GetMessageDeath(Messages.RuMessages.RuMessage.PlayerDeathRadiation, victimPlayer.netUser, null, null);
				        }
				        else if (component3 != null && component3.IsPoisoned())
				        {
					        text7 = Config.GetMessageDeath(Messages.RuMessages.RuMessage.PlayerDeathPoison, victimPlayer.netUser, null, null);
				        }
				        else if (component3 != null && component3.GetCalorieLevel() <= 0f)
				        {
					        text7 = Config.GetMessageDeath(Messages.RuMessages.RuMessage.PlayerDeathHunger, victimPlayer.netUser, null, null);
				        }
				        else if (component3 != null && component3.IsCold())
				        {
					        text7 = Config.GetMessageDeath(Messages.RuMessages.RuMessage.PlayerDeathCold, victimPlayer.netUser, null, null);
				        }
				        else
				        {
					        text7 = Config.GetMessageDeath(Messages.RuMessages.RuMessage.PlayerDeathBleeding, victimPlayer.netUser, null, null);
				        }
			        }
			        else
			        {
				        DamageTypeFlags damageTypes2 = damage.damageTypes;
				        switch (damageTypes2)
				        {
					        case DamageTypeFlags.damage_generic:
						        text7 = text7.Replace("%WEAPON%", "Melee");
						        break;
					        case DamageTypeFlags.damage_bullet:
						        if (weaponImpact != null)
						        {
							        text7 = text7.Replace("%WEAPON%", text3);
						        }
						        break;
					        case DamageTypeFlags.damage_generic | DamageTypeFlags.damage_bullet:
						        break;
					        case DamageTypeFlags.damage_melee:
						        if (weaponImpact == null)
						        {
							        text3 = "Hunting Bow";
						        }
						        text7 = text7.Replace("%WEAPON%", text3);
						        break;
					        default:
						        if (damageTypes2 == DamageTypeFlags.damage_explosion)
						        {
							        if (damage.attacker.id.name.StartsWith("F1Grenade"))
							        {
								        text3 = "F1 Grenade";
							        }
							        if (damage.attacker.id.name.StartsWith("ExplosiveCharge"))
							        {
								        text3 = "Explosive Charge";
							        }
							        text7 = text7.Replace("%WEAPON%", text3);
						        }
						        break;
				        }
				        string niceName2 = damage.bodyPart.GetNiceName();
				        niceName2 = Messages.RuMessages.BodyPart[niceName2];
				        text7 = text7.Replace("%BODYPART%", niceName2);
				        text7 = text7.Replace("%DISTANCE%", num4.ToString("N1"));
				        text7 = text7.Replace("%DAMAGE%", damage.amount.ToString("0.0"));
			        }
			        if (flag4)
			        {
				        Broadcast.MessageAll(Config._Settings.DeathName, text7);
			        }
			        Helper.LogChat(text7, false);
		        }
	        }
	        else
	        {
		        text = Helper.NiceName(damage.victim.character.name);
		        if (damage.amount >= takeDamage.health && ((userData2 != null) ? userData2.Clan : null) != null)
		        {
			        float num8 = 0f;
			        if (text.Equals("Chicken", StringComparison.OrdinalIgnoreCase))
			        {
				        num8 = Math.Abs(1f * Clans.ExperienceMultiplier);
			        }
			        else if (text.Equals("Rabbit", StringComparison.OrdinalIgnoreCase))
			        {
				        num8 = Math.Abs(1f * Clans.ExperienceMultiplier);
			        }
			        else if (text.Equals("Boar", StringComparison.OrdinalIgnoreCase))
			        {
				        num8 = Math.Abs(3f * Clans.ExperienceMultiplier);
			        }
			        else if (text.Equals("Stag", StringComparison.OrdinalIgnoreCase))
			        {
				        num8 = Math.Abs(5f * Clans.ExperienceMultiplier);
			        }
			        else if (text.Equals("Wolf", StringComparison.OrdinalIgnoreCase))
			        {
				        num8 = Math.Abs(10f * Clans.ExperienceMultiplier);
			        }
			        else if (text.Equals("Bear", StringComparison.OrdinalIgnoreCase))
			        {
				        num8 = Math.Abs(20f * Clans.ExperienceMultiplier);
			        }
			        else if (text.Equals("Mutant Wolf", StringComparison.OrdinalIgnoreCase))
			        {
				        num8 = Math.Abs(15f * Clans.ExperienceMultiplier);
			        }
			        else if (text.Equals("Mutant Bear", StringComparison.OrdinalIgnoreCase))
			        {
				        num8 = Math.Abs(30f * Clans.ExperienceMultiplier);
			        }
			        else
			        {
				        ConsoleSystem.LogWarning("[WARNING] Creature '" + text + "' not have experience for death.");
			        }

			        text = Messages.RuMessages.Names[text];
			        if (num8 >= 0f && num8 >= 1f)
			        {
				        Clans.Find(userData2.Clan).Experience += (ulong)num8;
				        if (Clans.Find(userData2.Clan).Members[userData2].Has(ClanMemberFlags.ExpDetails))
				        {
					        Broadcast.Message(client.netPlayer, Config.GetMessage(Messages.RuMessages.RuMessage.ClanExperienceMurder, client.netUser, null).Replace("%EXPERIENCE%", num8.ToString("N0")).Replace("%VICTIM%", text), null, 0f);
				        }
			        }
		        }
	        }
	        if (damage.damageTypes != (DamageTypeFlags)0 && damage.amount != 0f && !Config._Settings.OverrideDamage && (!float.IsInfinity(damage.amount) || !(damage.attacker.id == damage.victim.id)))
	        {
		        IDBase id = damage.attacker.id;
		        if (id != null)
		        {
			        SpikeWall spikeWall;
			        if ((spikeWall = (id as SpikeWall)) != null)
			        {
				        string text8 = spikeWall.baseReturnDmg.ToString(CultureInfo.InvariantCulture);
				        Helper.Log(string.Concat(new object[]
				        {
					        "Damage: ",
					        damage.attacker,
					        " owned ",
					        damage.attacker.client,
					        " hit ",
					        damage.victim.idMain,
					        "[",
					        damage.victim.networkViewID,
					        "] on ",
					        damage.amount,
					        "(",
					        text8,
					        ") pts."
				        }), false);
				        goto IL_1E6E;
			        }
			        TimedGrenade timedGrenade;
			        if ((timedGrenade = (id as TimedGrenade)) != null)
			        {
				        string text9 = timedGrenade.damage.ToString(CultureInfo.InvariantCulture);
				        Helper.Log(string.Concat(new object[]
				        {
					        "Damage: ",
					        damage.attacker,
					        " owned ",
					        damage.attacker.client,
					        " hit ",
					        damage.victim.idMain,
					        "[",
					        damage.victim.networkViewID,
					        "] on ",
					        damage.amount,
					        "(",
					        text9,
					        ") pts."
				        }), false);
				        goto IL_1E6E;
			        }
			        TimedExplosive timedExplosive;
			        if ((timedExplosive = (id as TimedExplosive)) != null)
			        {
				        string text10 = timedExplosive.damage.ToString(CultureInfo.InvariantCulture);
				        Helper.Log(string.Concat(new object[]
				        {
					        "Damage: ",
					        damage.attacker,
					        " owned ",
					        damage.attacker.client,
					        " hit ",
					        damage.victim.idMain,
					        "[",
					        damage.victim.networkViewID,
					        "] on ",
					        damage.amount,
					        "(",
					        text10,
					        ") pts."
				        }), false);
				        goto IL_1E6E;
			        }
		        }
		        if (damage.attacker.client && weaponImpact != null)
		        {
			        string text11 = weaponImpact.dataBlock.damageMin.ToString() + "-" + weaponImpact.dataBlock.damageMax.ToString();
			        Helper.Log(string.Concat(new object[]
			        {
				        "Damage: ",
				        damage.attacker,
				        "[",
				        damage.attacker.networkViewID,
				        "] from ",
				        weaponImpact.dataBlock.name,
				        " hit ",
				        damage.victim.idMain,
				        "[",
				        damage.victim.networkViewID,
				        "] on ",
				        damage.amount,
				        "(",
				        text11,
				        ") pts."
			        }), false);
		        }
		        else if (damage.attacker.client && weaponImpact == null)
		        {
			        string text12 = "75";
			        Helper.Log(string.Concat(new object[]
			        {
				        "Damage: ",
				        damage.attacker,
				        "[",
				        damage.attacker.networkViewID,
				        "] from Hunting Bow hit ",
				        damage.victim.idMain,
				        "[",
				        damage.victim.networkViewID,
				        "] on ",
				        damage.amount,
				        "(",
				        text12,
				        ") pts."
			        }), false);
		        }
	        }
	        IL_1E6E:
	        if (Economy.EData.Enabled && !flag2 && damage.amount >= takeDamage.health)
	        {
		        Economy.HurtKilled(damage);
	        }
	        return true;
        }
    }

    
    public static string RustLogFileName;
    public static StreamWriter RustLogStream;
    public static string ChatLogFileName;
    public static StreamWriter ChatLogStream;

    private static long _seed;

    public static string RustLogFile =>
        Path.Combine("serverdata\\logs", "Rust" + DateTime.Now.Date.ToString("yyyy-MM-dd") + ".log");

    public static string ChatLogFile =>
        Path.Combine("serverdata\\logs", "Chat" + DateTime.Now.Date.ToString("yyyy-MM-dd") + ".log");

    public static uint NewSerial => (uint)NewSerial64;
    private static ulong NewSerial64 => (ulong)(DateTime.Now.Ticks ^ (_seed += 0x1));

    public static void Log(string msg, bool inConsole = true)
    {
        if (RustLogFile != RustLogFileName && RustLogStream != null)
        {
            RustLogStream.Close();
            RustLogStream = null;
        }

        if (RustLogStream == null)
            RustLogStream =
                new StreamWriter(new FileStream(RustLogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
        if (RustLogStream != null)
        {
            RustLogStream.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ": " + msg);
            RustLogStream.Flush();
            File.SetLastWriteTime(RustLogFile, DateTime.Now);
        }

        if (inConsole) ConsoleSystem.Print(msg);
    }

    public static void LogChat(string msg, bool inConsole = false)
    {
        if (ChatLogFile != ChatLogFileName && ChatLogStream != null)
        {
            ChatLogStream.Close();
            ChatLogStream = null;
        }

        if (ChatLogStream == null)
            ChatLogStream =
                new StreamWriter(new FileStream(ChatLogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
        if (ChatLogStream != null)
        {
            ChatLogStream.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ": " + msg);
            ChatLogStream.Flush();
            File.SetLastWriteTime(ChatLogFile, DateTime.Now);
        }

        if (inConsole) ConsoleSystem.Print(msg);
    }

    public static DateTime StringToTime(string time, DateTime startTime = new())
    {
        foreach (Match match in Regex.Matches(time, @"(\d+\s*(y|M|d|h|m|s))"))
        {
            if (match.Value.EndsWith("y")) startTime = startTime.AddYears(int.Parse(match.Value.Trim('y')));
            if (match.Value.EndsWith("M")) startTime = startTime.AddMonths(int.Parse(match.Value.Trim('M')));
            if (match.Value.EndsWith("d")) startTime = startTime.AddDays(double.Parse(match.Value.Trim('d')));
            if (match.Value.EndsWith("h")) startTime = startTime.AddHours(double.Parse(match.Value.Trim('h')));
            if (match.Value.EndsWith("m")) startTime = startTime.AddMinutes(double.Parse(match.Value.Trim('m')));
            if (match.Value.EndsWith("s")) startTime = startTime.AddSeconds(double.Parse(match.Value.Trim('s')));
        }

        return startTime;
    }

    public static void TeleportTo(NetUser user, Vector3 vector3)
    {
        var management = RustServerManagement.Get();
        management.TeleportPlayerToWorld(user.playerClient.netPlayer, vector3);
    }

    public static List<string> GetAvailableKits(NetUser user)
    {
        return GetAvailableKits(user.userID);
    }

    public static List<string> GetAvailableKits(PlayerClient player)
    {
        return GetAvailableKits(player.userID);
    }

    public static List<string> GetAvailableKits(ulong userID)
    {
        return GetAvailableKits(Data.FindUser(userID));
    }

    public static List<string> GetAvailableKits(User user)
    {
        return Kits.KitData.FindAll(f => f.Ranks.Contains(user.Rank)).Select(kit => kit.KitName).ToList();
    }


    public static List<string> GetAvailableCommands(NetUser user)
    {
        return GetAvailableCommands(user.userID);
    }

    public static List<string> GetAvailableCommands(PlayerClient player)
    {
        return GetAvailableCommands(player.userID);
    }

    public static List<string> GetAvailableCommands(ulong userID)
    {
        return GetAvailableCommands(Data.FindUser(userID));
    }

    public static List<string> GetAvailableCommands(User user)
    {
        var tempCmdList = CommandHook.CommandList.FindAll(f => f.Ranks.Contains(user.Rank))
            .Select(command => command.CmdName).ToList();
        tempCmdList.AddRange(Configs.Commands.CommandList
            .FindAll(f => f.CmdRanks.Contains(user.Rank) && !tempCmdList.Contains(f.CmdName))
            .Select(command => command.CmdName));

        var admList = Configs.Commands.CommandList.Select(f => f.CmdName).ToList();
        admList.AddRange(CommandHook.CommandList.Select(f => f.CmdName).Where(f => !tempCmdList.Contains(f)));

        return NetUser.FindByUserID(user.SteamID).admin ? admList : tempCmdList;
    }


    public static GameObject GetLookObject(NetUser player, int layerMask = -1)
    {
        return player == null ? null : GetLookObject(player.playerClient);
    }

    public static GameObject GetLookObject(PlayerClient player, int layerMask = -1)
    {
        return player == null ? null : GetLookObject(player.controllable);
    }

    public static GameObject GetLookObject(Controllable controllable, int layerMask = -1)
    {
        return controllable == null ? null : GetLookObject(controllable.character);
    }

    public static GameObject GetLookObject(Character character, int layerMask = -1)
    {
        if (character == null) return null;
        var origin = character.transform.position;
        var direction = character.eyesRay.direction;
        origin.y += character.stateFlags.crouch ? 1.0f : 1.85f;
        return GetLookObject(new Ray(origin, direction));
    }

    public static GameObject GetLookObject(Ray ray, float distance = 300f, int layerMask = -1)
    {
        return GetLookObject(ray, out _, distance, layerMask);
    }

    public static GameObject GetLookObject(Ray ray, out Vector3 point, float distance = 300f, int layerMask = -1)
    {
        point = Vector3.zero;
        if (!Facepunch.MeshBatch.MeshBatchPhysics.Raycast(ray, out var hit, distance, layerMask, out var merged,
                out var instance))
            return null;
        var id = merged ? instance.idMain : IDBase.GetMain(hit.collider);
        point = hit.point;
        return id != null ? id.gameObject : hit.collider.gameObject;
    }

    public static int DestroyStructure(StructureMaster master)
    {
        if (master == null) return -1;
        var result = master._structureComponents.Count;
        foreach (var component in master._structureComponents) TakeDamage.HurtSelf(component, float.MaxValue);
        if (master._structureComponents.Count == 0) NetCull.Destroy(master.gameObject);
        return result;
    }

    #region [Public] Get GameObject by Line

    public static GameObject GetLineObject(Vector3 start, Vector3 end, out Vector3 point, int layerMask = -1)
    {
        point = Vector3.zero;
        if (!Facepunch.MeshBatch.MeshBatchPhysics.Linecast(start, end, out var hit, layerMask, out var merged,
                out var instance))
            return null;
        var id = merged ? instance.idMain : IDBase.GetMain(hit.collider);
        point = hit.point;
        return id != null ? id.gameObject : hit.collider.gameObject;
    }

    #endregion

    public static void InventoryItemRemove(PlayerClient player, ItemDataBlock item)
    {
        var inv = player.controllable.GetComponent<Inventory>();
        if (inv == null || item == null || item.transferable) return;
        var invItem = inv.FindItem(item);
        while (!ReferenceEquals(invItem, null))
        {
            inv.RemoveItem(invItem);
            invItem = inv.FindItem(item);
        }
    }

    #region [Public] Remove amount of item from specified player inventory

    public static int InventoryItemRemove(Inventory inventory, ItemDataBlock datablock, int quantity)
    {
        var consumed = 0;
        while (consumed < quantity)
        {
            var item = inventory.FindItem(datablock);
            if (item == null) break;
            if (!item.datablock.IsSplittable())
            {
                consumed++;
                inventory.RemoveItem(item);
                continue;
            }

            var remained = quantity - consumed;
            if (item.uses > remained)
            {
                consumed += remained;
                item.SetUses(item.uses - remained);
            }
            else
            {
                consumed += item.uses;
                inventory.RemoveItem(item);
            }
        }

        return consumed;
    }

    #endregion

    public static List<IInventoryItem> InventoryGetItems(Inventory inventory)
    {
        var result = new List<IInventoryItem>();
        var iterator = inventory.occupiedIterator;
        while (iterator.Next()) result.Add(iterator.item);
        return result;
    }

    public static int InventoryItemCount(Inventory inventory, ItemDataBlock datablock)
    {
        var result = 0;
        var iterator = inventory.occupiedIterator;
        while (iterator.Next())
            if (iterator.item.datablock == datablock)
            {
                if (iterator.item.datablock.IsSplittable())
                    result += iterator.item.uses;
                else result += 1;
            }

        return result;
    }

    public static List<Vector3> GetPlayerSpawns(NetUser netUser, bool Valid = true)
    {
        return GetPlayerSpawns(netUser.userID, Valid);
    }

    public static List<Vector3> GetPlayerSpawns(PlayerClient player, bool Valid = true)
    {
        return GetPlayerSpawns(player.userID, Valid);
    }

    public static List<Vector3> GetPlayerSpawns(ulong userID, bool Valid = true)
    {
        var Result = new List<Vector3>();
        var RustManagement = RustServerManagement.Get();
        foreach (var Obj in RustManagement.playerSpawns)
        {
            if (Obj.ownerID != userID) continue;
            var Spawn = Obj.GetComponent<DeployedRespawn>();
            if (Spawn == null || (Valid && !Spawn.IsValidToSpawn())) continue;
            Result.Add(Spawn.GetSpawnPos() + new Vector3(0f, 0.50f, 0f));
        }

        return Result;
    }

    public static PlayerClient GetPlayerClient(string value)
    {
        var userName = value.Replace("*", "");
        if (ulong.TryParse(value, out var userID) && PlayerClient.FindByUserID(userID, out var result)) return result;
        var comparison = Config._Settings.UniqueNames ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (value.StartsWith("*") && value.EndsWith("*"))
            return PlayerClient.All.Find(p => p.netUser.displayName.Contains(userName));
        if (value.StartsWith("*"))
            return PlayerClient.All.Find(p => p.netUser.displayName.EndsWith(userName, comparison));
        return value.EndsWith("*")
            ? PlayerClient.All.Find(p => p.netUser.displayName.StartsWith(userName, comparison))
            : PlayerClient.All.Find(p => p.netUser.displayName.Equals(userName, comparison));
    }

    public static PlayerClient GetPlayerClient(ulong SteamID)
    {
        PlayerClient Result;
        PlayerClient.FindByUserID(SteamID, out Result);
        return Result;
    }

    public static bool CreateFileBackup(string filename)
    {
        if (!File.Exists(filename)) return false;
        if (Config._Settings.SaveBackupCount > 0)
        {
            var count = Config._Settings.SaveBackupCount - 1;
            if (File.Exists(filename + ".old." + count)) File.Delete(filename + ".old." + count);
            for (var i = count - 1; i >= 0; i--)
                if (File.Exists(filename + ".old." + i))
                    File.Move(filename + ".old." + i, filename + ".old." + (i + 1));
            File.Move(filename, filename + ".old.0");
        }

        if (File.Exists(filename)) File.Delete(filename);
        return true;
    }

    #region [Public] Give item (by Name) for specificed player

    public static int GiveItem(PlayerClient player, string itemName, int quantity = 1, int slots = -1)
    {
        return GiveItem(player, DatablockDictionary.GetByName(itemName), quantity, slots);
    }

    #endregion

    #region [Public] Give item (by ItemDataBlock) for player

    public static int GiveItem(PlayerClient player, ItemDataBlock itemData, int quantity = 1, int modSlots = -1)
    {
        var inventory = player.controllable.GetComponent<PlayerInventory>();
        var slotPreference = Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Default, itemData.IsSplittable(),
            Inventory.Slot.Kind.Belt);
        return GiveItem(inventory, itemData, slotPreference, quantity, modSlots);
    }

    #endregion

    #region [Public] Give item (by ItemDataBlock) for inventory

    public static int GiveItem(PlayerInventory inventory, ItemDataBlock itemData,
        Inventory.Slot.Preference slotPreference, int quantity = 1, int modSlots = -1)
    {
        var result = 0;
        if (itemData == null || inventory == null) return result;
        if (itemData.IsSplittable())
        {
            result += quantity -
                      inventory.AddItemAmount(itemData, quantity, Inventory.AmountMode.Default, slotPreference);
        }
        else
        {
            int maxEligableSlots = itemData.GetMaxEligableSlots();
            for (var i = 0; i < quantity; i++)
            {
                var invItem = inventory.AddItem(itemData, slotPreference, itemData._spawnUsesMax);
                if (ReferenceEquals(invItem, null)) break;
                result++;
                if (modSlots == -1 || maxEligableSlots == 0) continue;
                var heldItem = invItem as IHeldItem;
                heldItem?.SetTotalModSlotCount(Mathf.Min(modSlots, maxEligableSlots));
            }
        }

        return result;
    }

    #endregion

    public static NetUser GetNetUser(string Value)
    {
        var Result = GetPlayerClient(Value);
        return Result != null ? Result.netUser : null;
    }

    public static string[] WarpChatText(string input, int maxlength = 80, string prefix = "", string suffix = "")
    {
        var Lines = input.Split(' ');
        var Result = new List<string>();
        var Build = new StringBuilder();

        if (Lines.Length > 1)
        {
            foreach (var word in Lines)
            {
                if (word.Length + Build.Length > maxlength)
                {
                    if (Build.Length > 0) Result.Add(Build.ToString().TrimStart());
                    Build.Length = 0;
                    Build.Capacity = 0;
                }

                Build.Append(' ' + word);
            }

            if (Build.Length > 0) Result.Add(prefix + Build.ToString().TrimStart() + suffix);
        }
        else
        {
            Result.Add(prefix + input + suffix);
        }

        return Result.ToArray();
    }

    public static string ReplaceVariables(NetUser netUser, string text, string varFrom = null, string varTo = "")
    {
        if (!string.IsNullOrEmpty(varFrom) && text.Contains(varFrom)) text = text.Replace(varFrom, varTo);
        if (netUser != null && text.Contains("%USERNAME%")) text = text.Replace("%USERNAME%", netUser.displayName);
        if (netUser != null && text.Contains("%STEAM_ID%"))
            text = text.Replace("%STEAM_ID%", netUser.userID.ToString());
        if (text.Contains("%MAXPLAYERS%")) text = text.Replace("%MAXPLAYERS%", NetCull.maxConnections.ToString());
        if (text.Contains("%SERVERNAME%")) text = text.Replace("%SERVERNAME%", Config._Settings.ServerName);
        if (text.Contains("%ONLINE%")) text = text.Replace("%ONLINE%", PlayerClient.All.Count.ToString());
        return text;
    }

    public static string NiceName(string input)
    {
        input = input.Replace("_A", "").Replace("A(Clone)", "").Replace("(Clone)", "");
        var matches = new Regex("([A-Z]*[^A-Z_]+)", RegexOptions.Compiled).Matches(input);
        var result = new string[matches.Count];
        for (var i = 0; i < result.Length; i++) result[i] = matches[i].Groups[0].Value.Trim();
        return string.Join(" ", result);
    }

    public static int GetPlayerComponents(ulong userID)
    {
        return StructureMaster.AllStructures.Where(master => master.ownerID == userID)
            .Sum(master => master._structureComponents.Count);
    }

    public static string GetChatTextColor(string color)
    {
        var rgb = color.Replace("#", "").Replace("$", "").ToInt32();
        return rgb == 0 ? "" : "[COLOR#" + rgb.ToHex(false) + "]";
    }

    public static string QuoteSafe(string text)
    {
        if (text.StartsWith("\"") && text.EndsWith("\"")) text = text.Trim('"');
        return "\"" + text.Replace("\"", "\\\"") + "\"";
    }

    public static string[] SplitQuotes(string input, char separator = ' ')
    {
        input = input.Replace("\\\"", "&qute;");
        var matches = new Regex("\"([^\"]+)\"|'([^']+)'|([^" + separator + "]+)", RegexOptions.Compiled).Matches(input);
        var result = new string[matches.Count];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = matches[i].Groups[0].Value.Trim(' ', '\t', '"');
            result[i] = result[i].Replace("&qute;", "\"");
        }

        return result;
    }

    #region [Public] Get Ray by Player Eyes

    public static Ray GetLookRay(NetUser player)
    {
        return player == null ? new Ray() : GetLookRay(player.playerClient);
    }

    public static Ray GetLookRay(PlayerClient player)
    {
        return player == null ? new Ray() : GetLookRay(player.controllable);
    }

    public static Ray GetLookRay(Controllable controllable)
    {
        return controllable == null ? new Ray() : GetLookRay(controllable.character);
    }

    public static Ray GetLookRay(Character character)
    {
        if (character == null) return new Ray();
        var origin = character.transform.position;
        var direction = character.eyesRay.direction;
        origin.y += character.stateFlags.crouch ? 0.85f : 1.65f;
        return new Ray(origin, direction);
    }

    #endregion

    #region [Public] Get Player EyesRay

    public static Ray GetEyesRay(NetUser player)
    {
        return player == null ? new Ray() : GetEyesRay(player.playerClient);
    }

    public static Ray GetEyesRay(PlayerClient player)
    {
        return player == null ? new Ray() : GetEyesRay(player.controllable);
    }

    public static Ray GetEyesRay(Controllable controllable)
    {
        return controllable == null ? new Ray() : GetEyesRay(controllable.character);
    }

    public static Ray GetEyesRay(Character character)
    {
        if (character == null) return new Ray();
        var origin = character.transform.position;
        var direction = character.eyesRay.direction;
        origin.y += character.stateFlags.crouch ? 1.1f : 1.6f;
        return new Ray(origin, direction);
    }

    #endregion
}