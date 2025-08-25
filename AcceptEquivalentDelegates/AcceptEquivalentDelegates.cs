using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using FrooxEngine;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace AcceptEquivalentDelegates
{
    [ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
    [BepInDependency(BepInExResoniteShim.PluginMetadata.GUID)]
    public class AcceptEquivalentDelegates : BasePlugin
	{
		public override void Load() { HarmonyInstance.PatchAll(); log = Log; }
		static ManualLogSource log;

		//patching DelegateEditor instead of SyncDelegate<T> since i dont want to deal with generic patching pain
		[HarmonyPatch(typeof(DelegateEditor), nameof(DelegateEditor.TryReceive))]
		class AcceptEquivalentDelegates_Patch
		{
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
			{
				bool find = true;
				foreach (var code in codes)
				{
					if (find && (code.operand as MethodInfo)?.Name == "TrySet")
					{
						find = false;
						yield return new(OpCodes.Call, AccessTools.Method(typeof(AcceptEquivalentDelegates), nameof(MyTrySet)));
					}
					else
					{
						yield return code;
					}
				}
				if (find) log.LogError("TryReceive Transpiler Failed to find target il");
            }
		}
		static bool MyTrySet(ISyncDelegate instance, Delegate target)
		{
			try
			{
                return instance.TrySet(target.Method.CreateDelegate(FirstGeneric(instance.GetType()), target.Target));
			}
			catch
			{
				return false;
			}
		}
		static Type FirstGeneric(Type last)
		{
			var generics = last.GetGenericArguments();
			if (generics != null && generics.Length > 0)
			{
				foreach (var generic in generics)
				{
					if (typeof(Delegate).IsAssignableFrom(generic)) return generic;
				}
			}
			var baseType = last.BaseType;
			if (baseType != null) return FirstGeneric(baseType);
			return null;
		}
	}
}