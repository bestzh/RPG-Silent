using System.Collections.Generic;
using SkillSystem.Core;
using SkillSystem.Demos.Scripts;
using UnityEngine;

namespace SkillSystem.Demos.Demo_2.Scripts
{
    public class SSDemo2Player : MonoBehaviour
    {
        [SerializeField] private List<SSSkill> skillList;
        [SerializeField] private GameObject indicator;
        
        [SerializeField] private Transform skillHolder;
        [SerializeField] private Transform targetTransform;
        
        private SSSkill _currentSkill;
        private Rigidbody _rb;
        private Vector2 _movement;
        private Vector3 _direction;
        private Camera _camera;

        void Awake()
        {
            _camera = Camera.main;
            _rb = GetComponent<Rigidbody>();
            _rb.maxLinearVelocity = 50f;
        }
        
        private void Start()
        {
            SSDictionary.Add("Target Transform", targetTransform);
            SSDictionary.Add("Player Transform", transform);
            ChangeActiveSkill(0);
        }

        void Update()
        {
            GetInput();
            Rotate();
        }
        void FixedUpdate()
        {
            Move();
        }
        
        private void GetInput()
        {
            if (_currentSkill.IsActionPlaying()) return;
            
            _movement.x = Input.GetAxisRaw("Horizontal");
            _movement.y = Input.GetAxisRaw("Vertical");
            _movement.Normalize();
            
            Vector3 screenPos = Input.mousePosition;
            Ray ray = new Ray();
            if (!float.IsNaN(screenPos.x) && !float.IsNaN(screenPos.y) && !float.IsInfinity(screenPos.x) && !float.IsInfinity(screenPos.y))
            {
                ray = _camera.ScreenPointToRay(Input.mousePosition);
            }
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                _direction = new Vector3(
                    hit.point.x - transform.position.x, 0 , hit.point.z - transform.position.z).normalized; 
            }

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                PlaySkill();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ChangeActiveSkill(0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ChangeActiveSkill(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ChangeActiveSkill(2);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ChangeActiveSkill(3);
            }
        }
        
        private void Rotate()
        {
            if (_currentSkill.IsActionPlaying()) return;
            if (_direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(_direction);
        }
        
        private void Move()
        {
            if (_currentSkill.IsActionPlaying()) return;
            if (_movement.magnitude < 0.1f) return;
            _rb.linearVelocity = new Vector3(_movement.x, _rb.linearVelocity.y, _movement.y) * 10;
        }

        private void ChangeActiveSkill(int index)
        {
            if (index < 0 && index > skillList.Count) return;
            foreach (Transform child in skillHolder)
            {
                Destroy(child.gameObject);
            }
            
            _currentSkill = Instantiate(skillList[index], skillHolder.transform);
        }
        private void PlaySkill()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            PlaceTargetPosition();
            _currentSkill.PlayNextAction();
        }
        
        private void PlaceTargetPosition()
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var pos = hit.point;
                if (hit.collider.TryGetComponent<SSDemoHealth>(out var health)) pos = hit.collider.transform.position;
                pos = new Vector3(pos.x, 0, pos.z);
                
                Instantiate(indicator, pos, Quaternion.identity);
                targetTransform.position = pos;
            }
        }
    }
}