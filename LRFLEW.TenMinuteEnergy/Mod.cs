using GDWeave;

namespace LRFLEW.TenMinuteEnergy;

public class Mod : IMod {

    public Mod(IModInterface modInterface) {
        modInterface.Logger.Information("10 Minute Energy Loaded");
        modInterface.RegisterScriptMod(new PlayerPatcher());
    }

    public void Dispose() { }
}
