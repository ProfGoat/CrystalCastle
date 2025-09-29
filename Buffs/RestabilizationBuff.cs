using Terraria;
using Terraria.ModLoader;

namespace CrystalCastle.Buffs
{
    public class RestabilizationBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = false;
        }
    }
}
