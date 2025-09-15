using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private ColliderBase collider;

    public float moveSpeed = 10f;
    private Vector3 targetPos;
    private bool isMoving = false;

    private void Update()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");
        Vector3 velocity = 10f * Time.deltaTime * new Vector3(inputX, 0, inputZ);
        // collider.SetSpeed(velocity);

        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Input.mousePosition;
            mousePos.z = Mathf.Abs(Camera.main.transform.position.y); 
            
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
            mouseWorldPos.y = 0; // Xóa trục Z vì 2D
            targetPos = mouseWorldPos;
            isMoving = true;
        }

        if (isMoving)
        {
            var direction = (targetPos - transform.position).normalized;
            velocity = moveSpeed * Time.deltaTime * direction;
            collider.SetSpeed(velocity);
            // Khi đã đến nơi
            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                isMoving = false;
                collider.SetSpeed(Vector3.zero);

            }
        }
    }
}