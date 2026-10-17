// SPDX-License-Identifier: CC-BY-NC-4.0
// Copyright (c) 2026 Landomatic. See LICENSE and README.txt.
// Companion to Nose Down by Kugler (https://github.com/Isamylov); contains none of its source.
using System;
using System.Text;
using UBOAT.Game.Scene.Entities;
using UnityEngine;

namespace NoseDownCompanion
{
    // Own hull detection (same type-name source Nose Down uses: SandboxEntity.Type.Name).
    internal static class HullKey
    {
        private static PlayerShip _ship;
        internal static string Current { get; private set; }
        // v1.0.15: finer key for Type IX sub-types (TypeIXA, TypeIXB, TypeIXC, TypeIXC40, TypeIXD); else = Current.
        internal static string Variant { get; private set; }

        internal static void Update(PlayerShip ship)
        {
            if (ReferenceEquals(ship, _ship) && Current != null)
                return;
            _ship = ship;
            Current = null;
            Variant = null;
            try
            {
                var se = ship.SandboxEntity;
                string name = se != null && se.Type != null ? se.Type.Name : null;
                if (string.IsNullOrEmpty(name))
                    return;
                Current = Resolve(name);
                Variant = ResolveVariant(name, Current);
                Debug.Log(CompanionLoader.Tag + " hull type=\"" + name + "\" key=" + Current + (Variant != null && Variant != Current ? " variant=" + Variant : "")
                    + " clamp=" + (IsVIIClampHull ? "always" : (IsIIClampHull ? "officer-seated" : "off"))
                    + " standDown=" + (IsStandDownHull ? (CompanionConfig.StandDownClampedOnly ? "when-clamped" : "any-manning") : "off (stock Nose Down)")
                    + (Current == "TypeIX" ? " ixDepthFix=" + (CompanionConfig.IxDepthFix ? "on (ordinary-dive approach brake only)" : "off") : "")
                    + (Current == "TypeIX" ? " ixTargetPin=" + (CompanionConfig.IxTargetPin && IxTargetPin.HullListed ? "on (ordinary dives > " + CompanionConfig.IxPinMinOrder + " m)" : "off") : "")
                    + (Current != "TypeIX" && Array.IndexOf(CompanionConfig.SurfaceBrakeHulls, Current) >= 0 && CompanionConfig.SurfaceBrakeOn ? " surfaceBrake=on" : "")
                    + (IsVIIClampHull ? " viiDeckHandover=" + (CompanionConfig.ViiDeckHandover ? "on" : "off") : "")
                    + (PdHoldDamper.HullListed ? " pdHoldDamp=" + (CompanionConfig.PdHoldDamp ? "on" : "off") : "")
                    + (Current == "TypeVIIC41" ? " c41PlaneDamping=" + (CompanionConfig.C41PlaneDamping ? "on" : "off") : ""));
            }
            catch (Exception e)
            {
                Current = "unknown";
                Debug.LogWarning(CompanionLoader.Tag + " hull detection failed: " + e.Message);
            }
        }

        internal static void Reset()
        {
            _ship = null;
            Current = null;
            Variant = null;
        }

        private static string ResolveVariant(string name, string key)
        {
            if (key != "TypeIX")
                return key;
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
            string n = sb.ToString();
            if (n.StartsWith("TYPEIXC40", StringComparison.Ordinal)) return "TypeIXC40";
            if (n.StartsWith("TYPEIXA", StringComparison.Ordinal)) return "TypeIXA";
            if (n.StartsWith("TYPEIXB", StringComparison.Ordinal)) return "TypeIXB";
            if (n.StartsWith("TYPEIXC", StringComparison.Ordinal)) return "TypeIXC";
            if (n.StartsWith("TYPEIXD", StringComparison.Ordinal)) return "TypeIXD";
            return "TypeIX";
        }

        private static string Resolve(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToUpperInvariant(c));
            string n = sb.ToString();
            if (n.StartsWith("TYPEIIA", StringComparison.Ordinal)) return "TypeIIA";
            if (n.StartsWith("TYPEIID", StringComparison.Ordinal)) return "TypeIID";
            if (n.StartsWith("TYPEVIIC41", StringComparison.Ordinal)) return "TypeVIIC41";
            if (n.StartsWith("TYPEVIIB", StringComparison.Ordinal)) return "TypeVIIB";
            if (n.StartsWith("TYPEVIIC", StringComparison.Ordinal)) return "TypeVIIC";
            if (n.StartsWith("TYPEIX", StringComparison.Ordinal)) return "TypeIX";
            return "other";
        }

        internal static bool IsVIIClampHull
        {
            get { return Current == "TypeVIIB" || Current == "TypeVIIC" || Current == "TypeVIIC41"; }
        }

        // v1.0.7: hulls that may receive the 2-10 m stand-down. Type IX and unknown hulls never do.
        internal static readonly string[] StandDownAllowed = { "TypeVIIB", "TypeVIIC", "TypeVIIC41", "TypeIIA", "TypeIID" };

        internal static bool IsStandDownHull
        {
            get
            {
                if (Current == null || Array.IndexOf(StandDownAllowed, Current) < 0)
                    return false;
                return CompanionConfig.StandDownHulls == null || Array.IndexOf(CompanionConfig.StandDownHulls, Current) >= 0;
            }
        }

        internal static bool IsTypeII
        {
            get { return Current == "TypeIIA" || Current == "TypeIID"; }
        }

        internal static bool IsIIClampHull
        {
            get { return Current == "TypeIIA" || (CompanionConfig.IidOfficerClamp && Current == "TypeIID"); }
        }
    }
}
