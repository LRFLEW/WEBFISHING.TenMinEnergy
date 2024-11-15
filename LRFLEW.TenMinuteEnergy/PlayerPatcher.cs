using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using Serilog;

namespace LRFLEW.TenMinuteEnergy;

internal class PlayerPatcher(ILogger logger) : IScriptMod
{
    private readonly ILogger Logger = logger;

    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/player.gdc";

    private static MultiTokenWaiter MatchWaiter(string caseString)
    {
        return new MultiTokenWaiter([
            t => t is ConstantToken { Value: StringVariant sv } && sv.Value == caseString,
            t => t is Token { Type: TokenType.Colon },
            t => t is Token { Type: TokenType.Newline, AssociatedData: 3 },
        ]);
    }

    private static IEnumerable<Token> WriteTimeInst(string timer, int time, string tier, Variant value)
    {
        // if {timer} > 0 && {tier} == {value} : ...
        yield return new Token(TokenType.CfIf);
        yield return new IdentifierToken(timer);
        yield return new Token(TokenType.OpGreater);
        yield return new ConstantToken(new IntVariant(0));
        yield return new Token(TokenType.OpAnd);
        yield return new IdentifierToken(tier);
        yield return new Token(TokenType.OpEqual);
        yield return new ConstantToken(value);
        yield return new Token(TokenType.Colon);

        // {timer} += {time}
        yield return new IdentifierToken(timer);
        yield return new Token(TokenType.OpAssignAdd);
        yield return new ConstantToken(new IntVariant(time));
        yield return new Token(TokenType.Newline, 3);

        // else : {timer} = {time}
        yield return new Token(TokenType.CfElse);
        yield return new Token(TokenType.Colon);
        yield return new IdentifierToken(timer);
        yield return new Token(TokenType.OpAssign);
        yield return new ConstantToken(new IntVariant(time));
        yield return new Token(TokenType.Newline, 3);
    }

    private static Func<Token, bool>[] AssignPattern(string var, int value)
    {
        return [
            t => t is IdentifierToken it && it.Name == var,
            t => t is Token { Type: TokenType.OpAssign },
            t => t is ConstantToken { Value: IntVariant iv } && iv.Value == value,
            t => t is Token { Type: TokenType.Newline, AssociatedData: 3 },
        ];
    }

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter func_waiter = new([
            t => t.Type is TokenType.PrFunction,
            t => t is IdentifierToken { Name: "_consume_item" },
        ]);

        MultiTokenWaiter[] speed_waiters =
            [ MatchWaiter("speed"), MatchWaiter("speed_burst") ];
        double[] speed_atms = [ 1.3, 4.0 ];
        int[] speed_timers = [ 18000, 900 ];

        MultiTokenWaiter[] catch_waiters =
            [ MatchWaiter("catch"), MatchWaiter("catch_big"), MatchWaiter("catch_deluxe") ];

        MultiTokenWaiter[] bounce_waiters =
            [ MatchWaiter("bounce"), MatchWaiter("bounce_big")];
        var bounce_timers = new int[] { 1200, 600 };

        MultiTokenWaiter? skip_waiter = null;

        foreach (var token in tokens)
        {
            if (skip_waiter != null)
            {
                if (skip_waiter.Check(token))
                {
                    // end of pattern to skip
                    skip_waiter = null;
                }
                else if (skip_waiter.Step == 0)
                {
                    // unexpected token found
                    Logger.Error("TenMinuteEnergy: " +
                        "Unexpected Token found when patching the player script. " +
                        "This may be caused by a mod conflict or an unsupported game update. " +
                        "Will continue to attempt the patch, but will likely cause errors.");
                    skip_waiter = null;
                    yield return token;
                }
            }
            else yield return token;

            if (func_waiter.Matched)
            {
                for (int i = 0; i < speed_waiters.Length; ++i)
                {
                    if (speed_waiters[i].Check(token))
                    {
                        var inst = WriteTimeInst("boost_timer", speed_timers[i],
                            "boost_amt", new RealVariant(speed_atms[i]));
                        foreach (var t in inst) yield return t;
                        skip_waiter = new(AssignPattern("boost_timer", speed_timers[i]));
                    }
                }

                for (int i = 0; i < catch_waiters.Length; ++i)
                {
                    if (catch_waiters[i].Check(token))
                    {
                        var inst = WriteTimeInst("catch_drink_timer", 18000,
                            "catch_drink_tier", new IntVariant(i + 1));
                        foreach (var t in inst) yield return t;
                        skip_waiter = new(AssignPattern("catch_drink_timer", 18000));
                    }
                }

                for (int i = 0; i < bounce_waiters.Length; ++i)
                {
                    if (bounce_waiters[i].Check(token))
                    {
                        var inst = WriteTimeInst("jump_bonus_timer", bounce_timers[i],
                            "jump_bonus_tier", new IntVariant(i + 1));
                        foreach (var t in inst) yield return t;

                        // jump_bonus_tier = <tier>
                        yield return new IdentifierToken("jump_bonus_tier");
                        yield return new Token(TokenType.OpAssign);
                        yield return new ConstantToken(new IntVariant(i + 1));
                        yield return new Token(TokenType.Newline, 3);

                        skip_waiter = new([
                            ..AssignPattern("jump_bonus_tier", i + 1),
                            ..AssignPattern("jump_bonus_timer", bounce_timers[i]),
                        ]);
                    }
                }
            }
            else func_waiter.Check(token);
        }

        foreach (var waiter in (MultiTokenWaiter[])
            [..speed_waiters, ..catch_waiters, ..bounce_waiters])
        {
            if (!waiter.Matched)
            {
                Logger.Warning("TenMinuteEnergy: " +
                    "Not all patch locations were found in the script file. " +
                    "This may be caused by a mod conflict or an unsupported game update. " +
                    "Not all mod functionality will work, but the game should otherwise work fine.");
                break;
            }
        }

        Logger.Information("TenMinuteEnergy: player.gdc Patched");
    }
}
