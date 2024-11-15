using GDWeave;

namespace LRFLEW.TenMinuteEnergy;

public sealed class Mod : IMod {

    public Mod(IModInterface modInterface) {
        modInterface.Logger.Information("TenMinuteEnergy: Mod Loaded");
        modInterface.RegisterScriptMod(new PlayerPatcher(modInterface.Logger));
    }

    public void Dispose() { }
}
