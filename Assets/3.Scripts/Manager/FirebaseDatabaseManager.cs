using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

namespace Bird.Network.Managers
{
    /// <summary>
    /// 확보된 UID로 실시간 데이터베이스에 접속하여 닉네임을 로드합니다.
    /// </summary>
    public class FirebaseDatabaseManager : Singleton<FirebaseDatabaseManager>
    {
        private const string PATH_USERS = "Users";
        private const string PATH_NICKNAME = "Nickname";
        
        private DatabaseReference _dbReference;

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
        }

        public async Task SaveNicknameAsync(string uid, string nickname)
        {
            if (_dbReference == null) return;

            try
            {
                await _dbReference.Child(PATH_USERS).Child(uid).Child(PATH_NICKNAME).SetValueAsync(nickname);
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
