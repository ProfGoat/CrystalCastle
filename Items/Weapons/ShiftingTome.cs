using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CrystalCastle.Items.Weapons
{
    public class ShiftingTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(4, 12));
            ItemID.Sets.AnimatesAsSoul[Item.type] = true;
        }

        private const int ShimmerManaCost = 25;
        private const int ShimmerDurationTicks = 90;
        private const int OptionalCooldownTicks = 0;

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 32;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.channel = true;

            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.damage = 0;
            Item.knockBack = 0f;
            Item.mana = 0;

            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 5);

            Item.shoot = ModContent.ProjectileType<Projectiles.PhaseWisp>();
            Item.shootSpeed = 0f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.channel = false;
                Item.useStyle = ItemUseStyleID.HoldUp;

                if (OptionalCooldownTicks > 0 &&
                    player.HasBuff(ModContent.BuffType<Buffs.RestabilizationBuff>()))
                    return false;

                if (player.HasBuff(BuffID.Shimmer))
                    return false;

                return player.statMana >= ShimmerManaCost;
            }

            Item.channel = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                if (player.CheckMana(ShimmerManaCost, pay: true))
                {
                    player.AddBuff(BuffID.Shimmer, ShimmerDurationTicks);

                    if (OptionalCooldownTicks > 0)
                        player.AddBuff(ModContent.BuffType<Buffs.RestabilizationBuff>(), OptionalCooldownTicks);

                    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.15f, Volume = 0.8f }, player.Center);
                    for (int i = 0; i < 10; i++)
                    {
                        int d = Dust.NewDust(player.position, player.width, player.height, DustID.WhiteTorch, 0, 0, 180, default, 1.0f);
                        Main.dust[d].noGravity = true;
                    }
                }
                return true;
            }

            return null;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
                                   Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
                return false;

            var mp = player.GetModPlayer<Common.Players.ShiftingPlayer>();
            if (mp.PhaseProjId < 0)
            {
                mp.PhaseProjId = Projectile.NewProjectile(
                    source, player.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.PhaseWisp>(), 0, 0f, player.whoAmI);
            }
            return false;
        }
    }
}
