using UnityEngine.EventSystems;
using UnityEngine;

public class UIFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float responsiveTime = 1f;
    [SerializeField] float sizeMultiplier = 1.2f;
    [SerializeField] float maxRotateAngle = 25f;
    [SerializeField] Transform target;

    private Vector3 initialScale;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    [SerializeField] bool hovering = false;

    [SerializeField] AudioSource audio;
    [SerializeField] AudioClip hoverClip;
    [SerializeField] AudioClip clickedClip;

    void Start()
    {
        if (target == null) target = transform;

        initialScale = target.localScale;
        initialRotation = target.localRotation;
        
        targetRotation = initialRotation;
    }

    void Update()
    {
        if(hovering)
        {
            target.localScale = Vector3.Lerp(target.localScale, initialScale * sizeMultiplier, Time.unscaledDeltaTime * responsiveTime);
            target.localRotation = Quaternion.Slerp(target.localRotation, targetRotation, Time.unscaledDeltaTime * responsiveTime);
        }
        else
        {
            target.localScale = Vector3.Lerp(target.localScale, initialScale, Time.unscaledDeltaTime * responsiveTime);
            target.localRotation = Quaternion.Slerp(target.localRotation, initialRotation, Time.unscaledDeltaTime * responsiveTime);
        }
    }

    public void Click()
    {
        if(!this.enabled) return;
        float randomAngle = Random.Range(-maxRotateAngle, maxRotateAngle);
        
        Quaternion offsetRotation = Quaternion.Euler(0, 0, randomAngle);
        targetRotation = initialRotation * offsetRotation;

        target.localScale = initialScale * (sizeMultiplier / 1.2f);
        target.localRotation = targetRotation;
        
        hovering = false;

        if(audio) PlayAudioClip(clickedClip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;

        float randomAngle = Random.Range(-maxRotateAngle, maxRotateAngle);
        
        Quaternion offsetRotation = Quaternion.Euler(0, 0, randomAngle);
        targetRotation = initialRotation * offsetRotation;
        
        if(audio) PlayAudioClip(hoverClip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    void PlayAudioClip(AudioClip clip)
    {
        audio.clip = clip;
        audio.pitch = Random.Range(0.9f, 1.1f);
        audio.Play();
    }
}
