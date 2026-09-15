using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class PlayerInputHandler : ICharacterInput, IInitializable, IDisposable
{
    public Action<Vector2> OnMove { get; set; }
    public Action<Vector2> OnLook { get; set;}
    public Action<bool> OnRun  { get; set;}
    public Action OnInteract { get; set;}
    public Action<float> OnScrollWheel  { get; set;}
    public Action Pause  { get; set;}

    [Inject] private readonly InputActionAsset _inputActionAsset;
    
    private InputAction MoveAction => _inputActionAsset.FindAction("Move");
    private InputAction LookAction => _inputActionAsset.FindAction("Look");
    private InputAction RunAction => _inputActionAsset.FindAction("Run");
    private InputAction OnInteractAction => _inputActionAsset.FindAction("Interact");
    private InputAction ScrollAction => _inputActionAsset.FindActionMap("UI").FindAction("ScrollWheel");
    private InputAction PauseAction => _inputActionAsset.FindAction("Pause");
    
    public void Initialize()
    {
       MoveAction.performed += Move;
       MoveAction.canceled += MoveCancel;
       
       LookAction.performed += Look;
       LookAction.canceled += LookCancel;
       
       RunAction.performed += Run;
       RunAction.canceled += RunCancel;
       
       OnInteractAction.performed += InteractPressed;
       
       ScrollAction.performed += ScrollWheel;
       PauseAction.performed +=  OnPause;
    }

    public void Dispose()
    {
        if (MoveAction != null)
        {
            MoveAction.performed -= Move;
            MoveAction.canceled -= MoveCancel;
        }

        if (LookAction != null)
        {
            LookAction.performed -= Look;
            LookAction.canceled -= LookCancel;
        }

        if (RunAction != null)
        {
            RunAction.performed -= Run;
            RunAction.canceled -= RunCancel;
        }

        if (OnInteractAction != null)
        {
            OnInteractAction.performed -= InteractPressed;
        }

        if (ScrollAction != null)
        {
            ScrollAction.performed -= ScrollWheel;
        }

        if (PauseAction != null)
        {
            PauseAction.performed -=  OnPause;
        }
    }

    private void Move(InputAction.CallbackContext context)
    {
        OnMove?.Invoke(context.ReadValue<Vector2>());
    }
    
    private void MoveCancel(InputAction.CallbackContext context)
    {
        OnMove?.Invoke(Vector2.zero);
    }

    private void Look(InputAction.CallbackContext context)
    {
        OnLook?.Invoke(context.ReadValue<Vector2>());
    }

    private void LookCancel(InputAction.CallbackContext context)
    {
        OnLook?.Invoke(Vector2.zero);
    }

    private void Run(InputAction.CallbackContext context)
    {
        OnRun?.Invoke(context.ReadValueAsButton());
    }

    private void RunCancel(InputAction.CallbackContext context)
    {
        OnRun?.Invoke(false);
    }

    private void InteractPressed(InputAction.CallbackContext context)
    {
       OnInteract?.Invoke();
    }

    private void ScrollWheel(InputAction.CallbackContext context)
    {
        var scrollValue = context.ReadValue<Vector2>();
        OnScrollWheel?.Invoke(scrollValue.y);
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        Pause?.Invoke();
    }
}
