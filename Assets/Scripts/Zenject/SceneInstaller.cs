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
            .Bind<AttackRangeSettings>()
            .FromScriptableObjectResource("Settings/AttackRangeSettings")
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<VisionSettings>()
            .FromScriptableObjectResource("Settings/VisionSettings")
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<CombatGroupSettings>()
            .FromScriptableObjectResource("Settings/CombatGroupSettings")
            .AsSingle()
            .NonLazy();
        
        Container
            .Bind<CombatPositioningSettings>()
            .FromScriptableObjectResource("Settings/CombatPositioningSettings")
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
        
        Container
            .BindInterfacesAndSelfTo<VisionSystem>()
            .AsSingle()
            .NonLazy();
        
        Container
            .BindInterfacesAndSelfTo<AttackRangeService>()
            .AsSingle()
            .NonLazy();
        
        Container
            .BindInterfacesAndSelfTo<CombatGroupRegistry>()
            .AsSingle()
            .NonLazy();
        
        
        
        Container
            .BindFactory<BaseCharacter, CombatGroup, CombatGroup.Factory>();
    }
}