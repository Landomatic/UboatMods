using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UBOAT.Game;
using UBOAT.Game.Core.Serialization;
using UBOAT.Game.Scene.Camera.ExternalControllers;
using UBOAT.Game.Scene.Camera;
using UBOAT.Game.UI.Periscope;
using UBOAT.Game.Scene.Items;
using UBOAT.Game.Sandbox;

namespace UBOAT.Mods.UzoNullDataGuard
{
    [NonSerializedInGameState]
    public class UzoNullDataGuardLoader : IUserMod
    {
        private static bool loaded;
        public void OnLoaded()
        {
            if (loaded) return;
            try
            {
                MethodInfo target = AccessTools.Method(typeof(PeriscopeController), "StartController");
                MethodInfo transpiler = AccessTools.Method(typeof(UzoNullDataGuardPatch), "Transpiler");
                if (target == null || transpiler == null)
                    throw new InvalidOperationException("Required camera method was not found.");
                Harmony harmony = new Harmony("Landomatic.UzoNullDataGuard");
                harmony.Patch(target, null, null, new HarmonyMethod(transpiler));
                MethodInfo uiTarget = AccessTools.Method(typeof(PeriscopeUI), "OnEnable");
                MethodInfo uiTranspiler = AccessTools.Method(typeof(UzoNullDataGuardPatch), "UiTranspiler");
                if (uiTarget == null || uiTranspiler == null)
                    throw new InvalidOperationException("Optics UI method was not found; only the camera guard is active.");
                HarmonyMethod uiPostfix = new HarmonyMethod(AccessTools.Method(typeof(UzoNullDataGuardPatch), "UiReturned"));
                uiPostfix.priority = Priority.Last;
                harmony.Patch(uiTarget, null, uiPostfix, new HarmonyMethod(uiTranspiler),
                    new HarmonyMethod(AccessTools.Method(typeof(UzoNullDataGuardPatch), "UiFailure")));
                loaded = true;
                Debug.Log("[UzoNullDataGuard] Loaded 0.2.0-test. Missing-data camera and UI name guards installed; no save-file edits.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UzoNullDataGuard] Installation failed or incomplete; inspect earlier match messages for which guard installed.");
                Debug.LogException(ex);
            }
        }
    }

    [NonSerializedInGameState]
    public static class UzoNullDataGuardPatch
    {
        private static readonly HashSet<int> reported = new HashSet<int>();
        private static bool uiFailureReported;
        private static readonly MethodInfo dataGetter = AccessTools.PropertyGetter(typeof(Equipment), "Data");
        private static readonly FieldInfo nameField = AccessTools.Field(typeof(SandboxEquipmentData), "Name");
        private static readonly MethodInfo safeName = AccessTools.Method(typeof(UzoNullDataGuardPatch), "EquipmentNameForView");
        private static readonly FieldInfo storedColour = AccessTools.Field(typeof(Periscope), "reticleIlluminationColor");
        private static readonly FieldInfo databaseField = AccessTools.Field(typeof(Equipment), "equipmentDatabase");

        // Replace only Equipment.get_Data -> SandboxEquipmentData.Name immediately before
        // the selected vanilla name comparison. The receiver and resulting string have
        // the same stack positions. Keep the field-read instruction as a nop so instruction
        // objects, labels and exception metadata remain present.
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RewriteNameRead(instructions, "Binocular", "camera");
        }

        public static IEnumerable<CodeInstruction> UiTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return RewriteNameRead(instructions, "Observation Periscope", "UI initialization");
        }

        private static IEnumerable<CodeInstruction> RewriteNameRead(
            IEnumerable<CodeInstruction> instructions, string expectedName, string targetLabel)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            int match = -1, count = 0;
            for (int i = 0; i + 2 < code.Count; i++)
            {
                if ((code[i].opcode == OpCodes.Callvirt || code[i].opcode == OpCodes.Call)
                    && Equals(code[i].operand, dataGetter)
                    && code[i + 1].opcode == OpCodes.Ldfld && Equals(code[i + 1].operand, nameField)
                    && code[i + 2].opcode == OpCodes.Ldstr && Equals(code[i + 2].operand, expectedName))
                {
                    count++;
                    match = i;
                }
            }
            if (dataGetter == null || nameField == null || safeName == null || count != 1)
                throw new InvalidOperationException("Expected one " + targetLabel + " name check, found " + count + "; this target left unchanged.");
            // A branch into the second instruction would require the old SandboxEquipmentData
            // stack value. Refuse that shape rather than change its meaning.
            if (code[match + 1].labels.Count != 0 || code[match + 1].blocks.Count != 0)
                throw new InvalidOperationException("Unexpected branch/exception boundary inside name check; this target left unchanged.");
            code[match].opcode = OpCodes.Call;
            code[match].operand = safeName;
            code[match + 1].opcode = OpCodes.Nop;
            code[match + 1].operand = null;
            Debug.Log("[UzoNullDataGuard] Matched one " + targetLabel + " equipment-name check.");
            return code;
        }

        // Healthy equipment follows the original Data.Name path exactly. When data is
        // missing, the prefab's equipment type still distinguishes handheld binoculars
        // ("Binocular") from the UZO ("Targeting Sight"). No replacement equipment data,
        // colour, texture or campaign object is assigned here.
        public static string EquipmentNameForView(Equipment equipment)
        {
            if (equipment == null) return null;
            SandboxEquipmentData data = equipment.Data;
            string result = data != null ? data.Name : (equipment.TypeName ?? string.Empty);
            try
            {
                if (reported.Add(equipment.GetInstanceID()))
                    Report(equipment, data, result);
            }
            catch (Exception ex)
            {
                // Diagnostics cannot prevent the guarded name check from completing.
                Debug.LogWarning("[UzoNullDataGuard] Diagnostic failed: " + ex.Message);
            }
            return result;
        }

        private static void Report(Equipment equipment, SandboxEquipmentData data, string result)
        {
            Periscope scope = equipment as Periscope;
            string details = "object='" + equipment.name + "', type='" + equipment.TypeName
                + "', data=" + (data == null ? "NULL" : "'" + data.Name + "'")
                + ", guarded-name='" + result + "'";
            if (scope != null)
            {
                details += ", overlay='" + scope.ScreenOverlayName + "'";
                if (storedColour != null)
                    details += ", stored-field-colour=" + storedColour.GetValue(scope);
                details += ", effective-colour=" + scope.ReticleIlluminationColor;
            }
            Debug.Log("[UzoNullDataGuard] " + details);
            if (data == null)
                Debug.LogWarning("[UzoNullDataGuard] Optics name used the equipment type because data is missing. This does not rebuild equipment data or prove other optics errors are fixed.");
            ReportDatabase(equipment.TypeName);
            ReportOwners(AccessTools.PropertyGetter(typeof(Periscope), "ReticleIlluminationColor"), "reticle colour getter");
            ReportOwners(AccessTools.Method(typeof(PeriscopeController), "StartController"), "optics entry");
        }


        public static void UiReturned()
        {
            try
            {
                MainCamera camera = MainCamera.Instance;
                PeriscopeController controller = camera == null || camera.GenericController == null
                    ? null : camera.GenericController.ExternalController as PeriscopeController;
                Periscope scope = controller == null ? null : controller.Periscope;
                if (scope == null) return;
                Debug.Log("[UzoNullDataGuard] Optics UI initialization returned: type='" + scope.TypeName
                    + "', overlay='" + scope.ScreenOverlayName + "', stored-field-colour="
                    + (storedColour == null ? "unknown" : Convert.ToString(storedColour.GetValue(scope)))
                    + ", effective-colour=" + scope.ReticleIlluminationColor);
                ReportOwners(AccessTools.PropertyGetter(typeof(Periscope), "ReticleIlluminationColor"), "reticle colour getter at UI entry");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UzoNullDataGuard] UI-entry diagnostic failed: " + ex.Message);
            }
        }

        public static Exception UiFailure(Exception __exception)
        {
            if (__exception != null && !uiFailureReported)
            {
                uiFailureReported = true;
                try
                {
                    Debug.LogError("[UzoNullDataGuard] Optics UI still threw after the name guard: " + __exception);
                    ReportOwners(AccessTools.Method(typeof(PeriscopeUI), "OnEnable"), "optics UI initialization");
                }
                catch (Exception) { }
            }
            // Preserve failures for the game's normal exception handling.
            return __exception;
        }

        private static void ReportDatabase(string typeName)
        {
            object database = databaseField == null ? null : databaseField.GetValue(null);
            if (database == null)
            {
                Debug.Log("[UzoNullDataGuard] Equipment database is unavailable.");
                return;
            }
            FieldInfo byNameField = AccessTools.Field(database.GetType(), "equipmentByName");
            FieldInfo loadedField = AccessTools.Field(database.GetType(), "loaded");
            IDictionary byName = byNameField == null ? null : byNameField.GetValue(database) as IDictionary;
            string hasType = byName == null || typeName == null ? "unknown" : byName.Contains(typeName).ToString();
            Debug.Log("[UzoNullDataGuard] Equipment database: loaded="
                + (loadedField == null ? "unknown" : Convert.ToString(loadedField.GetValue(database)))
                + ", entries=" + (byName == null ? "unknown" : byName.Count.ToString())
                + ", contains '" + typeName + "'=" + hasType);
        }

        private static void ReportOwners(MethodInfo method, string label)
        {
            if (method == null) return;
            var info = Harmony.GetPatchInfo(method);
            Debug.Log("[UzoNullDataGuard] " + label + " patch owners: "
                + (info == null ? "none" : string.Join(", ", info.Owners)));
        }
    }
}
