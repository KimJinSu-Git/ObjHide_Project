using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Bird.Network.Managers
{
    /// <summary>
    /// 서버와 통신하여 리소스 다운로드 크기를 확인하고 패치를 진행합니다.
    /// </summary>
    public class AddressableResourceManager : Singleton<AddressableResourceManager>
    {
        public async Task PreloadByLabel(string label, Action<float, long, long> onProgress)
        {
            // 다운로드해야 할 크기 확인 (이미 다운받았다면 0 출력)
            var sizeHandle = Addressables.GetDownloadSizeAsync(label);
            await sizeHandle.Task;
            long totalSize = sizeHandle.Result;

            if (totalSize > 0)
            {
                Debug.Log($"[ResourceManager] '{label}' 다운로드 필요 : {totalSize} Bytes");
                
                // 실제 다운로드 시작
                var downloadHandle = Addressables.DownloadDependenciesAsync(label);

                while (!downloadHandle.IsDone)
                {
                    var status = downloadHandle.GetDownloadStatus();
                    
                    onProgress?.Invoke(status.Percent, status.DownloadedBytes, status.TotalBytes);
                    
                    await Task.Yield();
                }

                if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"[ResourceManager] '{label} 다운로드 및 캐싱 완료");
                }
                
                Addressables.Release(downloadHandle);
            }
            else
            {
                onProgress?.Invoke(1.0f, 0, 0);
                Debug.Log($"[ResourceManager] '{label} 이미 로컬에 최신 버전이 있습니다.");
            }
            
            Addressables.Release(sizeHandle);
        }
    }
}
