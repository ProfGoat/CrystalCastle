using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CrystalCastle.Projectiles
{
    public class PhaseWisp : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        const float Speed = 12f;
        const int ManaPerTick = 2;
        const int CooldownTicks = 90;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            {
                var mp = Main.player[Projectile.owner].GetModPlayer<Common.Players.ShiftingPlayer>();
                mp.PhaseProjId = Projectile.whoAmI;
            }
        }

        public override void Kill(int timeLeft)
        {
            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            {
                Player p = Main.player[Projectile.owner];
                if (p.active && !p.dead)
                    p.AddBuff(ModContent.BuffType<Buffs.RestabilizationBuff>(), CooldownTicks);

                var mp = p.GetModPlayer<Common.Players.ShiftingPlayer>();
                if (mp.PhaseProjId == Projectile.whoAmI)
                    mp.PhaseProjId = -1;
            }
        }

        private int frameCounter = 0;

        public override void AI()
        {
            Player p = Main.player[Projectile.owner];
            if (!p.active || p.dead) { Projectile.Kill(); return; }

            bool stillChanneling =
                p.channel &&
                !p.noItems &&
                !p.CCed &&
                p.HeldItem?.type == ModContent.ItemType<Items.Weapons.ShiftingTome>();

            if (!stillChanneling) { Projectile.Kill(); return; }

            if (!p.CheckMana(ManaPerTick, pay: true)) { Projectile.Kill(); return; }

            Microsoft.Xna.Framework.Vector2 dir = Microsoft.Xna.Framework.Vector2.Zero;
            if (p.controlLeft) dir.X -= 1;
            if (p.controlRight) dir.X += 1;
            if (p.controlUp) dir.Y -= 1;
            if (p.controlDown) dir.Y += 1;
            if (dir != Microsoft.Xna.Framework.Vector2.Zero) dir.Normalize();

            Projectile.velocity = dir * Speed;
            Projectile.timeLeft = 2;

            if (Main.rand.NextBool(4))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemDiamond, 0, 0, 150, default, 1.0f);
                Main.dust[d].noGravity = true;
            }

            frameCounter++;
            if (frameCounter >= 4)
            {
                frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 5)
                    Projectile.frame = 0;
            }
        }
    }
}
