using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkillSystem.Core
{
    [Serializable]
    public class SSAction
    {
        public string actionName = "New Action";
        
        public float actionDuration;
        public float TotalActionDuration
        {
            get
            {
                float dur = 0;
                foreach (SSFunction func in functions)
                {
                    dur += func.delay;
                }
                return dur + actionDuration;
            }
        }

        [SerializeReference] public List<SSFunction> functions = new List<SSFunction>();
        private Coroutine _functionRoutine;
        // Chain
        public float resetActionIndexDelay;

        
        public IEnumerator PlayAllFunctionsRoutine(bool isRealtime,float speed = 1)
        {
            foreach (var function in functions)
            {
                if (!isRealtime) yield return new WaitForSeconds(function.delay / speed);
                else yield return new WaitForSecondsRealtime(function.delay / speed);
                function.Play();
            }
        }

    }
}