using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bird.Network.Data;
using Bird.Network.Managers;
using Bird.Network.Player;
using TMPro;
using UnityEngine;

namespace Bird.Network.UI
{
    public class PropSelectionUIHandler : MonoBehaviour
    {
        [SerializeField] private GameObject panel; // 슬롯머신 전체 패널
        [SerializeField] private BirdPropSlotUI[] slots; // 3개의 슬롯
        [SerializeField] private PropDatabase propDatabase;
        [SerializeField] private TextMeshProUGUI timerText;

        private Coroutine timerCoroutine;
        
        public bool hasSelected = false;

        private void Awake()
        {
            panel.SetActive(false);
        }
        
        private IEnumerator Start()
        {
            while (BirdGameManager.Instance == null)
            {
                yield return null;
            }

            BirdGameManager.Instance.OnSelectionPhaseStarted += HandleSelectionStarted;
            BirdGameManager.Instance.OnSelectionPhaseEnded += CloseUI;
        }

        private void OnDisable()
        {
            if (BirdGameManager.Instance != null)
            {
                BirdGameManager.Instance.OnSelectionPhaseStarted -= HandleSelectionStarted;
                BirdGameManager.Instance.OnSelectionPhaseEnded -= CloseUI;
            }
        }
        
        private void HandleSelectionStarted(bool isSeeker)
        {
            if (isSeeker) return; 
            
            hasSelected = false;
            OpenSelectionUI();
        }

        private void OpenSelectionUI()
        {
            if (hasSelected) return;
            
            panel.SetActive(true);
            hasSelected = false;

            var randomProps = propDatabase.GetRandomUniqueProps(slots.Length);

            for (int i = 0; i < slots.Length; i++)
            {
                if (i < randomProps.Count)
                {
                    slots[i].SetupSlot(randomProps[i], ConfirmSelection, RequestReroll);
                    slots[i].SetRerollActive(true); // 처음 리롤 버튼 활성화
                }
            }
            
            if(timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(Co_StartTimer(20f));
        }

        private void RequestReroll(BirdPropSlotUI targetSlot)
        {
            List<int> currentDisplayedIDs = slots
                .Where(s => s.CurrentPropID != -1)
                .Select(s => s.CurrentPropID)
                .ToList();
            
            var newPropList = propDatabase.GetRandomUniqueProps(1, currentDisplayedIDs);

            if (newPropList.Count > 0)
            {
                targetSlot.SetupSlot(newPropList[0], ConfirmSelection, RequestReroll);
                targetSlot.SetRerollActive(false);
            }
        }

        private IEnumerator Co_StartTimer(float duration)
        {
            float remaining = duration;
            while (remaining > 0)
            {
                timerText.text = $"Choose Time : {Mathf.CeilToInt(remaining)}s)";
                yield return new WaitForSeconds(1f);
                remaining--;
            }

            if (!hasSelected)
            {
                int randomSlotIndex = Random.Range(0, slots.Length);
                ConfirmSelection(slots[randomSlotIndex].CurrentPropID);
            }
        }

        private void ConfirmSelection(int propID)
        {
            hasSelected = true;
            // 내 캐릭터 컨트롤러를 찾아 RPC 호출
            var myPlayer = BirdPlayerController.Instance;
            if (myPlayer != null)
            {
                myPlayer.Visual.RPC_RequestChangeProp(propID);
            }
            CloseUI();
        }

        public void CloseUI()
        {
            panel.SetActive(false);
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        }
    }

}
