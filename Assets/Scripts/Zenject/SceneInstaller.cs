using UnityEngine.InputSystem;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container
            .Bind<InputActionAsset>()
            .FromScriptableObjectResource("Input/InputSystem_Actions")
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<GravityCollisionSettings>()
            .FromScriptableObjectResource("Settings/GravityCollisionSettings")
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<Cursor>()
            .FromComponentInNewPrefabResource("Input/Cursor")
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<CameraSystem>()
            .FromComponentInNewPrefabResource("Camera/CameraSystem")
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<DialogueCanvasController>()
            .FromComponentInNewPrefabResource("UI/DialogueCanvas")
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<ElevatorPaneController>()
            .FromComponentInNewPrefabResource("UI/ElevatorPaneCanvas")
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<SceneUI>()
            .FromComponentInNewPrefabResource("UI/SceneUI")
            .AsSingle()
            .NonLazy();
        
        Container
            .BindInterfacesAndSelfTo<PlayerInputHandler>()
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<AnimationStates>()
            .AsSingle()
            .NonLazy();
    }
}