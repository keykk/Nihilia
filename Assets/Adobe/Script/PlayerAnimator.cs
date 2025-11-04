using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
        // Pega o componente Animator do personagem
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Movimentação simples (exemplo com teclas WASD)
        bool frente = Input.GetKey(KeyCode.W);
        bool tras = Input.GetKey(KeyCode.S);
        bool pulando = Input.GetKeyDown(KeyCode.Space);

        // Define os bools no Animator conforme as teclas
        anim.SetBool("isRunningForward", frente);
        anim.SetBool("isRunningBackward", tras);

        if (pulando)
        {
            anim.SetTrigger("Jump");  // Jump pode ser Trigger ao invés de bool
        }

        // Se nenhuma tecla for pressionada → Idle
        if (!frente && !tras && !pulando)
        {
            anim.SetBool("isRunningForward", false);
            anim.SetBool("isRunningBackward", false);
        }
    }
}
