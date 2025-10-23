using UnityEngine;

public class SwipeInput : MonoBehaviour
{
    public static SwipeInput Instance { get; private set; }
    public bool SwipedLeft { get; private set; }
    public bool SwipedRight { get; private set; }
    public bool SwipedUp { get; private set; }
    public bool SwipedDown { get; private set; }

    [SerializeField] float deadZone = 80f;

    Vector2 startPos;
    bool isDragging;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        SwipedLeft = SwipedRight = SwipedUp = SwipedDown = false;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) { isDragging = true; startPos = Input.mousePosition; }
        else if (Input.GetMouseButtonUp(0)) { isDragging = false; }
        if (isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - startPos;
            CheckSwipe(delta);
        }
#else
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { isDragging = true; startPos = t.position; }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { isDragging = false; }
            if (isDragging) CheckSwipe(t.position - startPos);
        }
#endif
    }

    void CheckSwipe(Vector2 delta)
    {
        if (delta.magnitude < deadZone) return;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            if (delta.x > 0) SwipedRight = true; else SwipedLeft = true;
        else
            if (delta.y > 0) SwipedUp = true; else SwipedDown = true;

        isDragging = false; // หนึ่งครั้งต่อหนึ่ง swipe
    }
}
