using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace lathemod.src.Patches {
    [HarmonyPatchCategory("lathemod")]
    internal static class LatheModPatches {
        /*static MethodBase TargetMethod() {
            return AccessTools.Method(
                typeof(Vintagestory.GameContent.ItemBow),
                "OnHeldInteractStop",
                new Type[] {
                typeof(float),
                typeof(ItemSlot),
                typeof(EntityAgent),
                typeof(BlockSelection),
                typeof(EntitySelection)
                });
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            try {
                var codes = new List<CodeInstruction>(instructions);
                var getEntityType = AccessTools.Method(typeof(IWorldAccessor), "GetEntityType", new[] { typeof(AssetLocation) });
                var replacement = AccessTools.Method(typeof(LatheModPatches), "CustomGetArrowEntityType");

                if (getEntityType == null)
                    throw new Exception("getEntityType not found");
                if (replacement == null)
                    throw new Exception("CustomGetArrowEntityType not found");

                for (int i = 0; i < codes.Count; i++) {
                    if (codes[i].Calls(getEntityType)) {
                        if (i >= 10 && codes[i].Calls(getEntityType)) {
                            codes.RemoveRange(i - 10, 11);

                            codes.Insert(i - 10, new CodeInstruction(OpCodes.Ldarg_3));
                            codes.Insert(i - 9, new CodeInstruction(OpCodes.Ldloc_S, 2));
                            codes.Insert(i - 8, new CodeInstruction(OpCodes.Call, replacement));
                        } else {
                            throw new Exception("Too few instructions before GetEntityType");
                        }
                        break;
                    }
                }

                return codes;
            } catch (Exception e) {
                Console.WriteLine("[LatheMod] Transpiler error: " + e);
                throw;
            }
            var codes = new List<CodeInstruction>(instructions);
            var targetMethod = AccessTools.Method(typeof(IWorldAccessor), "GetEntityType", new[] { typeof(AssetLocation) });
            var replacementMethod = AccessTools.Method(typeof(LatheModPatches), "CustomGetArrowEntityType", new[] { typeof(AssetLocation) });

            foreach (var code in codes) {
                Console.WriteLine($"[lathemod transpiler] {code.opcode} {code.operand}");
            }

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].Calls(targetMethod)) {
                    Console.WriteLine("[lathemod transpiler] Replacing GetEntityType at index " + i);
                    codes[i].operand = replacementMethod;
                }
            }
            return codes;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var code = new List<CodeInstruction>(instructions);

            // Target: the call to IWorldAccessor.GetEntityType(AssetLocation)
            var getEntityTypeMethod = AccessTools.Method(typeof(IWorldAccessor), nameof(IWorldAccessor.GetEntityType), new[] { typeof(AssetLocation) });

            // Your replacement method
            var resolveArrowEntityMethod = AccessTools.Method(typeof(LatheModPatches), nameof(LatheModPatches.ResolveArrowEntity));

            for (int i = 0; i < code.Count; i++) {
                // Look for the call to GetEntityType
                if (code[i].opcode == OpCodes.Callvirt && code[i].operand as MethodInfo == getEntityTypeMethod) {
                    // At this point in the IL stack:
                    // - The arrow AssetLocation is already created
                    // We want to replace this with:
                    //   ResolveArrowEntity(stack, byEntity)

                    // Remove the previous instructions that built the AssetLocation (we assume 4 back, adjust if needed)
                    code.RemoveRange(i - 4, 5);

                    // Insert:
                    // ldarg.2 = ItemSlot slot → slot.Itemstack
                    // ldarg.3 = EntityAgent byEntity
                    code.InsertRange(i - 4, new[]
                    {
                    // Load ItemStack from slot (arg2.Itemstack)
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ItemSlot), nameof(ItemSlot.Itemstack))),

                    // Load EntityAgent (arg3)
                    new CodeInstruction(OpCodes.Ldarg_3),

                    // Call the replacement method
                    new CodeInstruction(OpCodes.Call, resolveArrowEntityMethod)
                });

                    break; // patch only once
                }
            }

            return code;
        }

    public static EntityProperties ResolveArrowEntity(ItemStack stack, EntityAgent byEntity) {
            try {
                string refinedCode = stack.ItemAttributes["arrowEntityCodes"].AsString("arrow-refined-" + stack.Collectible.Variant["material"]);
                return byEntity.World.GetEntityType(new AssetLocation(refinedCode));
            } catch (NullReferenceException) {
                try {
                    string normalCode = stack.ItemAttributes["arrowEntityCode"].AsString("arrow-" + stack.Collectible.Variant["material"]);
                    return byEntity.World.GetEntityType(new AssetLocation(normalCode));
                } catch (Exception ex) {
                    Console.WriteLine("[lathemod] Failed to resolve arrow entity code: " + ex);
                    return null;
                }
            }
        }
        public static EntityProperties CustomGetArrowEntityType(EntityAgent byEntity, ItemStack stack) {
            {
                try {
                    string refinedCode = stack.ItemAttributes["arrowEntityCodes"].AsString("arrow-refined-" + stack.Collectible.Variant["material"]);
                    return byEntity.World.GetEntityType(new AssetLocation(refinedCode));
                } catch (NullReferenceException) {
                    try {
                        string normalCode = stack.ItemAttributes["arrowEntityCode"].AsString("arrow-" + stack.Collectible.Variant["material"]);
                        return byEntity.World.GetEntityType(new AssetLocation(normalCode));
                    } catch (Exception ex) {
                        Console.WriteLine("[lathemod] Failed to resolve arrow entity code: " + ex);
                        return null;
                    }
                }
            }
        }*/
    }
}

