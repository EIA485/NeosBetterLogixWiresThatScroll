using HarmonyLib;
using NeosModLoader;
using System;
using FrooxEngine;
using FrooxEngine.LogiX;
using BaseX;

namespace BetterLogixWiresThatScroll
{
	public class BetterLogixWiresThatScroll : NeosMod
	{
		public override string Name => "BetterLogixWiresThatScroll";
		public override string Author => "eia485";
		public override string Version => "1.0.1";
		public override string Link => "https://github.com/EIA485/NeosBetterLogixWiresThatScroll/";
		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("net.eia485.BetterLogixWiresThatScroll");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(ConnectionWire))]
		class BetterLogixWiresThatScrollPatch
		{
			[HarmonyPrefix]
			[HarmonyPatch("DeleteHighlight")]
			public static bool DeleteHighlightPatch(SyncRef<FresnelMaterial> ___Material, SyncRef<Slot> ___WireSlot, ConnectionWire __instance)
			{
				Type type = GetWireType(__instance.InputField.Target.GetType());
				___Material.Target = GetWireMaterial(color.Red, type.GetDimensions(), typeof(Impulse)==type, __instance);
				___WireSlot.Target.GetComponent<MeshRenderer>(null, false).Materials[0] = ___Material.Target;
				return false;
			}

			[HarmonyPrefix]
			[HarmonyPatch("SetupStyle")]
			public static bool SetupStylePatch(color color, int dimensions, bool isImpulse, Sync<color> ___TypeColor, SyncRef<FresnelMaterial> ___Material, SyncRef<Slot> ___WireSlot,  ConnectionWire __instance)
			{
				___TypeColor.Value = color;
				___Material.Target = GetWireMaterial(color, dimensions, isImpulse, __instance);
				___WireSlot.Target.GetComponent<MeshRenderer>(null, false).Materials.Add(___Material.Target);
				return false;
			}

			[HarmonyPrefix]
			[HarmonyPatch("SetTypeColor")]
			public static bool SetTypeColorPrefix(SyncRef<FresnelMaterial> ___Material, Sync<color> ___TypeColor, SyncRef<Slot> ___WireSlot, ConnectionWire __instance)
			{
				Type type = GetWireType(__instance.InputField.Target.GetType());
				___Material.Target = GetWireMaterial(___TypeColor, type.GetDimensions(), typeof(Impulse)==type, __instance);
				___WireSlot.Target.GetComponent<MeshRenderer>(null, false).Materials[0] = ___Material.Target;
				return false;
			}

			[HarmonyPrefix]
			[HarmonyPatch("SetupTempWire")]
			public static bool SetupTempWirePatch(Slot targetPoint, bool output, Sync<bool> ___TempWire, SyncRef<FresnelMaterial> ___Material, SyncRef<Slot> ___WireSlot, Sync<color> ___TypeColor, ConnectionWire __instance)
			{
				___TempWire.Value = true;
				__instance.TargetSlot.Target = targetPoint;
				___WireSlot.Target.ActiveSelf = true;
				___Material.Target = ___WireSlot.Target.DuplicateComponent<FresnelMaterial>(___Material.Target, false);
				float2 value = new float2(0, 1);
				___Material.Target.FarTextureScale.Value = value;
				___Material.Target.NearTextureScale.Value = value;
				___WireSlot.Target.GetComponent<MeshRenderer>(null, false).Materials[0] = ___Material.Target;
				Panner2D panner = ___WireSlot.Target.AttachComponent<Panner2D>();
				if (___TypeColor.Value == new color(1, 1, 1, 1))
				{
					panner.Speed = new float2(-1, 0);
				}
				else
				{
					panner.Speed = new float2(1, 0);
				}
				panner.Target = ___Material.Target.NearTextureOffset;
				if (output)
				{
					__instance.SetupAsOutput(true);
				}
				MeshCollider component = ___WireSlot.Target.GetComponent<MeshCollider>(null, false);
				if (!(component == null))
				{
					component.Destroy();
				}
				return false;
			}

			[HarmonyPrefix]
			[HarmonyPatch("OnAttach")]
			public static bool OnAttachPrefix(SyncRef<Slot> ___WireSlot, FieldDrive<float3> ___WirePoint, FieldDrive<float3> ___WireTangent, FieldDrive<floatQ> ___WireOrientation, FieldDrive<float> ___WireWidth, ConnectionWire __instance)
			{
				___WireSlot.Target = __instance.Slot.AddSlot("Wire", true);
				StripeWireMesh stripeWireMesh = ___WireSlot.Target.AttachComponent<StripeWireMesh>(true, null);
				stripeWireMesh.Orientation0.Value = floatQ.Euler(0f, 0f, -90f);
				SyncField<float3> tangent = stripeWireMesh.Tangent0;
				float3 left = float3.Left;
				tangent.Value = (left) * 0.25f;
				stripeWireMesh.Width0.Value = 0.025600001f;
				___WirePoint.Target = stripeWireMesh.Point1;
				___WireTangent.Target = stripeWireMesh.Tangent1;
				___WireOrientation.Target = stripeWireMesh.Orientation1;
				___WireWidth.Target = stripeWireMesh.Width1;
				MeshCollider meshCollider = ___WireSlot.Target.AttachComponent<MeshCollider>(true, null);
				meshCollider.Mesh.Target = stripeWireMesh;
				meshCollider.Sidedness.Value = MeshColliderSidedness.DualSided;
				___WireSlot.Target.AttachComponent<SearchBlock>(true, null);
				___WireSlot.Target.ActiveSelf = false;
				___WireSlot.Target.AttachComponent<MeshRenderer>(true, null).Mesh.Target = stripeWireMesh;
				return false;
			}

		}

		public static FresnelMaterial GetWireMaterial(color color, int dimensions, bool isImpulse, ConnectionWire instance)
		{
			Slot LogixAssets = instance.World.AssetsSlot.FindOrAdd("LogixAssets", true);
			ValueMultiDriver<float2> multiDriver;
			string pannerKey = string.Format("Logix_WirePanner_{0}", isImpulse ? "Impulse" : "Value");
			Panner2D panner = instance.World.KeyOwner(pannerKey) as Panner2D;
			if (panner == null)
			{
				panner = LogixAssets.AttachComponent<Panner2D>(true, null);
				panner.AssignKey(pannerKey, 1, false);
				if (isImpulse)
				{
					panner.Speed = new float2(-1, 0);
				}
				else
				{
					panner.Speed = new float2(1, 0);
				}
				multiDriver = LogixAssets.AttachComponent<ValueMultiDriver<float2>>(true, null);
				panner.Target = multiDriver.Value;
				panner.Offset = new float2(0, 0);
			}
			else
			{
				multiDriver = panner?.Target?.Parent as ValueMultiDriver<float2>;
				if (multiDriver == null)
				{
					multiDriver = LogixAssets.AttachComponent<ValueMultiDriver<float2>>(true, null);
					panner.Target = multiDriver.Value;
					panner.Offset = new float2(0, 0);
				}
			}
			ISyncList listOfDrives = multiDriver.Drives;
			CleanList(listOfDrives);
			string key = string.Format("Logix_WireMaterial_{0}_{1}_{2}", color, isImpulse ? "Impulse" : "Value", dimensions);
			FresnelMaterial fresnelMaterial = instance.World.KeyOwner(key) as FresnelMaterial;
			if (fresnelMaterial == null)
			{
				fresnelMaterial = LogixAssets.AttachComponent<FresnelMaterial>(true, null);
				fresnelMaterial.AssignKey(key, 1, false);
				fresnelMaterial.BlendMode.Value = BlendMode.Alpha;
				fresnelMaterial.ZWrite.Value = ZWrite.On;
				fresnelMaterial.Sidedness.Value = Sidedness.Double;
				StaticTexture2D wireTexture = LogixHelper.GetWireTexture(instance.World, dimensions, isImpulse);
				fresnelMaterial.NearTexture.Target = wireTexture;
				fresnelMaterial.FarTexture.Target = wireTexture;
				float2 value = new float2(0.5f, 1f);
				fresnelMaterial.NearTextureScale.Value = value;
				fresnelMaterial.FarTextureScale.Value = value;
				fresnelMaterial.NearColor.Value = color.MulA(.8f);
				fresnelMaterial.FarColor.Value = color.MulRGB(.5f).MulA(.8f);
				(listOfDrives.AddElement() as FieldDrive<float2>).Target = fresnelMaterial.NearTextureOffset;
			}
			else if (fresnelMaterial.NearTextureOffset.IsDriven | fresnelMaterial.NearTextureOffset.IsLinked)
			{
				if (!((fresnelMaterial.NearTextureOffset.ActiveLink as SyncElement).Component == multiDriver))
				{
					(listOfDrives.AddElement() as FieldDrive<float2>).ForceLink(fresnelMaterial.NearTextureOffset);
				}
			}
			else
				(listOfDrives.AddElement() as FieldDrive<float2>).Target = fresnelMaterial.NearTextureOffset;
			return fresnelMaterial;
		}
		public static Type GetWireType(Type t)
		{
			try
			{
				return t.GetGenericArguments()[0];
			}
			catch
			{
				return t;
			}
		}

		public static void CleanList(ISyncList list)
		{
			for (int i=list.Count-1; i>=0; i--)
			{
				ISyncMember syncMember= list.GetElement(i);
				if (syncMember == null||(syncMember as FieldDrive<float2>)?.Target == null)
				{
					list.RemoveElement(i);
				}
					
			}
		}
	}
}