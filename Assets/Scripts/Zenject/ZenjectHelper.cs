using Zenject;

public static class ZenjectHelper
{
    public static ICharacterInput CreateCharacterInput(InjectContext ctx)
    {
        var characterType = ctx.Container.Resolve<CharacterType>();

        if (characterType == CharacterType.AI)
        {
            return ctx.Container.Instantiate<AIInputHandler>();
        }

        return ctx.Container.Resolve<PlayerInputHandler>();
    }
}
