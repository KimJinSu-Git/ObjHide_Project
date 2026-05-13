using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerTestScript : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Components")]
    [SerializeField] private Animator animator;
    
    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        
        // 애니메이터를 수동으로 넣지 않았다면 자식 객체에서 찾습니다.
        if (animator == null) 
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
        HandleShoot();
    }

    /// <summary>
    /// WASD 방향키 이동 및 8방향 블렌드 트리 애니메이션 처리
    /// </summary>
    private void HandleMovement()
    {
        // 바닥에 닿아있는지 체크
        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // 땅에 안정적으로 붙어있도록 살짝 누릅니다.
        }

        // 1. 키보드 입력 받기 (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // 2. 물리적 이동 처리 (스크립트 통제)
        Vector3 move = transform.right * x + transform.forward * z;
        _controller.Move(move * (moveSpeed * Time.deltaTime));

        // 3. [애니메이션 연동] 2D 블렌드 트리에 입력값 직접 전달 ⭐수정된 부분⭐
        if (animator != null)
        {
            // 부드러운 전환을 위해 원본 입력값(-1 ~ 1)을 그대로 애니메이터에 넘깁니다.
            animator.SetFloat("Horizontal", x);
            animator.SetFloat("Vertical", z);
            
            animator.SetBool("isGrounded", _isGrounded);
        }
    }

    /// <summary>
    /// 스페이스바 점프 및 체공 애니메이션 처리
    /// </summary>
    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
            Debug.Log("[Test] 점프!");
        }

        // 매 프레임마다 중력을 적용하여 아래로 당깁니다.
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    /// <summary>
    /// 마우스 좌클릭 사격 애니메이션 처리
    /// </summary>
    private void HandleShoot()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (animator != null)
            {
                animator.SetTrigger("Shoot");
            }
            Debug.Log("[Test] 빵야! 사격 애니메이션 재생");
        }
    }
}