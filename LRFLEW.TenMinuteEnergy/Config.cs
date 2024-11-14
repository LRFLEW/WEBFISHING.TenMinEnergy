using System.Text.Json.Serialization;

namespace LRFLEW.TenMinuteEnergy;

public class Config {
    [JsonInclude] public bool SomeSetting = true;
}
