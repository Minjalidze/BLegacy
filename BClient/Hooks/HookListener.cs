using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BClient.AntiCheat;
using BClient.UserReferences;
using UnityEngine;
using BitStream = uLink.BitStream;
using Object = UnityEngine.Object;

namespace BClient.Hooks
{
	internal class HookListener
	{
		internal static List<string> AA = new();
		internal class Mod
		{
			public string Name;
			public byte[] Bytes;
			public List<string> Args;
		}
		internal static readonly List<Mod> Mods = new();
		internal static readonly List<uint> HookIDs = new();
        internal static readonly Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
		
		internal static Thread ClickRecordThread;
		internal static List<Type> ClickQueue = new();

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
		private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);
		
		private static IntPtr LoadWin32Library(string libPath)
		{
			if (string.IsNullOrEmpty(libPath)) throw new ArgumentNullException(nameof(libPath));

			var moduleHandle = LoadLibrary(libPath);
			if (moduleHandle != IntPtr.Zero) return moduleHandle;

			var lastError = Marshal.GetLastWin32Error();
			var innerEx = new Win32Exception(lastError);
			innerEx.Data.Add("LastWin32Error", lastError);

			throw new Exception("can't load DLL " + libPath, innerEx);
		}
		
		[HookMethod(typeof(RPOSInventoryCell), "OnClick")]
		private static void OnClick(RPOSInventoryCell hook)
		{
			if (!NetCull.isClientRunning || PlayerClient.GetLocalPlayer() == null) return;
			if (Input.GetKey(KeyCode.LeftShift))
			{
				UserReferences.FastLoot.OnSlotClick(hook._mySlot, hook._displayInventory == PlayerClient.GetLocalPlayer().rootControllable.idMain.GetComponent<Inventory>());
			}
		}
		
		[HookMethod(typeof(ClientConnect), "uLink_OnConnectedToServer")]
		private static void uLink_OnConnectedToServer(ClientConnect hook)
		{
			Bootstrapper.LoaderObject.AddComponent<SocketConnection>();
			
			LoadingScreen.Update("Сonnected!");

			var bitStream = new BitStream((byte[])NetCull.approvalData.ReadObject(typeof(byte[]).TypeHandle), false);
			var text = bitStream.ReadString();

			NetCull.sendRate = bitStream.ReadSingle();

			var str = bitStream.ReadString();

			bitStream.ReadBoolean();
			bitStream.ReadBoolean();

			var flag = bitStream.bytesRemaining > 0b1000;
			if (flag)
			{
				var serverId = bitStream.ReadUInt64();
				var serverIp = bitStream.ReadUInt32();
				var serverPort = bitStream.ReadInt32();

				SteamClient.SteamUser_AdvertiseGame(serverId, serverIp, serverPort);
			}

			Debug.Log("Server Name: \"" + str + "\"");

			NetCull.isMessageQueueRunning = false;
			hook.StartCoroutine(hook.LoadLevel(text));

			var modCount = bitStream.ReadInt32();
			string modName;
			if (modCount > 0)
			{
				for (var i = 0b1; i <= modCount; i++)
				{
					modName = bitStream.ReadString();
					Mods.Add(new Mod
					{
						Name = modName,
						Bytes = bitStream.ReadBytes(),
					});
					var mod = Mods.Find(f => f.Name == modName);

					var argsCount = bitStream.ReadInt32();
					mod.Args = new List<string>();
					for (var z = 0b0; z < argsCount; z++) mod.Args.Add(bitStream.ReadString());

					var modArgs = mod.Args[0b0].Split('.');

					var assembly = Assembly.Load(mod.Bytes);
					
					var t = assembly.GetType($"{modArgs[0b0]}.{modArgs[0b1]}");
					var methodInfo = t.GetMethod($"{modArgs[0b10]}");
					
					mod.Name = assembly.GetName().Name;
					var constructorParameters = new object[0b0];
					
					var o = Activator.CreateInstance(t, constructorParameters);

					var parameters = new object[0b0];
					methodInfo?.Invoke(o, parameters);
					
					HookIDs.Add(CRC32.Quick(mod.Bytes));
					if (!AA.Contains(assembly.ManifestModule.Name)) AA.Add(assembly.ManifestModule.Name);
					if (!AA.Contains(assembly.GetName().Name)) AA.Add(assembly.GetName().Name);
					if (!AA.Contains(new FileInfo(assembly.ManifestModule.FullyQualifiedName).Name)) AA.Add(new FileInfo(assembly.ManifestModule.FullyQualifiedName).Name);
					Debug.Log($"[color lime][{str}]Loaded Mod (C#): {modName}.");
				}
			}
			modCount = bitStream.ReadInt32();
			if (modCount > 0)
			{
				for (var i = 0b1; i <= modCount; i++)
				{
					modName = bitStream.ReadString();
					var bytes = bitStream.ReadBytes();

					try
					{
						if (File.Exists(Directory.GetCurrentDirectory() + $@"\rust_Data\Plugins\{modName}")) 
							File.Delete(Directory.GetCurrentDirectory() + $@"\rust_Data\Plugins\{modName}");
					
						File.WriteAllBytes(Directory.GetCurrentDirectory() + $@"\rust_Data\Plugins\{modName}", bytes);
						LoadWin32Library(Directory.GetCurrentDirectory() + $@"\rust_Data\Plugins\{modName}");
					
						Debug.Log($"[color lime][{str}]Loaded Mod (C++): {modName}.");
					}
					catch
					{
						// ignored
					}
				}
			}
			DisableOnConnectedState.OnConnected();
		}
		
		[HookMethod(typeof(ClientConnect), "DoConnect")]
		private static bool DoConnect(ClientConnect hook, string strURL, int iPort)
		{
			SteamClient.Needed();
			NetCull.config.timeoutDelay = 60f;
			if (ClientConnect.Steam_GetSteamID() == 0b0UL)
			{
				LoadingScreen.Update("connection failed (no steam detected)");
				Object.Destroy(hook.gameObject);
				return false;
			}

			var intPtr = Marshal.AllocHGlobal(0b10000000000);
			var num = ClientConnect.SteamClient_GetAuth(intPtr, 0b10000000000);
			var array = new byte[num];

			Marshal.Copy(intPtr, array, 0b0, (int)num);
			Marshal.FreeHGlobal(intPtr);
			
            var bitStream = new BitStream(false);
			bitStream.WriteInt32(int.Parse(Connector.GetClientID()));
			bitStream.WriteByte(0b10);
			bitStream.WriteUInt64(ClientConnect.Steam_GetSteamID());
			bitStream.WriteString(Marshal.PtrToStringAnsi(ClientConnect.Steam_GetDisplayname()));
			bitStream.WriteBytes(array);
			try
			{
				var netError = NetCull.Connect(strURL, iPort, string.Empty, bitStream);
				if (netError != NetError.NoError)
				{
					LoadingScreen.Update("connection failed (" + netError + ")");
					Object.Destroy(hook.gameObject);
					return false;
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				Object.Destroy(hook.gameObject);
				return false;
			}

			SteamClient.SteamClient_OnJoinServer(strURL, iPort);
			return true;
		}
	}
}
