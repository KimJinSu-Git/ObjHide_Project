using System.Xml;
using Bird.Network.Data;
using Bird.Network.Player;
using UnityEngine;

namespace Bird.Network.UI
{
    public class PropSelectionUIHandler : MonoBehaviour
    {
        [SerializeField] private GameObject panel; // 슬롯머신 전체 패널
        [SerializeField] private BirdPropSlotUI[] slots; // 3개의 슬롯
        [SerializeField] private PropDatabase propDatabase;
        
        private void Start() => panel.SetActive(false);

        public void OpenSelectionUI()
        {
            panel.SetActive(true);

            foreach (var slot in slots)
            {
                // 사물 데이터베이스에서 랜덤 사물 하나 추출
                var randomProp = propDatabase.GetRandomProp();
                
                slot.SetupSlot(randomProp, (selectedID) =>
                {
                    ConfirmSelection(selectedID);
                });
                
                slot.SetRerollActive(true); // 처음 한번은 리롤 가능
            }
        }

        private void ConfirmSelection(int propID)
        {
            // 내 캐릭터 컨트롤러를 찾아 RPC 호출
            var myPlayer = BirdPlayerController.Local;
            if (myPlayer != null)
            {
                myPlayer.RPC_RequestChangeProp(propID);
                panel.SetActive(false); // 선택 완료 후 UI 닫기
            }
        }
    }

}
