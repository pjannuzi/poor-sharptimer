/*
Copyright (C) 2024 Dea Brcka

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer
{
  internal static class WorldTextManager
    {
        internal static Dictionary<uint, int> WorldTextOwners = new();
        internal static Dictionary<uint, (Vector Position, QAngle Angles)> EntityTransforms = new();

        internal static Dictionary<uint, CPointWorldText> WorldTextEntities = new(); 
        
         public static bool IsValidPlayer(CCSPlayerController? p)
        {
            return p != null && p.IsValid && !p.IsBot && !p.IsHLTV && p.Connected == PlayerConnectedState.PlayerConnected;
        }

        public static CCSPlayerPawn? GetPlayerPawn(this CCSPlayerController player)
        {
            return player.PlayerPawn.Value;
        }
        public static CCSPlayerPawnBase? GetPlayerPawnBase(this CCSPlayerController player)
        {
            return player.GetPlayerPawn();
        }

        public static bool hasWorldText(this CCSPlayerController player)
        {
            return WorldTextOwners.ContainsValue(player.UserId!.Value);
        }

        public static bool hasEntity(this CCSPlayerController player)
        {   
            if(WorldTextOwners.ContainsValue(player.UserId!.Value)) {
                var existingEntityIndex = WorldTextOwners.FirstOrDefault(kvp => kvp.Value == player.UserId.Value).Key;
                return WorldTextEntities.ContainsKey(existingEntityIndex);
            }

            return false;
        }

        public static CCSGOViewModel? EnsureCustomView(this CCSPlayerController player, int index)
        {

            CCSPlayerPawnBase? pPawnBase = player.GetPlayerPawnBase();
            if (pPawnBase == null)
            {
                return null;
            }
            ;

            if (pPawnBase.LifeState == (byte)LifeState_t.LIFE_DEAD)
            {

                var playerPawn = player.Pawn.Value;
                if (playerPawn == null || !playerPawn.IsValid)
                {
                    return null;
                }

                if (player.ControllingBot)
                {
                    return null;
                }

                var observerServices = playerPawn.ObserverServices;
                if (observerServices == null)
                {
                    return null;
                }

                var observerPawn = observerServices.ObserverTarget?.Value?.As<CCSPlayerPawn>();
                if (observerPawn == null || !observerPawn.IsValid)
                {
                    return null;
                }

                var observerController = observerPawn.OriginalController.Value;
                if (observerController == null || !observerController.IsValid)
                {
                    return null;
                }

                pPawnBase = observerController.GetPlayerPawnBase();
                if (pPawnBase == null)
                {
                    return null;
                }
            }

            var pawn = pPawnBase as CCSPlayerPawn;
            if (pawn == null)
            {
                return null;
            }

            if (pawn.ViewModelServices == null)
            {
                return null;
            }

            int offset = Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
            IntPtr viewModelHandleAddress = (IntPtr)(pawn.ViewModelServices.Handle + offset + 4);

            var handle = new CHandle<CCSGOViewModel>(viewModelHandleAddress);
            if (!handle.IsValid)
            {
                CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
                viewmodel.DispatchSpawn();
                handle.Raw = viewmodel.EntityHandle.Raw;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_pViewModelServices");
            }

            return handle.Value;
        }

        internal static uint? RemoveText(CCSPlayerController player)
        {
            if (WorldTextOwners.ContainsValue(player.UserId!.Value))
            {
                var existingEntityIndex = WorldTextOwners.FirstOrDefault(kvp => kvp.Value == player.UserId.Value).Key;

                if (existingEntityIndex != default)
                {
                    if (WorldTextEntities.ContainsKey(existingEntityIndex))
                    {
                        CPointWorldText existingEntity = WorldTextEntities[existingEntityIndex];
                        if (existingEntity != null && existingEntity.IsValid)
                        {
                            existingEntity.Remove();
                            WorldTextEntities.Remove(existingEntityIndex);
                            WorldTextOwners.Remove(existingEntityIndex);
                            return existingEntityIndex;
                        }
                    }
                }
            }
            return null;
        }

        internal static CPointWorldText? UpdateText(
            CCSPlayerController player,
            string text
        )
        {
            if (WorldTextOwners.Values.Contains(player.UserId!.Value))
            {
                var existingEntityIndex = WorldTextOwners.FirstOrDefault(kvp => kvp.Value == player.UserId.Value).Key;

                if (existingEntityIndex != default)
                {
                    if (WorldTextEntities.ContainsKey(existingEntityIndex))
                    {
                        CPointWorldText existingEntity = WorldTextEntities[existingEntityIndex];

                        if (existingEntity != null && existingEntity.IsValid)
                        {
                            existingEntity.AcceptInput("SetMessage", existingEntity, existingEntity, $"{text}");
                            WorldTextEntities[existingEntityIndex] = existingEntity;
                            return existingEntity;
                        }
                    }
                }
            }

            return null;
        }

        internal static CPointWorldText? CreateText(
            CCSPlayerController player,
            string text,
            float size = 35,
            Color? color = null,
            string font = "",
            float shiftX = 0f,
            float shiftY = -0.5f
        )
        {
            var viewmodel = player.EnsureCustomView(0);
            if (viewmodel == null)
            {
                return null;
            }

            CCSPlayerPawn pawn = player?.PlayerPawn.Value!;
            QAngle eyeAngles = pawn.EyeAngles;

            Vector forward = new(), right = new(), up = new();
            NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

            Vector offset = new();
            offset += forward * 7;
            offset += right * shiftX;
            offset += up * shiftY;
            QAngle angles = new()
            {
                Y = eyeAngles.Y + 270,
                Z = 90 - eyeAngles.X,
                X = 0
            };

            Vector finalPos = pawn.AbsOrigin! + offset + new Vector(0, 0, pawn.ViewOffset.Z);

            CPointWorldText? newEntity = CreateEntity(finalPos, angles, text, size, color ?? Color.White, font, viewmodel);

            if (newEntity != null)
            {
                WorldTextEntities[newEntity.Index] = newEntity;
                WorldTextOwners[newEntity.Index] = player!.UserId!.Value;       
            }
            
            return newEntity;
        }

        private static CPointWorldText? CreateEntity(
            Vector position,
            QAngle angles,
            string text,
            float size,
            Color color,
            string font,
            CCSGOViewModel handle)
        {
            CPointWorldText? entity = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
            if (entity == null)
                return null;

            entity.MessageText = text;
            entity.Enabled = true;
            entity.FontSize = size;
            entity.Fullbright = true;
            entity.Color = color;
            entity.WorldUnitsPerPx = (0.25f / 1050) * size;
            entity.FontName = font;
            entity.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
            entity.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
            entity.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;      

            entity.DispatchSpawn();
            entity.Teleport(position, angles, null);
            entity.AcceptInput("SetParent", handle, null, "!activator");

            EntityTransforms[entity.Index] = (position, angles);

            return entity;
        }
    }
}