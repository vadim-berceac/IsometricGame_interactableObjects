using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class SceneCharacterService : IInitializable, IDisposable
{
    private readonly HashSet<BaseCharacter> _characters = new ();
    
    public void Initialize()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    public void Dispose()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        _characters.RemoveWhere(c => c == null);
    }

    public void AddCharacter(BaseCharacter character)
    {
        _characters.Add(character);
    }

    public void RemoveCharacter(BaseCharacter character)
    {
        _characters.Remove(character);
    }

    public bool HasCharacter(BaseCharacter character)
    {
        return _characters.Contains(character);
    }
    
    public IReadOnlyCollection<BaseCharacter> GetAllCharacters()
    {
        return _characters;
    }
}
