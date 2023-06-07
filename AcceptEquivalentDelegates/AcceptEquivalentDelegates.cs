using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AcceptEquivalentDelegates
{
	public class AcceptEquivalentDelegates : NeosMod
	{
		public override string Name => "AcceptEquivalentDelegates";
		public override string Author => "eia485";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/EIA485/NeosAcceptEquivalentDelegates/";
		public override void OnEngineInit()
		{
			new Harmony("net.eia485.AcceptEquivalentDelegates").PatchAll();
		}

		//patching DelegateEditor instead of SyncDelegate<T> since i dont want to deal with generic pathcing pain
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