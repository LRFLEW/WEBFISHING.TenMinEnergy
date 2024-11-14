using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;

namespace LRFLEW.TenMinuteEnergy;

internal class PlayerPatcher : IScriptMod
{
    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/player.gdc";

    private MultiTokenWaiter MatchWaiter(string caseString)
    {
        return new MultiTokenWaiter([
            t => t is ConstantToken { Value: StringVariant } &&
                ((StringVariant) ((ConstantToken) t).Value).Value == caseString,
            t => t.Type is TokenType.Colon,
            t => t.Type is TokenType.Newline,
        ]);
    }

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        var func_waiter = new MultiTokenWaiter([
            t => t.Type is TokenType.PrFunction,
            t => t is IdentifierToken { Name: "_consume_item" },
        ]);

        var speed_waiters = new MultiTokenWaiter[]
        {
            MatchWaiter("speed"),
            MatchWaiter("speed_burst"),
        };
        var speed_atms = new double[] { 1.3, 4.0 };
        var speed_timers = new int[] { 18000, 900 };

        var catch_waiters = new MultiTokenWaiter[]
        {
            MatchWaiter("catch"),
            MatchWaiter("catch_big"),
            MatchWaiter("catch_deluxe"),
        };

        var bounce_waiters = new MultiTokenWaiter[]
        {
            MatchWaiter("bounce"),
            MatchWaiter("bounce_big"),
        };
        var bounce_timers = new int[] { 1200, 600 };

        int newline_skip = 0;

        foreach (var token in tokens)
        {
            if (newline_skip > 0 && token.Type is TokenType.Newline) newline_skip--;
            if (newline_skip <= 0) yield return token;

            if (func_waiter.Matched)
            {
                for (int i = 0; i < speed_waiters.Length; ++i)
                {
                    if (speed_waiters[i].Check(token))
                    {
                        // if boost_timer > 0 && boost_amt == <atm> :
                        yield return new Token(TokenType.CfIf);
                        yield return new IdentifierToken("boost_timer");
                        yield return new Token(TokenType.OpGreater);
                        yield return new ConstantToken(new IntVariant(0));
                        yield return new Token(TokenType.OpAnd);
                        yield return new IdentifierToken("boost_amt");
                        yield return new Token(TokenType.OpEqual);
                        yield return new ConstantToken(new RealVariant(speed_atms[i]));
                        yield return new Token(TokenType.Colon);

                        // boost_timer += <timer>
                        yield return new IdentifierToken("boost_timer");
                        yield return new Token(TokenType.OpAssignAdd);
                        yield return new ConstantToken(new IntVariant(speed_timers[i]));
                        yield return new Token(TokenType.Newline, 3);

                        // else : [boost_timer = <timer>]
                        yield return new Token(TokenType.CfElse);
                        yield return new Token(TokenType.Colon);
                    }
                }

                for (int i = 0; i < catch_waiters.Length; ++i)
                {
                    if (catch_waiters[i].Check(token))
                    {
                        // if catch_drink_timer > 0 && catch_drink_tier == <tier> :
                        yield return new Token(TokenType.CfIf);
                        yield return new IdentifierToken("catch_drink_timer");
                        yield return new Token(TokenType.OpGreater);
                        yield return new ConstantToken(new IntVariant(0));
                        yield return new Token(TokenType.OpAnd);
                        yield return new IdentifierToken("catch_drink_tier");
                        yield return new Token(TokenType.OpEqual);
                        yield return new ConstantToken(new IntVariant(i + 1));
                        yield return new Token(TokenType.Colon);

                        // catch_drink_timer += 18000
                        yield return new IdentifierToken("catch_drink_timer");
                        yield return new Token(TokenType.OpAssignAdd);
                        yield return new ConstantToken(new IntVariant(18000));
                        yield return new Token(TokenType.Newline, 3);

                        // else : [catch_drink_timer = 18000]
                        yield return new Token(TokenType.CfElse);
                        yield return new Token(TokenType.Colon);
                    }
                }

                for (int i = 0; i < bounce_waiters.Length; ++i)
                {
                    if (bounce_waiters[i].Check(token))
                    {
                        // if jump_bonus_timer > 0 && jump_bonus_tier == <tier> : ...
                        yield return new Token(TokenType.CfIf);
                        yield return new IdentifierToken("jump_bonus_timer");
                        yield return new Token(TokenType.OpGreater);
                        yield return new ConstantToken(new IntVariant(0));
                        yield return new Token(TokenType.OpAnd);
                        yield return new IdentifierToken("jump_bonus_tier");
                        yield return new Token(TokenType.OpEqual);
                        yield return new ConstantToken(new IntVariant(i + 1));
                        yield return new Token(TokenType.Colon);

                        // jump_bonus_timer += <timer>
                        yield return new IdentifierToken("jump_bonus_timer");
                        yield return new Token(TokenType.OpAssignAdd);
                        yield return new ConstantToken(new IntVariant(bounce_timers[i]));
                        yield return new Token(TokenType.Newline, 3);

                        // else : jump_bonus_timer = <timer>
                        yield return new Token(TokenType.CfElse);
                        yield return new Token(TokenType.Colon);
                        yield return new IdentifierToken("jump_bonus_timer");
                        yield return new Token(TokenType.OpAssign);
                        yield return new ConstantToken(new IntVariant(bounce_timers[i]));
                        yield return new Token(TokenType.Newline, 3);

                        // jump_bonus_tier = <tier>
                        yield return new IdentifierToken("jump_bonus_tier");
                        yield return new Token(TokenType.OpAssign);
                        yield return new ConstantToken(new IntVariant(i + 1));

                        // Skip two lines, as they're replaced by above
                        newline_skip = 2;
                    }
                }
            }
            else func_waiter.Check(token);
        }
    }
}
