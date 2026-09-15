using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Zenject;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class StartUI : MonoBehaviour
{
    public bool alwaysDisplayMouse;
    public GameObject pauseCanvas;

    private bool _inPause;
    private PlayableDirector[] _directors;
    [Inject] private readonly PlayerInputHandler _playerInputHandler;    
   
    private void Start()
    { 
        if (!alwaysDisplayMouse)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
        else
        { 
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        _directors = FindObjectsByType<PlayableDirector> (FindObjectsSortMode.None);
        
        _playerInputHandler.Pause +=  ShowPauseMenu;
    }

    private void OnDestroy()
    {
        _playerInputHandler.Pause -=  ShowPauseMenu;
    }

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ExitPause()
    {
        _inPause = true;
        SwitchWindows(ref pauseCanvas);
    }

    public void RestartLevel()
    {
        _inPause = true;
        SwitchWindows(ref pauseCanvas);
        //SceneController.RestartZone();
        var currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    private void ShowPauseMenu()
    {
        SwitchWindows(ref pauseCanvas);
    }

    private void SwitchWindows(ref GameObject window)
    {
        if (_inPause && Time.timeScale > 0)
        {
            return;
        }

        if (!alwaysDisplayMouse)
        {
            UnityEngine.Cursor.lockState = _inPause ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !_inPause;
        }

        foreach (var dir in _directors)
        {
            if (dir.state == PlayState.Playing && !_inPause)
            {
                dir.Pause ();
            }
            else if(dir.state == PlayState.Paused && _inPause)
            {
                dir.Resume ();
            }
        }

        Time.timeScale = _inPause ? 1 : 0;

        if (window)
        {
            window.SetActive(!_inPause);
        }

        _inPause = !_inPause;
    }
}