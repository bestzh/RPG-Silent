using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace SkillSystem.Core
{
    public class SSSkill : MonoBehaviour
    {
        public enum SkillType { Single, InOrder, Chain, Random }
        
        //Settings
        public SkillType type;
        
        public string skillName = "New Skill";
        public bool autoPlayOnStart;
        public bool playOnRealtime;
        [Min(0.01f)] public float skillSpeed = 1;
        [Min(0)] public float skillCooldown;
        [Min(1)] public int maxCharge = 1;
        public bool saveToDictionary;
        public string dictionaryKeyName;


        public int currentCharge;
        public List<SSAction> actions = new List<SSAction>();
        
        private Coroutine _currentActionRoutine;
        private Coroutine _actionPlayingRoutine;
        private Coroutine _resetActionIndexRoutine;
        
        private int _actionIndex;
        private bool _isActionPlaying;
        private bool _isCharging;

        private float _currentSkillSpeed;

        private void Awake()
        {
            if (saveToDictionary) SSDictionary.ForceAdd(dictionaryKeyName, this);
        }

        private void OnEnable()
        {
            currentCharge = maxCharge;
            _currentSkillSpeed = skillSpeed;
            if (autoPlayOnStart) PlayNextAction();
        }
        
        public void PlayNextAction(float speed = 0)
        {
            if (_isActionPlaying || currentCharge <= 0 || actions.Count == 0) return;
            _isActionPlaying = true;
            if (speed > 0) _currentSkillSpeed = speed;
            
            switch (type)
            {
                case SkillType.Single:
                    HandleSingleTypeSkill();
                    break;
                case SkillType.Random:
                    HandleRandomTypeSkill();
                    break;
                case SkillType.InOrder:
                    HandleInOrderTypeSkill();
                    break;
                case SkillType.Chain:
                    HandleChainTypeSkill();
                    break;
            }
        }
        public void StopAction()
        {
            if (_currentActionRoutine != null) StopCoroutine(_currentActionRoutine);
            _isActionPlaying = false;
        }

        public bool IsActionPlaying()
        {
            return _isActionPlaying;
        }

        private void HandleSingleTypeSkill()
        {
            currentCharge--;
            
            PlayAction(0);
            StartCoroutine(ChargeRoutine());
            
            if (_actionPlayingRoutine != null) StopCoroutine(_actionPlayingRoutine);
            _actionPlayingRoutine = StartCoroutine(ActionPlayingRoutine(actions[_actionIndex].TotalActionDuration));
        }
        
        private void HandleRandomTypeSkill()
        {
            currentCharge--;
            
            _actionIndex = Random.Range(0, actions.Count);
            PlayAction(_actionIndex);
            StartCoroutine(ChargeRoutine());
            
            if (_actionPlayingRoutine != null) StopCoroutine(_actionPlayingRoutine);
            _actionPlayingRoutine = StartCoroutine(ActionPlayingRoutine(actions[_actionIndex].TotalActionDuration));
        }
        
        private void HandleInOrderTypeSkill()
        {
            currentCharge--;
            
            PlayAction(_actionIndex);
            StartCoroutine(ChargeRoutine());
            
            if (_actionPlayingRoutine != null) StopCoroutine(_actionPlayingRoutine);
            _actionPlayingRoutine = StartCoroutine(ActionPlayingRoutine(actions[_actionIndex].TotalActionDuration));
            
            if (actions.Count - 1 > _actionIndex) _actionIndex++;
            else _actionIndex = 0;
        }
        
        private void HandleChainTypeSkill()
        {
            PlayAction(_actionIndex);

            if (_actionPlayingRoutine != null) StopCoroutine(_actionPlayingRoutine);
            _actionPlayingRoutine = StartCoroutine(ActionPlayingRoutine(actions[_actionIndex].TotalActionDuration));
            
            if (_resetActionIndexRoutine != null) StopCoroutine(_resetActionIndexRoutine);
            if (actions.Count - 1 > _actionIndex)
            {
                _resetActionIndexRoutine = StartCoroutine(ResetActionIndexRoutine(actions[_actionIndex].resetActionIndexDelay));
                _actionIndex++;
            }
            else
            {
                _actionIndex = 0;
                currentCharge--; 
                StartCoroutine(ChargeRoutine());
            }
        }

        private void PlayAction(int actionIndex)
        {
            if (_currentActionRoutine != null) StopCoroutine(_currentActionRoutine);
            _currentActionRoutine = StartCoroutine(actions[actionIndex].PlayAllFunctionsRoutine(playOnRealtime, _currentSkillSpeed));
        }

        private IEnumerator ActionPlayingRoutine(float time)
        {
            if (!playOnRealtime) yield return new WaitForSeconds(time / _currentSkillSpeed);
            else yield return new WaitForSecondsRealtime(time / _currentSkillSpeed);
            _isActionPlaying = false;
        }
        
        private IEnumerator ResetActionIndexRoutine(float time)
        {
            if (!playOnRealtime) yield return new WaitForSeconds(time / _currentSkillSpeed);
            else yield return new WaitForSecondsRealtime(time / _currentSkillSpeed);
            _actionIndex = 0;
            currentCharge--;
            StartCoroutine(ChargeRoutine());
        }

        private IEnumerator ChargeRoutine()
        {
            if (_isCharging) yield break;
            _isCharging = true;
            yield return new WaitForSeconds(skillCooldown);
            currentCharge++;
            _isCharging = false;
            if (currentCharge < maxCharge) StartCoroutine(ChargeRoutine());
        }

    }
}
