using UnityEngine;

public class InteractObjectSpawner : MonoBehaviour
{
   [SerializeField] private InteractAnimation interactAnimation;
   [SerializeField] private StaticPartSettings[] settings;
   
   private GameObject[] _spawned;

   private void Awake()
   {
      interactAnimation.onInteractEnter.AddListener(Spawn);
      interactAnimation.onInteractExit.AddListener(Despawn);
   }

   private void OnDestroy()
   {
      interactAnimation.onInteractEnter.RemoveListener(Spawn);
      interactAnimation.onInteractExit.RemoveListener(Despawn);
   }

   private void Spawn(Character controller)
   {
      _spawned = new GameObject[settings.Length];
      
      for (var i = 0; i < settings.Length; i++)
      {
         var obj = Instantiate(settings[i].Prefab);
         _spawned[i] = obj;
         settings[i].BoneSettings.SetPropBone(obj.transform, controller.PropBones);
      }
   }

   private void Despawn(Character controller)
   {
      foreach (var obj in _spawned)
      {
         Destroy(obj);
      }
      _spawned = null;
   }
}
