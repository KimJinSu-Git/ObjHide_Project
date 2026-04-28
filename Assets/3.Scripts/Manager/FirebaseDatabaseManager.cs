using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

namespace Bird.Network.Managers
{
    public class FirebaseDatabaseManager : MonoBehaviour
    {
        public static FirebaseDatabaseManager Instance { get; private set; }
        
        private DatabaseReference _dbReference;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            FirebaseManager.OnLoginSuccess += InitializeDatabase;
        }

        private void OnDisable()
        {
            FirebaseManager.OnLoginSuccess -= InitializeDatabase;
        }

        private void InitializeDatabase(FirebaseUser user)
        {
            string dbUrl = "https://birdprophunt-default-rtdb.asia-southeast1.firebasedatabase.app/";
            _dbReference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
            Debug.Log("[FirebaseDB] 데이터베이스 연결 준비 완료");
        }

        public async Task SaveNicknameAsync(string uid, string nickname)
        {
            if (_dbReference == null) return;

            try
            {
                await _dbReference.Child("Users").Child(uid).Child("Nickname").SetValueAsync(nickname);
                Debug.Log($"[FirebaseDB] 닉네임 저장 성공! UID: {uid}, 닉네임: {nickname}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseDB] 닉네임 저장 실패: {e.Message}");
            }
        }

        public async Task<string> LoadNicknameAsync(string uid)
        {
            if (_dbReference == null) return "UnKnown";

            try
            {
                DataSnapshot snapshot = await _dbReference.Child("Users").Child(uid).Child("Nickname").GetValueAsync();
                
                if (snapshot.Exists)
                {
                    string loadedName = snapshot.Value.ToString();
                    Debug.Log($"[FirebaseDB] 닉네임 불러오기 성공: {loadedName}");
                    return loadedName;
                }
                else
                {
                    Debug.Log("[FirebaseDB] 등록된 닉네임이 없습니다.");
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseDB] 닉네임 불러오기 실패: {e.Message}");
                return "ErrorName";
            }
        }
    }
}
