using Zenject;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container
            .BindInterfacesAndSelfTo<SceneCharacterService>()
            .AsSingle()
            .NonLazy();
    }
}