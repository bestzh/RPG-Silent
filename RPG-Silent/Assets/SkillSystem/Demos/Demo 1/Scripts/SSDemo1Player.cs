using System;
using SkillSystem.Core;
using UnityEngine;

namespace SkillSystem.Demos.Demo1.Scripts
{
    public class SSDemo1Player : MonoBehaviour
    {
        [SerializeField] private SSSkill skill1;
        [SerializeField] private SSSkill skill2;
        [SerializeField] private SSSkill skill3;
        [SerializeField] private SSSkill skill4;

        private SSSkill _currentSkill;
        
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private ParticleSystem _ps;

        private int _faceDirection = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponentInChildren<SpriteRenderer>();
            _ps = GetComponentInChildren<ParticleSystem>();
            _currentSkill = skill1;
        }

        private void Start()
        {
            SSDictionary.Add("handPos", new Vector2(_faceDirection, 0));
        }

        private void FixedUpdate()
        {
            Move();
            Boundry();
        }

        private void Update()
        {
            Attack();
        }

        private void Boundry()
        {
            if (_rb.position.x > 8) _rb.position = new Vector2(8, _rb.position.y);
            if (_rb.position.x < -8) _rb.position = new Vector2(-8, _rb.position.y);
        }

        private void Move()
        {
            if (_currentSkill.IsActionPlaying()) return;
            
            var input = Input.GetAxisRaw("Horizontal");
            if (input != 0)
            {
                _rb.linearVelocityX = input * 10;
                if (input > 0)
                {
                    _faceDirection = 1;
                    _sr.flipX = false;
                    _ps.transform.localScale = new Vector3(1, 1, 1);
                }
                else
                {
                    _faceDirection = -1;
                    _sr.flipX = true;
                    _ps.transform.localScale = new Vector3(-1, 1, 1);
                }

                SSDictionary.ForceAdd("handPos", new Vector2(_faceDirection, 0));
            }
            else _rb.linearVelocityX = 0;
        }

        private void Attack()
        {
            if (_currentSkill.IsActionPlaying()) return;
            
            
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                _rb.linearVelocity = Vector2.zero;
                _currentSkill = skill1;
                _currentSkill.PlayNextAction();
            }
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                _rb.linearVelocity = Vector2.zero;
                _currentSkill = skill2;
                SSDictionary.ForceAdd("mousePos", Camera.main.ScreenToWorldPoint(Input.mousePosition));
                _currentSkill.PlayNextAction();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _rb.linearVelocity = Vector2.zero;
                _currentSkill = skill3;
                _currentSkill.PlayNextAction();
            }
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                _rb.linearVelocity = Vector2.zero;
                _currentSkill = skill4;
                _currentSkill.PlayNextAction();
            }
        }
    }
}
