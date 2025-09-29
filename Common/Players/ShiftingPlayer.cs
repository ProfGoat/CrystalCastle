using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace CrystalCastle.Common.Players
{
    public class ShiftingPlayer : ModPlayer
    {
        public int PhaseProjId = -1;

        private const float GhostFade = 0.55f;
        private const float ShimmerDamageMultiplier = 0.20f;
        private const bool LockControlsWhileGhost = true;

        public bool IsPhasing
        {
            get
            {
                if (PhaseProjId < 0 || PhaseProjId >= Main.maxProjectiles) return false;
                Projectile pr = Main.projectile[PhaseProjId];
                return pr.active && pr.type == ModContent.ProjectileType<Projectiles.PhaseWisp>();
            }
        }

        private int FindOwnWisp()
        {
            int t = ModContent.ProjectileType<Projectiles.PhaseWisp>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile pr = Main.projectile[i];
                if (pr.active && pr.type == t && pr.owner == Player.whoAmI)
                    return i;
            }
            return -1;
        }

        public override void PostUpdate()
        {
            if (PhaseProjId < 0 || PhaseProjId >= Main.maxProjectiles ||
                !Main.projectile[PhaseProjId].active ||
                Main.projectile[PhaseProjId].type != ModContent.ProjectileType<Projectiles.PhaseWisp>())
            {
                int found = FindOwnWisp();
                if (found != -1) PhaseProjId = found;
            }

            if (!IsPhasing) return;

            Projectile wisp = Main.projectile[PhaseProjId];
            Player.Center = wisp.Center;
            Player.velocity = Vector2.Zero;

            Player.noFallDmg = true;
            Player.fallStart = (int)(Player.position.Y / 16f);

            if (LockControlsWhileGhost)
            {
                Player.controlUseItem = false;
                Player.controlUseTile = false;
                Player.controlHook = false;

                if (Player.mount.Active) Player.mount.Dismount(Player);
                Player.RemoveAllGrapplingHooks();
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (IsPhasing)
            {
                modifiers.FinalDamage *= 0f;
                modifiers.Knockback *= 0f;
                modifiers.HitDirectionOverride = 0;
                modifiers.DisableSound();
                modifiers.DisableDust();
                return;
            }

            if (Player.HasBuff(BuffID.Shimmer))
            {
                modifiers.FinalDamage *= ShimmerDamageMultiplier;
                modifiers.Knockback *= 0f;
            }
        }

        public override void ResetEffects()
        {
            if (!IsPhasing) PhaseProjId = -1;
        }

        public override void UpdateDead()
        {
            PhaseProjId = -1;
        }

        public override void FrameEffects()
        {
            if (IsPhasing)
            {
                Player.armorEffectDrawShadow = true;
                Player.armorEffectDrawShadowSubtle = true;
                Player.armorEffectDrawOutlines = true;
            }
        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (!IsPhasing) return;

            float f = GhostFade;
            drawInfo.colorBodySkin *= f;
            drawInfo.colorLegs *= f;
            drawInfo.colorEyeWhites *= f;
            drawInfo.colorEyes *= f;
            drawInfo.colorHair *= f;

            drawInfo.colorArmorHead *= f;
            drawInfo.colorArmorBody *= f;
            drawInfo.colorArmorLegs *= f;

            Player.yoraiz0rEye = 2;
        }
    }
}
