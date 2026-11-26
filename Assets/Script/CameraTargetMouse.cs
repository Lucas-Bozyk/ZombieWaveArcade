using UnityEngine;

public class CameraTargetMouse : MonoBehaviour
{
    [Header("Controle da câmera")]
    public float sensibilidade = 2f;
    public float limiteInferior = -30f;
    public float limiteSuperior = 70f;

    private float rotacaoHorizontal;
    private float rotacaoVertical;

    private void Start()
    {
        rotacaoHorizontal = transform.eulerAngles.y;
        rotacaoVertical = 10f;

        TravarMouse();
    }

    private void Update()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensibilidade;
            float mouseY = Input.GetAxis("Mouse Y") * sensibilidade;

            rotacaoHorizontal += mouseX;
            rotacaoVertical -= mouseY;

            rotacaoVertical = Mathf.Clamp(
                rotacaoVertical,
                limiteInferior,
                limiteSuperior
            );

            transform.rotation = Quaternion.Euler(
                rotacaoVertical,
                rotacaoHorizontal,
                0f
            );
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TravarMouse();
        }
    }

    private void TravarMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}