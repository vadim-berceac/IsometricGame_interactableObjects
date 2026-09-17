using UnityEngine;
using Zenject;

public abstract class BaseCharacter : MonoBehaviour
{
    [field: SerializeField] public CharacterType CharacterType { get; private set; } = CharacterType.Neutral;
    
    [Inject] private readonly SceneCharacterService _sceneCharacterService;
    [Inject] private readonly Transform _transform;
    
    public Transform Transform => _transform;

    protected virtual void OnEnable()
    {
        _sceneCharacterService.AddCharacter(this);
    }

    protected virtual void OnDisable()
    {
        _sceneCharacterService.RemoveCharacter(this);
    }
}
