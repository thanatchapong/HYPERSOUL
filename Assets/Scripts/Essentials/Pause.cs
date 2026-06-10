using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{
    public static bool isPaused = false; 
    [SerializeField] GameObject pauseMenuUI; 
    [SerializeField] PlayableDirector openTimeline;
    [SerializeField] PlayableDirector closeTimeline;
    [SerializeField] float currentTime = 1f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                DoPause();
            }
        }
    }

    public void Resume()
    {   
        closeTimeline.Play();
        Time.timeScale = currentTime;          
        isPaused = false;
    }

    public void DoPause()
    {
        currentTime = Time.timeScale;

        pauseMenuUI.SetActive(true); 
        openTimeline.Play();
        Time.timeScale = 0f;          
        isPaused = true;
    }
}